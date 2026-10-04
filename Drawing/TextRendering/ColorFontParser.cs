using System;
using System.Collections.Generic;
using System.IO;

namespace SummerGUI
{
	/// <summary>
	/// Parsed color-bitmap glyph entry.
	/// TopBearing = distance from baseline up to the top of the bitmap
	///             (positive means the glyph's top is ABOVE the baseline in screen Y-down coordinates).
	/// </summary>
	public readonly struct ColorGlyphEntry
	{
		public byte[] PngData { get; }          // raw PNG bytes from CBDT (format 17)
		public int Width { get; }
		public int Height { get; }
		public int Advance { get; }
		public int LeftBearing { get; }         // horizontal bearing (bearX from CBDT)
		public int TopBearing { get; }          // vertical bearing (bearY from CBDT); destY = baseline - TopBearing

		public ColorGlyphEntry(byte[] png, int w, int h, int adv, int lB, int topB)
		{
			PngData = png; Width = w; Height = h;
			Advance = adv; LeftBearing = lB; TopBearing = topB;
		}
	}

	/// <summary>
	/// Parses OpenType CBDT/CBLC color-bitmap tables (imageFormat 17 = embedded PNG).
	/// Supports Noto Color Emoji (Linux), Apple Color Emoji (macOS), Segoe UI Emoji (Windows)
	/// — all use the same CBDT+CBLC binary layout with imageFormat=17.
	/// Only parses on demand; no GPU/GL resources needed here.
	/// </summary>
	public sealed class ColorFontParser
	{
		// table offsets (ABsolute file offsets within _fontBytes)
		readonly byte[] _fontBytes;
		readonly int    _face0;         // Start des sfnt-Records: 0 = Einzel-Sfnt, sonst Face-Offset einer TTC
		int    _cbdtOff;       // absolute
		int    _cbdtLen;       // CBDT table size
		int    _cblcOff;       // absolute

		// parsed strike 0
		int    _numIdxSub;
		uint[] _subFirst;
		uint[] _subLast;
		int[]  _subAddOff;
		ushort _subIdxFmt;
		ushort _subImgFmt;
		int[]  _subImgOff;  // relative to CBDT table start
		int[][] _subOffs;   // u32 offset arrays (indexFormat 1) or null

		// cmap: codepoint (uint) -> glyph index (uint)
		readonly Dictionary<uint, uint> _cmap = new();

		public ColorFontParser(string fontPath)
		{
			if (!File.Exists(fontPath))
				throw new FileNotFoundException("Emoji font not found: " + fontPath, fontPath);

			_fontBytes = File.ReadAllBytes(fontPath);
			if (_fontBytes.Length < 0x20)
				throw new InvalidDataException("Font file too small: " + fontPath);

			// TrueType Collection (ttcf): "ttcf" | version(u32) | numFonts(u32)
			// | faceOffsets(u32 numFonts). Die echte Apple Color Emoji.ttc ist
			// immer eine solche Collection.
			//
			// WICHTIG: In einer .ttc sind die Tabellen-Offsen in der
			// Tabellen-Direktor ABSOLUT (relativ zum Datei-Start), NICHT
			// face-relativ. (Verifiziert: CBDT.off + CBDT.len == CBLC.off —
			// die Tables liegen kontiguiert und sind datei-absolute referenziert.)
			// Deshalb NICHT die Face-Bytes rauskopieren (das würde jede
			// Tabellen-Bereferenz um face0 verschieben), sondern nur die
			// sfnt-Header-/Direktor-Lesestellen an die Face-Position anbinden.
			// Für ein Einzel-Sfnt ist _face0 = 0 (alter Codepfad unverändert).
			_face0 = 0;
			if (_fontBytes.Length >= 20
				&& _fontBytes[0] == (byte)'t' && _fontBytes[1] == (byte)'t'
				&& _fontBytes[2] == (byte)'c' && _fontBytes[3] == (byte)'f')
			{
				int firstFace = (_fontBytes[12] << 24) | (_fontBytes[13] << 16) | (_fontBytes[14] << 8) | _fontBytes[15];
				if (firstFace < 12 || firstFace + 0x20 > _fontBytes.Length)
					throw new InvalidDataException("Invalid TrueType Collection layout: " + fontPath);
				_face0 = firstFace;
			}

			ParseSfnt();
			ParseCmap();
			ParseCBLC();

			if (_subImgFmt != 17)
				throw new InvalidDataException($"Unsupported emoji font: CBDT imageFormat={_subImgFmt} (expected 17=PNG). Font: {fontPath}");
		}

