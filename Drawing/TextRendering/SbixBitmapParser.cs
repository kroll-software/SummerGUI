using System;
using System.Collections.Generic;
using System.IO;

namespace SummerGUI
{
	/// <summary>
	/// Parsed <b>sbix</b> color-bitmap entry — the macOS system-emoji format
	/// (Apple Color Emoji.ttc, 9 strikes 20-160 px, one PNG per glyph per strike).
	///
	/// Field semantics mirror <see cref="ColorGlyphEntry"/> (the CBDT-17 struct
	/// used by the Linux Noto path), so <see cref="EmojiFont"/> can hand
	/// <c>PngData</c> to the same <c>TextureImage.FromBytes</c> upload on
	/// either platform.
	/// </summary>
	public readonly struct SbixGlyphEntry
	{
		public byte[]  PngData     { get; }   // raw PNG bytes (null if empty)
		public int     Width       { get; }
		public int     Height      { get; }
		public int     AdvanceEm   { get; }   // em-unit advance (1 em = upem)
		public int     OriginOffsetX { get; } // i16 (record bytes 0-1); usually 0
		public int     OriginOffsetY { get; } // i16 (record bytes 2-3); usually 0
		public int     StrikePx    { get; }   // ppem of the strike we picked

		public SbixGlyphEntry(byte[] png, int w, int h, int advEm, int ox, int oy, int strikePx)
		{
			PngData = png; Width = w; Height = h;
			AdvanceEm = advEm; OriginOffsetX = ox; OriginOffsetY = oy; StrikePx = strikePx;
		}
	}

	/// <summary>
	/// Parses OpenType <b>sbix</b> (Standard Bitmap Image Format) — the color-
	/// bitmap format used by the Apple/macOS system emoji font
	/// (<c>Apple Color Emoji.ttc</c>).
	///
	/// Layout (big-endian, all offsets relative to the strike start; verified
	/// against raw bytes of <c>Apple Color Emoji.ttc</c> 2026-10-04):
	///
	/// <b>sbix table</b> (at <c>sbixOff</c>):
	/// <pre>
	///   0  u16 version (=1)
	///   2  u16 flags
	///   4  u32 numStrikes
	///   8  u32 strikeOffsets[numStrikes]   // relative to sbixOff
	/// </pre>
	///
	/// <b>Each strike</b> (soff = sbixOff + strikeOffsets[i]):
	/// <pre>
	///   soff+0   u16 ppem
	///   soff+2   u16 resolution
	///   soff+4   u32 firstGlyphDataOffset   := firstData  (also = offsets[0])
	///   soff+4.. u32 glyphDataOffsets[0..N] // N+1 = (firstData-4)//4 entries
	///            (each entry is an offset RELATIVE TO soff)
	///   soff+firstData ..
	///            glyph record data
	/// </pre>
	///
	/// <b>Record count:</b>  N = (firstData - 8) / 4
	/// (the offset array has N+1 entries, delimiting N records gids 0..N-1)
	///
	/// <b>Record boundaries for glyph gid g (0 ≤ g ≤ N-1):</b>
	/// <pre>
	///   start = soff + offsets[g]
	///   end   = soff + offsets[g+1]
	/// </pre>
	/// where offsets[g] is at <c>soff + 4 + g*4</c>.
	///
	/// <b>Record format:</b>
	/// <pre>
	///   i16  originOffsetX
	///   i16  originOffsetY
	///   4s   graphicType   ("png " | "dupe" | "flip")
	///   ...  (PNG bytes for "png "; u16 ref-gid for "dupe"/"flip")
	/// </pre>
	///
	/// The advance is read from <c>hmtx</c> with the spec-clamp
	/// <c>idx = min(gid, numHMetrics-1)</c>; Apple's real font has
	/// numHMetrics=76, gid(0x1F600) ≥ 76 → clamped = hmtx[75] = 800 (= 1 em).
	/// </summary>
	public sealed class SbixBitmapParser
	{
		readonly byte[] _b;
		readonly int     _face0;       // TTC face-0 offset (0 for plain sfnt)
		readonly int     _upm;         // head.unitsPerEm (fallback = 1 em)
		readonly int     _numHMetrics; // hhea.numberOfHMetrics (0 if absent)
		readonly int     _hmtxOff;     // absolute (0 if absent)
		readonly int     _cmapOff;     // absolute (0 if absent)
		readonly Dictionary<uint, uint> _cmap = new();
		// Per-strike parallel lists (indexed by strike number):
		readonly List<int> _strikePx  = new();   // ppem
		readonly List<int> _soff      = new();   // absolute strike start
		readonly List<int> _recN      = new();   // N (records 0..N-1); offsets[gid] at soff+4+gid*4

