using System;
using System.IO;
using System.Drawing;
using OpenTK.Mathematics;
using KS.Foundation;
using Pfz.Collections;

namespace SummerGUI
{
	/// <summary>
	/// On-demand cached emoji font. Three backends, dispatched by the
	/// color-format table present in the font file (mutually exclusive):
	///
	/// <b>CBDT/CBLC (PNG bitmaps)</b> — Noto Color Emoji (Linux system font).
	/// Parsed by <see cref="ColorFontParser"/>; a single fixed-size strike is
	/// cached one texture per code point.
	///
	/// <b>COLR/CPAL (vector)</b> — Segoe UI Emoji (Windows system font,
	/// <c>Segoe UI Emoji.ttf</c>). Rasterized on demand by
	/// <see cref="ColrEmojiRasterizer"/> via the bundled FreeType
	/// <c>ftcolr</c> module (<c>FT_LOAD_COLOR</c>), at exactly the requested
	/// pixel size and cached per (code point, height).
	///
	/// <b>sbix (PNG bitmaps in TTC)</b> — Apple Color Emoji (macOS system
	/// font, <c>Apple Color Emoji.ttc</c>). Parsed by
	/// <see cref="SbixBitmapParser"/>; the closest strike is cached per
	/// (code point, ppem).
	///
	/// All backends read the OS system emoji font (path from
	/// <c>SystemSpecific.*.Common.EmojiFontPath</c>) — no bundled font.
	/// Each backend exposes the same <see cref="GlyphInfo"/> contract through
	/// <c>TryGetGlyphInfo</c>, <c>HasGlyph</c>, and <c>GetAdvance</c>.
	///
	/// Design (paint hot path):
	///  - No exceptions on the fast path (a single throw costs ~50 µs and
	///    aborts a render frame).
	///  - PFZ <c>ThreadSafeDictionary</c>: concurrent readers never block;
	///    writes are atomic, at most one insert per cache key.
	///  - Lazy singleton: the font file is opened on first emoji render, not
	///    at application start; no GL work until a glyph is actually drawn.
	/// </summary>
	public sealed class EmojiFont : DisposableObject
	{
		private readonly ColorFontParser     _cbdt;   // CBDT-17 backend (Linux)
		private readonly ColrEmojiRasterizer _colr;   // COLR/CPAL backend (Windows)
		private readonly SbixBitmapParser    _sbix;   // sbix backend (macOS)

		// Cache key: (codepoint << 16) | height.
		// Heights > 0xFFFE px are meaningless for emoji; clamp as a safety net.
		private readonly ThreadSafeDictionary<uint, TextureImage> _cache
			= new ThreadSafeDictionary<uint, TextureImage>(64);

		// --- Lazy singleton (never loaded unless an emoji is actually rendered) ---
		private static readonly Lazy<EmojiFont> _instance = new(() =>
		{
			string path = ResolveFontPath();
			if (string.IsNullOrEmpty(path) || !File.Exists(path))
				return null;

			try
			{
				// Sniff which color-format table the font actually has and
				// construct only the matching backend.
				bool hasSubx = HasSfntTable(path, "sbix");
				bool hasColr = HasSfntTable(path, "COLR");
				bool hasCbdt = HasSfntTable(path, "CBDT");

				SbixBitmapParser    sbx  = hasSubx ? new SbixBitmapParser(path) : null;
				ColrEmojiRasterizer colr = hasColr ? new ColrEmojiRasterizer(path) : null;
				ColorFontParser     cbdt = hasCbdt ? new ColorFontParser(path) : null;

				if (sbx == null && colr == null && cbdt == null)
				{
					System.Diagnostics.Debug.WriteLine(
						"EmojiFont: no recognized color-format table in " + path);
					return null;
				}

				return new EmojiFont(cbdt, colr, sbx);
			}
			catch (Exception ex)
			{
				ex.LogError();
				return null;
			}
		});

		public static EmojiFont Instance => _instance.Value;

		/// <summary>
		/// Liefert die Instanz NUR, wenn sie bereits geladen wurde — startet
		/// die Lazy-Initialisierung bewusst NICHT (Shutdown-Pfad: nichts
		/// zu räumen, wenn nie ein Emoji gerendert wurde).
		/// </summary>
		public static EmojiFont InstanceOrNull => _instance.IsValueCreated ? _instance.Value : null;