		/// <summary>True if <paramref name="cp"/> has a CBDT-17 (PNG) glyph.</summary>
		public bool HasGlyph(uint cp)
		{
			return TryGetGlyph(cp, out _);
		}

		/// <summary>
		/// True if <paramref name="cp"/> has a CBDT-17 (PNG) glyph.
		/// <paramref name="cp"/> is a Unicode code point (0x0000 – 0x10FFFF).
		/// </summary>
		public bool TryGetGlyph(uint cp, out ColorGlyphEntry entry)
		{
			entry = default;
			if (!_cmap.TryGetValue(cp, out uint gid))
				return false;

			// find the subtable that contains this glyph
			for (int k = 0; k < _numIdxSub; k++)
			{
				if (gid < _subFirst[k] || gid > _subLast[k]) continue;

				int pos = (int)(gid - _subFirst[k]);
				if (_subOffs[k] == null || pos + 1 >= _subOffs[k].Length)
					return false;

				// CBDT imageFormat 17 layout:
				//   SmallGlyphMetrics (5 bytes): height(u8) width(u8) bearingX(i8) bearingY(i8) advance(u8)
				//   u32 dataLen
				//   PNG bytes
				int start  = _subImgOff[k] + _subOffs[k][pos];
				int end    = _subImgOff[k] + _subOffs[k][pos + 1];
				int length = end - start;
				if (length <= 9 || start < 0 || start + length > _cbdtLen)
					return false;

				byte[] raw = new byte[length];
				Buffer.BlockCopy(_fontBytes, _cbdtOff + start, raw, 0, length);

				int height    = raw[0];
				int width     = raw[1];
				int bearingX  = unchecked((sbyte)raw[2]);
				int bearingY  = unchecked((sbyte)raw[3]);
				int advance   = raw[4];
				int pngLen    = length - 9;   // skip 5 metrics + 4 u32
				byte[] png    = new byte[pngLen];
				Buffer.BlockCopy(raw, 9, png, 0, pngLen);

				entry = new ColorGlyphEntry(png, width, height, advance, bearingX, bearingY);
				return true;
			}
			return false;
		}

		/// <summary>Number of characters in the font cmap (useful for diagnostics).</summary>
		public int CharCount => _cmap.Count;

		// ---- internals ----

		static int BE16(byte[] b, int o) => (b[o] << 8) | b[o + 1];
		static int BE32(byte[] b, int o) => (b[o] << 24) | (b[o+1] << 16) | (b[o+2] << 8) | b[o+3];

		void ParseSfnt()
		{
			int numTables = BE16(_fontBytes, _face0 + 4);
			for (int i = 0; i < numTables; i++)
			{
				int o = _face0 + 12 + i * 16;
				string tag = System.Text.Encoding.ASCII.GetString(_fontBytes, o, 4);
				if (tag == "CBDT") { _cbdtOff = BE32(_fontBytes, o+8); _cbdtLen = BE32(_fontBytes, o+12); }
				else if (tag == "CBLC") _cblcOff = BE32(_fontBytes, o+8);
			}
			if (_cbdtOff == 0 || _cblcOff == 0)
				throw new InvalidDataException("Font is missing CBDT or CBLC tables.");
		}

		void ParseCmap()
		{
			int numTables = BE16(_fontBytes, _face0 + 4);
			int cmapOff = 0;
			for (int i = 0; i < numTables; i++)
			{
				int o = _face0 + 12 + i * 16;
				string tag = System.Text.Encoding.ASCII.GetString(_fontBytes, o, 4);
				if (tag == "cmap") { cmapOff = BE32(_fontBytes, o + 8); break; }
			}
			if (cmapOff == 0) return;

			int numSub = BE16(_fontBytes, cmapOff + 2);
			for (int i = 0; i < numSub; i++)
			{
				int subRef = BE32(_fontBytes, cmapOff + 4 + i*8 + 4);  // offset from cmap table start
				int abs = cmapOff + subRef;
				int fmt = BE16(_fontBytes, abs);

				if (fmt == 4) ParseCmap4(abs);
				else if (fmt == 12) ParseCmap12(abs);
			}
		}