		public SbixBitmapParser(string fontPath)
		{
			if (!File.Exists(fontPath))
				throw new FileNotFoundException("Emoji font not found", fontPath);

			_b = File.ReadAllBytes(fontPath);
			if (_b.Length < 0x20)
				throw new InvalidDataException("Font file too small: " + fontPath);

			// --- TTC or plain sfnt ---
			_face0 = 0;
			if (_b.Length >= 20
			 && _b[0] == (byte)'t' && _b[1] == (byte)'t'
			 && _b[2] == (byte)'c' && _b[3] == (byte)'f')
			{
				int firstFace = BE32(12);
				if (firstFace < 12 || firstFace + 0x20 > _b.Length)
					throw new InvalidDataException("Invalid TTC layout: " + fontPath);
				_face0 = firstFace;
			}

			// --- table directory ---
			int sbixOff = 0;
			int numTabs = BE16(_face0 + 4);
			for (int t = 0; t < numTabs; t++)
			{
				int o   = _face0 + 12 + t * 16;
				string tag = Tag(o);
				int tabOff = BE32(o + 8);
				switch (tag)
				{
					case "sbix": sbixOff  = tabOff; break;
					case "head": _upm = BE16(tabOff + 18); break;           // unitsPerEm @+18
					case "hhea": _numHMetrics = BE16(tabOff + 34); break;   // numberOfHMetrics
					case "hmtx": _hmtxOff = tabOff; break;
					case "cmap": _cmapOff = tabOff; break;
				}
			}

			if (sbixOff == 0 || sbixOff >= _b.Length)
				throw new InvalidDataException("No sbix table: " + fontPath);

			int numStrikes = BE32(sbixOff + 4);    // u16ver u16flags u32numStrikes
			if (numStrikes < 1)
				throw new InvalidDataException("sbix has no strikes: " + fontPath);

			for (int si = 0; si < numStrikes; si++)
			{
				int soff = sbixOff + BE32(sbixOff + 8 + si * 4);   // strikeOffsets[] @ sbixOff+8
				if (soff < 4 || soff + 8 > _b.Length)
					continue;   // malformed / out of range

				int ppem = BE16(soff);
				int firstData = BE32(soff + 4);
				// N = number of glyph records; offsets array has N+1 entries
				// firstData = 4N + 8  →  N = (firstData - 8) / 4
				if (firstData < 8 || (firstData - 8) % 4 != 0)
					continue;

				int N = (firstData - 8) / 4;
				if (N < 0)
					N = 0;

				// last offset entry (index N) at soff+4+N*4, must be in bounds
				if (soff + 4 + (N + 1) * 4 > _b.Length)
					continue;

				_strikePx.Add(ppem);
				_soff.Add(soff);
				_recN.Add(N);
			}

			if (_strikePx.Count == 0)
				throw new InvalidDataException("No usable sbix strikes: " + fontPath);

			if (_cmapOff > 0)
				ParseCmap(_cmapOff);
		}

		public bool HasGlyph(uint cp) => _cmap.ContainsKey(cp);

		public int GetAdvanceEm(uint cp)
		{
			if (_hmtxOff > 0 && _numHMetrics > 0 && _cmap.TryGetValue(cp, out uint gid))
			{
				int idx = (int)Math.Min(gid, (uint)(_numHMetrics - 1));
				int o   = _hmtxOff + idx * 4;
				if (o + 2 <= _b.Length)
					return BE16(o);
			}
			return _upm;
		}

		/// <summary>
		/// Picks the closest strike by ppem to <paramref name="targetPx"/>
		/// and retrieves the PNG entry for <paramref name="cp"/>.
		/// </summary>
		public bool TryGetGlyph(uint cp, int targetPx, out SbixGlyphEntry entry)
		{
			entry = default;
			if (!_cmap.TryGetValue(cp, out uint gid))
				return false;

			int best = 0, bestDist = int.MaxValue;
			for (int k = 0; k < _strikePx.Count; k++)
			{
				int d = Math.Abs(_strikePx[k] - targetPx);
				if (d < bestDist) { bestDist = d; best = k; }
			}

			return ResolveRecord(cp, gid, best, out entry, 0);
		}

