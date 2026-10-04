using System;
using System.IO;
using System.Runtime.InteropServices;
using FreeTypeSharp;
using static FreeTypeSharp.FT;
using KS.Foundation;

namespace SummerGUI
{
	/// <summary>
	/// Rasterisiert COLR/CPAL-Vektor-Emoji (z. B. <c>seguiemj.ttf</c>, Segoe UI Emoji)
	/// über das gelieferte FreeType-Modul <c>ftcolr</c> (<c>FT_LOAD_COLOR</c>).
	/// Ergebnis: fertige RGBA-Farbdaten + 26.6-Metriken in <see cref="RasterizeResult"/>.
	///
	/// Das Objekt bleibt GL-kontextfrei; das Uploaden nach OpenGL und das Scalen
	/// übernimmt die Aufrufern (siehe <see cref="EmojiFont"/>).
	/// </summary>
	public unsafe sealed class ColrEmojiRasterizer : DisposableObject
	{
		readonly byte[] _bytes;
		GCHandle _pin;            // hält die Font-Bytes für die Lifetime des Objekts pinned
		FT_FaceRec_* _face;
		readonly object _gate = new object();

		public ColrEmojiRasterizer(string fontPath)
		{
			if (!File.Exists(fontPath))
				throw new FileNotFoundException("Emoji font not found: " + fontPath, fontPath);

			_bytes = File.ReadAllBytes(fontPath);

			// CRITICAL — die Font-Bytes MÜSSEN für das LEBEN des Face gepinnt sein.
			//
			// FT_New_Memory_Face hält einen nativen Zeiger auf den verwalteten Puffer.
			// Ein <c>fixed</c>-Block, der nach dem ctor-Block endet, macht das Array
			// wieder movable: in jeder Lücke zwischen dem <c>fixed</c>-Block und dem
			// nächsten <c>FT_Load_Glyph</c>-Aufruf darf der GC die Bytes verschieben oder
			// den Puffer wiederverwenden. Dann liest FreeType von einem veralteten
			// Zeiger — jede Glyph-Outline degeneriert zu einer ~1px-Sliver ohne Fehler.
			//
			// Ein <c>GCHandle</c> (<c>Pinned</c>) hält den Puffer an Ort und Stelle bis
			// wir ihn im <see cref="CleanupUnmanagedResources"/> freigeben.
			_pin = GCHandle.Alloc(_bytes, GCHandleType.Pinned);

			try
			{
				byte* b = (byte*)_pin.AddrOfPinnedObject();
				unsafe
				{
					fixed (FT_FaceRec_** ppf = &_face)
					{
						FT_Error err = FT_New_Memory_Face(
							FontManager.Library,
							b,
							new IntPtr(_bytes.Length),
							new IntPtr(0),   // face_index = 0 — IntPtr, 64-bit ABI
							ppf);
						if (err != FT_Error.FT_Err_Ok)
							throw new InvalidOperationException($"FT_New_Memory_Face failed: {err}");
					}
				}
			}
			catch
			{
				// Cleanup auf Fehler: Handle auch freigeben, kein Leck.
				_pin.Free();
				_pin = default;
				throw;
			}
		}

		/// <summary>True, wenn der Font einen (vektorierten) Glyphen für <paramref name="cp"/> hat.</summary>
		unsafe public bool HasGlyph(uint cp)
		{
			if (_face == null)
				return false;

			return FT_Get_Char_Index(_face, new UIntPtr(cp)) != 0;
		}

		/// <summary>
		/// Rasterisiert den Glyphen für <paramref name="cp"/> bei <paramref name="pixelSize"/> px.
		/// Liefern <paramref name="result"/>. Die 26.6-Metriken (Left/Top/Advance)
		/// sind in 1/64 px; BitmapW/BitmapH sind einfache Pixel.
		/// </summary>
		public bool Rasterize(uint cp, int pixelSize, out RasterizeResult result)
		{
			result = default;

			if (_face == null || pixelSize <= 0)
				return false;

			lock (_gate) // FT_Set_Pixel_Sizes + FT_Load_Glyph sind face-local, nicht thread-safe
			{
				return RasterizeLocked(cp, pixelSize, out result);
			}
		}