		/// <summary>
		/// True if the character is an emoji (either surrogate half of an
		/// astral pair, or an emoji-possible BMP/supplementary code point).
		/// Static and font-independent so widgets can use it as an input
		/// filter before an <see cref="EmojiFont"/> instance is loaded.
		/// </summary>
		public static bool IsEmojiChar(char c)
		{
			if (char.IsHighSurrogate(c) || char.IsLowSurrogate(c))
				return true;

			int cp = c;
			if (cp == 0x200D) return true;   // ZWJ  (family / skin-tone combos)
			if (cp == 0xFE0F) return true;   // VS-16 (emoji presentation selector)

			return
				(cp >= 0x2190 && cp <= 0x21FF)    // Arrows
				|| (cp >= 0x2300 && cp <= 0x23FF) // Misc Tech
				|| (cp >= 0x2600 && cp <= 0x26FF) // Misc Symbols
				|| (cp >= 0x2700 && cp <= 0x27BF) // Dingbats
				|| (cp >= 0x2B00 && cp <= 0x2BFF) // Misc Shapes (⭐)
				|| cp == 0x3297 || cp == 0x3299   // ㊗ ㊙
				|| (cp >= 0x1F000 && cp <= 0x1FAFF);   // supplementary emoji planes
		}

		/// <summary>
		/// Safe UTF-16 → code point extraction. Never throws: lone surrogates
		/// and out-of-range indices yield 0.
		/// </summary>
		public static uint SafeCodePoint(string text, int index)
		{
			if (text == null || index < 0 || index >= text.Length)
				return 0;

			char c = text[index];
			if (char.IsHighSurrogate(c) && index + 1 < text.Length)
			{
				char low = text[index + 1];
				if (char.IsLowSurrogate(low))
					return (((uint)(c - 0xD800)) << 10) | (uint)(low - 0xDC00) | 0x10000;
			}
			return char.IsSurrogate(c) ? 0u : (uint)c;
		}

		/// <summary>True if a color glyph is available for <paramref name="cp"/>.</summary>
		public bool HasGlyph(uint cp)
		{
			if (_sbix != null)
				return _sbix.HasGlyph(cp);
			if (_colr != null)
				return _colr.HasGlyph(cp);
			if (_cbdt != null)
				return _cbdt.HasGlyph(cp);
			return false;
		}

		/// <summary>
		/// Fills <paramref name="info"/> for <paramref name="cp"/> at
		/// <paramref name="targetHeight"/> px, creating (or returning) a cached
		/// <see cref="TextureImage"/> at most once per (code point, height).
		/// Returns false (never throws) when the font has no such glyph.
		/// </summary>
		public bool TryGetGlyphInfo(uint cp, float targetHeight, out GlyphInfo info)
		{
			info = default;
			if (targetHeight <= 0)
				targetHeight = 16f;

			if (_sbix != null)
				return TryGetSbix(cp, targetHeight, out info);

			if (_colr != null)
				return TryGetColr(cp, targetHeight, out info);

			if (_cbdt != null)
				return TryGetCbdt(cp, targetHeight, out info);

			return false;
		}

		/// <summary>
		/// Advance in px for <paramref name="cp"/> at <paramref name="targetHeight"/>.
		/// No GL context required — safe for layout / word-wrap passes.
		/// </summary>
		public float GetAdvance(uint cp, float targetHeight)
		{
			if (targetHeight <= 0)
				return 0f;

			if (_sbix != null)
				return REF_ADV * targetHeight;

			if (_colr != null)
				return REF_ADV * targetHeight;

			if (_cbdt != null && _cbdt.TryGetGlyph(cp, out ColorGlyphEntry e) && e.Height > 0)
				return e.Advance * (targetHeight / e.Height);

			return targetHeight; // 1× em as a sane fallback
		}

		// ---------------------------------------------------------------- backends