		void ParseCmap4(int abs)
		{
			int segCount = BE16(_fontBytes, abs + 2) / 2;
			int startCodeOff = abs + 6 + segCount * 2;   // skip endCodes[segCount] + reservedPad
			int idDeltaOff   = startCodeOff + segCount * 2;
			int idRangeOff   = idDeltaOff + segCount * 2;

			for (int s = 0; s < segCount; s++)
			{
				int endC   = BE16(_fontBytes, abs + 6 + s*2);
				int startC = BE16(_fontBytes, startCodeOff + s*2);
				int idDl   = BE16(_fontBytes, idDeltaOff + s*2);
				int idRf   = BE16(_fontBytes, idRangeOff + s*2);

				for (int c = startC; c <= endC && c != 0xFFFF; c++)
				{
					uint g;
					if (idRf == 0)
						g = (uint)(c + idDl) & 0xFFFF;
					else
					{
						int idAbs = idRangeOff + s*2 + idRf + (c - startC)*2;
						int raw = BE16(_fontBytes, idAbs);
						g = raw == 0 ? 0 : ((uint)(raw + idDl) & 0xFFFF);
					}
					if (g > 0) _cmap[(uint)c] = g;
				}
			}
		}

		void ParseCmap12(int abs)
		{
			int nGroups = BE32(_fontBytes, abs + 12);
			int off = abs + 16;
			for (int g = 0; g < nGroups; g++)
			{
				uint sc = (uint)BE32(_fontBytes, off + g*12);
				uint ec = (uint)BE32(_fontBytes, off + g*12 + 4);
				uint sg = (uint)BE32(_fontBytes, off + g*12 + 8);
				for (uint cp = sc; cp <= ec; cp++)
					_cmap[cp] = sg + (cp - sc);
			}
		}

		void ParseCBLC()
		{
			int numSizes = BE32(_fontBytes, _cblcOff + 4);
			if (numSizes < 1) throw new InvalidDataException("CBLC has no strikes.");

			int b = _cblcOff + 8;  // first bitmapSizeTable
			int idxSubArrOff  = BE32(_fontBytes, b);      // relative to CBLC table start
			_numIdxSub        = BE32(_fontBytes, b + 8);
			if (_numIdxSub < 1) throw new InvalidDataException("CBLC strike has no index subtables.");

			int arrAbs = _cblcOff + idxSubArrOff;
			_subFirst  = new uint[_numIdxSub];
			_subLast   = new uint[_numIdxSub];
			_subAddOff = new int[_numIdxSub];
			_subOffs   = new int[_numIdxSub][];
			_subImgOff = new int[_numIdxSub];
			_subIdxFmt = 0;
			_subImgFmt = 0;

			for (int k = 0; k < _numIdxSub; k++)
			{
				int e = arrAbs + k * 8;
				uint first  = (uint)BE16(_fontBytes, e);
				uint last   = (uint)BE16(_fontBytes, e + 2);
				int  addOff = BE32(_fontBytes, e + 4);    // relative to CBLC table start

				int sub = _cblcOff + idxSubArrOff + addOff;
				ushort iFmt  = (ushort)BE16(_fontBytes, sub);
				ushort mFmt  = (ushort)BE16(_fontBytes, sub + 2);
				int  imgOff  = BE32(_fontBytes, sub + 4); // relative to CBDT table start

				_subFirst[k]  = first;
				_subLast[k]   = last;
				_subAddOff[k] = addOff;
				_subIdxFmt    = iFmt;
				_subImgFmt    = mFmt;
				_subImgOff[k] = imgOff;

				if (iFmt == 1)
				{
					int n  = (int)(last - first) + 1;
					var offs = new int[n + 1];
					int oa = sub + 8;
					for (int j = 0; j < n + 1; j++)
						offs[j] = BE32(_fontBytes, oa + j*4);
					_subOffs[k] = offs;
				}
			}
		}
	}
}