		/// <summary>
		/// Liefert nur den Vorspan (Advance) im 26.6 (1/64 px) beim angegebenen
		/// <paramref name="pixelSize"/> — ohne Farb-Rasterisierung (FT_LOAD_ADVANCE_ONLY).
		/// Damit die Layout-/Cursor-Berechnung (GetAdvance) keine GL-Textur erzeugen muss.
		/// </summary>
		unsafe public bool GetAdvance266(uint cp, int pixelSize, out int adv266)
		{
			adv266 = 0;
			if (_face == null || pixelSize <= 0)
				return false;

			lock (_gate)
			{
				if (FT_Set_Pixel_Sizes(_face, (uint)pixelSize, (uint)pixelSize) != FT_Error.FT_Err_Ok)
					return false;
				uint gi = FT_Get_Char_Index(_face, new UIntPtr(cp));
				if (gi == 0)
					return false;
				if (FT_Load_Glyph(_face, gi, (FT_LOAD)FT_LOAD.FT_LOAD_ADVANCE_ONLY) != FT_Error.FT_Err_Ok)
					return false;
				adv266 = (int)(long)_face->glyph->advance.x;
				return adv266 > 0;
			}
		}

		unsafe bool RasterizeLocked(uint cp, int pixelSize, out RasterizeResult result)
		{
			result = default;

			if (_face == null || pixelSize <= 0)
				return false;

			if (FT_Set_Pixel_Sizes(_face, (uint)pixelSize, (uint)pixelSize) != FT_Error.FT_Err_Ok)
				return false;

			uint gi = FT_Get_Char_Index(_face, new UIntPtr(cp));
			if (gi == 0)
				return false;

			if (FT_Load_Glyph(_face, gi, (FT_LOAD)(FT_LOAD.FT_LOAD_RENDER | FT_LOAD.FT_LOAD_COLOR)) != FT_Error.FT_Err_Ok)
				return false;

			FT_GlyphSlotRec_* slot = _face->glyph;
			FT_Bitmap_ bm = slot->bitmap;

			int w = (int)bm.width;
			int h = (int)bm.rows;
			if (w <= 0 || h <= 0 || bm.buffer == null || bm.pitch < w * 4)
				return false;

			if (bm.pixel_mode != FT_Pixel_Mode_.FT_PIXEL_MODE_BGRA)
				return false;   // COLR liefert ausschließlich BGRA

			// Schritt 1: rohes Bitmap (pitch×rows) byte-für-byte kopieren.
			// Schritt 2: BGRA → RGBA. WICHTIG: das Ziel-Index muss pro Pixel
			// d = (y*w + x) * 4 sein (Zeile + Spalte). Fehlt der x-Anteil,
			// schreibt jede Zeile nur in ihre ersten 4 Bytes (~1px-vertikaler
			// Strich, Rest bleibt Null) — genau der „Windows-Emoji"-Sliver-Bug.
			byte[] bgra = new byte[(long)h * bm.pitch];
			for (int y = 0; y < h; y++)
				for (int i = 0; i < bm.pitch; i++)
					bgra[(long)y * bm.pitch + i] = ((byte*)bm.buffer)[(long)y * bm.pitch + i];

			byte[] rgba = new byte[w * h * 4];
			for (int y = 0; y < h; y++)
			{
				int s = (int)((long)y * bm.pitch);
				for (int x = 0; x < w; x++)
				{
					int p = s + x * 4;
					int d = (y * w + x) * 4;
					rgba[d + 0] = bgra[p + 2];   // R
					rgba[d + 1] = bgra[p + 1];   // G
					rgba[d + 2] = bgra[p + 0];   // B
					rgba[d + 3] = bgra[p + 3];   // A
				}
			}

			result = new RasterizeResult(
				rgba, w, h,
				slot->bitmap_left,
				slot->bitmap_top,
				(int)(long)slot->advance.x);   // 26.6 (1/64 px)
			return true;
		}

		protected override void CleanupUnmanagedResources()
		{
			unsafe
			{
				// REIHENFOLGE (Windows, AV 0xc0000005): das Face gehört zu
				// FontManager.Library und MUSS vor FT_Done_FreeType geschlossen
				// werden. FontManager erledigt genau das (Face-Eigner erst,
				// dann Library).
				if (_face != null)
				{
					FT_Done_Face(_face);
					_face = null;
				}
			}
			if (_pin != default)
				_pin.Free();
			base.CleanupUnmanagedResources();
		}

		/// <summary>Rasterisierungs-Ergebnis: RGBA-Farbdaten + 26.6-Metriken (1/64 px).</summary>
		public readonly struct RasterizeResult
		{
			public byte[] Rgba    { get; }   // RGBA-BYTES (Byte-Reihenfolge R,G,B,A)
			public int BitmapW   { get; }   // in px
			public int BitmapH   { get; }   // in px
			public int Left      { get; }   // 26.6 (1/64 px); positiv = rechts von Pen-X → x = PenX + Left
			public int Top       { get; }   // 26.6; positiv = über Baseline → topY = BaselineY - Top
			public int Advance   { get; }   // 26.6

			public RasterizeResult(byte[] rgba, int w, int h, int left, int top, int advance)
			{
				Rgba = rgba;
				BitmapW = w;
				BitmapH = h;
				Left = left;
				Top = top;
				Advance = advance;
			}
		}
	}
}