		// Metrik-Vertrag für COLR/CPAL-Emojis — referenziert am vor der
		// Windows-Arbeit „perfekt" ausgerichten Emoji-Font (Noto Color Emoji,
		// CBDT-17): dort sind bearY/Height = 101/128 und Advance/Height = 136/128,
		// unabhängig vom Codepoint. Beide Anteile sind proportional zur
		// Zielhöhe, damit bei jeder Größe identisch.
		const float REF_TOP = 101f / 128f;   // Anteil der Höhe ÜBER der Baseline
		const float REF_ADV = 136f / 128f;   // Vorlauf in Einheiten der Höhe

		// sbix (macOS Apple Color Emoji) — Metrik-Vertrag:
		// Apple liefert originOffset = (0,0) für alle Glyphen und 1 em = 800 upm.
		// Damit Apple identisch mit den verifizierten Noto(101/128)/Segoe-
		// Referenzen sitzt, setzen wir wie dort REF_TOP / REF_ADV — die Tinte
		// ragt ~21 % über die Baseline hinaus und der Vorlauf ist die volle
		// Emoji-Zelle (136/128), unabhängig von der Zielhöhe.
		const float SBIX_TOP = REF_TOP;      // Anteil der Höhe ÜBER der Baseline
		const float SBIX_ADV = REF_ADV;      // Vorlauf in Einheiten der Höhe

		bool TryGetSbix(uint cp, float targetHeight, out GlyphInfo info)
		{
			info = default;

			int ppem = Math.Max(1, (int)MathF.Round(targetHeight));

			if (!_sbix.TryGetGlyph(cp, ppem, out SbixGlyphEntry e) || e.PngData == null || e.PngData.Length == 0)
				return false;

			uint key = (cp << 16) | (uint)e.StrikePx;
			TextureImage tex = null;
			if (!_cache.TryGetValue(key, out tex) || tex == null)
			{
				tex = TextureImage.FromBytes(e.PngData, "emoji_" + cp.ToString("X8"));
				if (tex != null && tex.Width > 0 && tex.Height > 0)
					_cache.TryAdd(key, tex);
				else
					return false;
			}

			if (tex.Height <= 0)
				return false;

			float scale = targetHeight / tex.Height;
			info.TextureId = tex.TextureID;
			info.Size      = new Vector2(tex.Width * scale, targetHeight);
			info.Bearing   = new Vector2(e.OriginOffsetX * scale, SBIX_TOP * targetHeight);
			info.Advance   = SBIX_ADV * targetHeight;
			info.UV        = new RectangleF(0, 0, 1, 1);
			return true;
		}

		bool TryGetCbdt(uint cp, float targetHeight, out GlyphInfo info)
		{
			info = default;

			if (!_cbdt.TryGetGlyph(cp, out ColorGlyphEntry entry) || entry.PngData == null || entry.PngData.Length == 0)
				return false;

			uint key = cp << 16;
			TextureImage tex = null;
			if (!_cache.TryGetValue(key, out tex) || tex == null)
			{
				tex = TextureImage.FromBytes(entry.PngData, "emoji_" + cp.ToString("X8"));
				if (tex != null && tex.Width > 0 && tex.Height > 0)
					_cache.TryAdd(key, tex);
				else
					return false;
			}

			if (tex.Height <= 0)
				return false;

			float scale = targetHeight / tex.Height;
			info.TextureId = tex.TextureID;
			info.Size      = new Vector2(tex.Width * scale, targetHeight);
			info.Bearing   = new Vector2(entry.LeftBearing * scale, entry.TopBearing * scale);
			info.Advance   = entry.Advance * scale;
			info.UV        = new RectangleF(0, 0, 1, 1);
			return true;
		}