		bool ResolveRecord(uint cp, uint gid, int strikeIdx, out SbixGlyphEntry entry, int depth)
		{
			entry = default;
			if (depth > 16 || strikeIdx < 0 || strikeIdx >= _strikePx.Count)
				return false;

			int soff = _soff[strikeIdx];
			int N    = _recN[strikeIdx];
			int ppem = _strikePx[strikeIdx];

			if (N <= 0 || gid >= (uint)N)
				return false;

			// offsets[gid]     at soff + 4 + gid*4
			// offsets[gid+1]   at soff + 4 + (gid+1)*4
			int gi = (int)gid;
			int o  = BE32(soff + 4 + gi * 4);
			int o1 = BE32(soff + 4 + (gi + 1) * 4);

			if (o >= o1)
				return false;

			int rs = soff + o;
			int re = soff + o1;
			if (rs < soff || re > _b.Length || re <= rs + 8)
				return false;

			short ox = (short)BE16(rs);
			short oy = (short)BE16(rs + 2);
			string gt;
			try { gt = System.Text.Encoding.ASCII.GetString(_b, rs + 4, 4).Trim(); }
			catch { return false; }

			if (gt == "flip" || gt == "dupe")
			{
				// Reference record: body is a u16 glyph id.
				if (re < rs + 10) return false;
				uint refId = (uint)BE16(rs + 8);
				return ResolveRecord(cp, refId, strikeIdx, out entry, depth + 1);
			}

			if (gt != "png")
				return false;

			int pngOff = rs + 8;
			int pngLen = re - pngOff;
			// Body is a FULL PNG file: 8-byte signature, then chunks.
			//   [0:8]   signature  89 50 4E 47 0D 0A 1A 0A
			//   [8:12]  IHDR length (u32, == 13)
			//   [12:16] "IHDR"
			//   [16:20] width (u32)
			//   [20:24] height (u32)
			if (pngLen < 24 || pngOff + 24 > _b.Length)
				return false;

			if (_b[pngOff]     != 0x89
			 || _b[pngOff + 1] != (byte)'P'
			 || _b[pngOff + 2] != (byte)'N'
			 || _b[pngOff + 3] != (byte)'G'
			 || _b[pngOff + 12] != (byte)'I'
			 || _b[pngOff + 13] != (byte)'H'
			 || _b[pngOff + 14] != (byte)'D'
			 || _b[pngOff + 15] != (byte)'R')
				return false;

			int w = BE32(pngOff + 16);
			int h = BE32(pngOff + 20);
			if (w <= 0 || h <= 0 || w > 0xFFFF || h > 0xFFFF)
				return false;

			byte[] png = new byte[pngLen];
			Buffer.BlockCopy(_b, pngOff, png, 0, pngLen);

			int advEm = GetAdvanceEm(cp);
			entry = new SbixGlyphEntry(png, w, h, advEm, ox, oy, ppem);
			return true;
		}

		// ---- big-endian helpers ----
		static int BE16(byte[] b, int o) => (b[o] << 8) | b[o + 1];
		static int BE32(byte[] b, int o) => (b[o] << 24) | (b[o + 1] << 16) | (b[o + 2] << 8) | b[o + 3];
		int BE16(int o)  => BE16(_b, o);
		int BE32(int o)  => BE32(_b, o);
		string Tag(int o) => System.Text.Encoding.ASCII.GetString(_b, o, 4);

		// ---- cmap (format 4 + format 12) ----
		void ParseCmap(int cmapOff)
		{
			int numSub = BE16(cmapOff + 2);
			for (int i = 0; i < numSub; i++)
			{
				int subRef = BE32(cmapOff + 4 + i * 8 + 4);
				int abs    = cmapOff + subRef;
				int fmt    = BE16(abs);
				if (fmt == 4) ParseCmap4(abs);
				else if (fmt == 12) ParseCmap12(abs);
			}
		}

		void ParseCmap4(int abs)
		{
			int segCount = BE16(abs + 2) / 2;
			int startOff = abs + 6 + segCount * 2;
			int idelOff  = startOff + segCount * 2;
			int idrngOff = idelOff  + segCount * 2;
			for (int s = 0; s < segCount; s++)
			{
				int endC   = BE16(abs + 6 + s * 2);
				int startC = BE16(startOff + s * 2);
				int idDl   = BE16(idelOff + s * 2);
				int idRf   = BE16(idrngOff + s * 2);
				for (int c = startC; c <= endC && c != 0xFFFF; c++)
				{
					uint g;
					if (idRf == 0)
						g = (uint)(c + idDl) & 0xFFFF;
					else
					{
						int idAbs = idrngOff + s * 2 + idRf + (c - startC) * 2;
						if (idAbs + 2 > _b.Length) continue;
						int raw = BE16(idAbs);
						g = raw == 0 ? 0 : ((uint)(raw + idDl) & 0xFFFF);
					}
					if (g > 0) _cmap[(uint)c] = g;
				}
			}
		}

		void ParseCmap12(int abs)
		{
			int groups = BE32(abs + 12);
			int off    = abs + 16;
			for (int i = 0; i < groups; i++)
			{
				int gOff = off + i * 12;
				if (gOff + 12 > _b.Length) break;
				uint sc  = (uint)BE32(gOff);
				uint ec  = (uint)BE32(gOff + 4);
				uint sg  = (uint)BE32(gOff + 8);
				for (uint cp = sc; cp <= ec; cp++)
					_cmap[cp] = sg + (cp - sc);
			}
		}
	}
}