		bool TryGetColr(uint cp, float targetHeight, out GlyphInfo info)
		{
			info = default;

			int h = Math.Max(1, (int)MathF.Round(targetHeight));

			// Rasterize exactly at target height (vector ⇒ crisp, no rescale).
			if (!_colr.Rasterize(cp, h, out var raster) || raster.Rgba == null || raster.BitmapW <= 0 || raster.BitmapH <= 0)
				return false;

			int w = raster.BitmapW;
			int rh = raster.BitmapH;

			uint key = (cp << 16) | (uint)Math.Min(rh, 0xFFFE);
			TextureImage tex = null;
			if (!_cache.TryGetValue(key, out tex) || tex == null)
			{
				tex = TextureImage.FromRawBytes(raster.Rgba, "emoji_" + cp.ToString("X8") + "_" + h, new Size(w, rh));
				if (tex != null && tex.Width > 0 && tex.Height > 0)
					_cache.TryAdd(key, tex);
				else
					return false;
			}

			float scale = targetHeight / rh;
			info.TextureId = tex.TextureID;
			info.Size      = new Vector2(w * scale, targetHeight);

			// Metrik-Vertrag (siehe REF_TOP/REF_ADV): die Tinte ragt ~21% unter
			// die Baseline (wie ein Großbuchstabe mit etwas Descent) und der
			// Vorlauf ist die volle Emoji-Zelle — exakt wie die CBDT-Referenz.
			//
			// FreeType meldt für COLR-Bitmaps bitmap_top ≈ 0 → ein naives
			// Bearing.Y würde das ganze Glyph UNTER die Baseline drücken (der
			// „zu niedrig"-Bug). REF_TOP korrigiert das.
			info.Bearing   = new Vector2((raster.Left / 64f) * scale, REF_TOP * targetHeight);
			info.Advance   = REF_ADV * targetHeight;
			info.UV        = new RectangleF(0, 0, 1, 1);
			return true;
		}

		// ---------------------------------------------------------------- internals

		/// <summary>
		/// True if the sfnt header of <paramref name="path"/> lists <paramref name="tag"/>.
		/// Versteht auch <b>TrueType Collections</b> (ttcf, z. B. die echte
		/// Apple Color Emoji.ttc): dann wird das erste Face gelesen, nicht die
		/// Collection-Header-Bytes (sonst sieht man hier 0 gültige Tabellen).
		/// </summary>
		static bool HasSfntTable(string path, string tag)
		{
			try
			{
				using var fs = File.OpenRead(path);
				byte[] header = new byte[Math.Min(8192, (int)fs.Length)];
				int n = fs.Read(header, 0, header.Length);
				if (n < 12)
					return false;

				// ttcf-Header: "ttcf" | version(u32) | numFonts(u32) | faceOffsets[u32][numFonts].
				// Das erste Face beginnt am ersten Offset — von dort ist der
				// Aufbau identisch zu einem einzelnen sfnt.
				int baseOffset = 0;
				if (n >= 20 && header[0] == (byte)'t' && header[1] == (byte)'t'
					&& header[2] == (byte)'c' && header[3] == (byte)'f')
				{
					baseOffset = (header[12] << 24) | (header[13] << 16) | (header[14] << 8) | header[15];
					if (baseOffset < 12 || n < baseOffset + 12)
						return false;
				}

				int numTables = (header[baseOffset + 4] << 8) | header[baseOffset + 5];
				for (int i = 0; i < numTables; i++)
				{
					int o = baseOffset + 12 + i * 16;
					if (o + 4 > n)
						break;
					if (System.Text.Encoding.ASCII.GetString(header, o, 4) == tag)
						return true;
				}
				return false;
			}
			catch
			{
				return false;
			}
		}

		protected override void CleanupUnmanagedResources()
		{
			foreach (var kv in _cache)
			{
				if (kv.Value != null)
					kv.Value.Dispose();
			}
			_cache.Clear();

			if (_colr != null)
				_colr.Dispose();
			// _cbdt and _sbix hold only managed data (bytes + structs) — nothing to release.

			base.CleanupUnmanagedResources();
		}

		private EmojiFont(ColorFontParser cbdt, ColrEmojiRasterizer colr, SbixBitmapParser sbix)
		{
			_cbdt = cbdt;
			_colr = colr;
			_sbix = sbix;
		}

		/// <summary>
		/// Resolve the emoji font path from the platform-specific <c>Common</c>
		/// class — always the OS system emoji font, no bundled file.
		/// </summary>
		static string ResolveFontPath()
		{
			return PlatformExtensions.CurrentOS switch
			{
				PlatformExtensions.OS.Windows => SystemSpecific.Windows.Common.EmojiFontPath,
				PlatformExtensions.OS.Mac     => SystemSpecific.Mac.Common.EmojiFontPath,
				_                             => SystemSpecific.Linux.Common.EmojiFontPath,
			};
		}
	}
}
