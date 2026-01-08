using System;
using System.Linq;
using System.Drawing;
using System.Collections.Generic;
using System.Text;
using KS.Foundation;

namespace SummerGUI
{
	public interface IGUIFont : IDisposable
	{		
		float Size { get; }
		float LineHeight { get; }
		float TextBoxHeight { get; }
		float CaptionHeight { get; }
		float Height { get; }
		float YOffset { get; set; }
		float LineSpacing { get; set; }

		float Ascender { get; }
		float Descender { get; }

		SizeF Measure(string text, int start = 0, int len = -1);
		SizeF Measure(string text, float width, FontFormat sf);
		SizeF MeasureMnemonicString (string text);

		bool ContainsChar (char c);
		float CharWidth (char c);
		int CharPos (string text, float cursorPos);

		SizeF Print(string text, RectangleF bounds, FontFormat format, Color color = default(Color));
		SizeF PrintSelectedString (string text, int selStart, int selLength, RectangleF bounds, float offsetX, FontFormat format, Color foreColor, Color selectionBackColor, Color selectionForeColor);
		void PrintTextLine (uint[] glyphs, RectangleF bounds, Color foreColor);

		GlyphChar GetGlyph (char c, SpecialCharacterFlags flags);

		void Begin();
		void End();

		void Rescale (float scaleFactor);
	}


	public static class SpecialCharacters
	{
		public static char Paragraph = (char)182;	// paragraph, alt. 9166 carriage return symbol
		public static char SpaceDot = (char)183;	// small dot, alt. 8226 bullet

		public static char EndOfText = (char)9670;	// 'black diamond' (rhombus), alt.: 9830, 11201, 11045, 8900
		public static char Tabulator = (char)8594;	// right arrow
		public static char Ellipsis = (char)8230;	// ellipsis
	}

	[Flags]
	public enum SpecialCharacterFlags
	{
		None = 0,
		WhiteSpace = 1,
		LineBreaks = 2,
		EndOfText = 4,
		Default = None,
		All = WhiteSpace + LineBreaks + EndOfText
	}

	[Flags]
	public enum CursorFlags
	{
		NoCursor = 0,
		VerticalLineCursor = 1,
		UnderLineCursor = 2,
		BlockCursor = 4,
		BlinkingCursor = 8,
		Default = 11,
		Classic = 14
	}

	// of course we love coloured text !
	// we can spare a large amount of memory when
	// having our own GlyphColor without alpha
	public struct GlyphColor : IEquatable<GlyphColor>
	{
		// glyph-colors are to define special colors
		// for glyph-colors, black is not a color.
		public static readonly GlyphColor Empty = new GlyphColor {
			R = 0,
			G = 0,
			B = 0
		};

		public byte R;
		public byte G;
		public byte B;

		public override bool Equals (object obj)
		{			
			return obj is GlyphColor && (Equals((GlyphColor)obj));
		}

		public bool Equals (GlyphColor other)
		{			
			return R == other.R	&& G == other.G	&& B == other.B;
		}

		public override int GetHashCode ()
		{
			return R + G + B + (R ^ G ^ B);
		}

		public static bool operator == (GlyphColor c1, GlyphColor c2)
		{
			return c1.Equals(c2);
		}

		public static bool operator != (GlyphColor c1, GlyphColor c2)
		{
			return !c1.Equals(c2);
		}

		public override string ToString ()
		{
			return string.Format ("[GlyphColor ({0}, {1}, {2})]", R, G, B);
		}
	}

	// When rendering long text or static content, 
	// we should use precompiled Glyph-Strings,
	// which can be quickly converted to unsigned-int arrays
	// for very fast output with GL.CallList and without any overhead.
	// this gives comparable speed to VertexBuffers, 
	// but in C# without stressing the garbage collector.

	public struct GlyphChar : IEquatable<GlyphChar>, IComparable<GlyphChar>
	{		
		public readonly char Char;
		public uint Glyph;
		public readonly int Width;	// this also helps for fast text-layout, word-wrapping, ..
		//public GlyphColor Color;

		public GlyphChar(char c, uint glyph, int width)
		{
			Char = c;
			Glyph = glyph;
			Width = width;
		}

		public static readonly GlyphChar Empty = new GlyphChar ((char)0, 0, 0);

		public override bool Equals (object obj)
		{			
			return obj is GlyphChar && Equals((GlyphChar)obj);
		}

		public bool Equals(GlyphChar other)
		{
			return Char == other.Char;
				//&& Glyph == other.Glyph
				//&& Width == other.Width;
		}

		public override int GetHashCode ()
		{
			unchecked {
				//return ((int)Char ^ 17) + ((int)Glyph ^ 23) + Width;	// + Color.GetHashCode ();
				return (int)Char;
			}
		}

		public static bool operator ==(GlyphChar c1, GlyphChar c2)
		{
			return c1.Equals (c2);
		}

		public static bool operator !=(GlyphChar c1, GlyphChar c2)
		{
			return !c1.Equals (c2);
		}

		public static bool operator <(GlyphChar c1, GlyphChar c2)
		{
			return c1.Char < c2.Char;
		}

		public static bool operator >(GlyphChar c1, GlyphChar c2)
		{
			return c1.Char > c2.Char;
		}

		public int CompareTo (GlyphChar other)
		{			
			return Char.CompareTo(other.Char);
		}

		public override string ToString ()
		{
			return string.Format ("[GlyphChar: '{0}']", Char);
		}
	}		

	// this class is NOT a common string replacement.
	// we don't want to support too many interfaces here, such as serialization and stuff,
	// because this is only used internally, to renderer text with fastest speed

	// Not completely finished yet. Not tested. Don't use.
	// the editor uses a linked list instead, so much simpler..

	internal class GlyphString
	{
		GlyphChar[] m_Glyphs;
		protected GlyphChar[] Glyphs 
		{ 
			get {
				return m_Glyphs;
			}
			set {
				m_Glyphs = value;
			}
		}			

		public GlyphChar this[int index]
		{
			get{
				return Glyphs [index];
			}
		}

		public int Length { get; protected set; }
		public int Width { get; protected set; }

		/// <summary>
		/// This will create an optimal short array without extra space
		/// </summary>
		/// <param name="source">Source.</param>
		/// <param name="font">Font.</param>
		/// <param name="flags">Flags.</param>
		public GlyphString(string source, IGUIFont font, SpecialCharacterFlags flags = SpecialCharacterFlags.None)
		{
			ParseString (source, font, flags);
		}
			
		public GlyphString()
		{
			m_Glyphs = new GlyphChar[0];
		}		

		// internally used for concatenation
		protected GlyphString(GlyphChar[] arr)
		{			
			m_Glyphs = arr;
		}
			
		/// <summary>
		/// We usually just pass this UINT-array to the renderer
		/// </summary>
		/// <returns>The render bytes.</returns>
		public uint[] ToRenderBytes()
		{
			if (Length == 0)
				return new uint[0];
			return Glyphs.Take(Length).Select (g => g.Glyph).ToArray();
		}

		public override string ToString ()
		{
			if (Length == 0)				
				return String.Empty;			
			return Glyphs.Select (g => g.Char).ToString();
		}

		public void ParseString(string text, IGUIFont font, SpecialCharacterFlags flags = SpecialCharacterFlags.None)
		{
			Length = 0;
			GlyphChar[] tmp = null;
			if (!String.IsNullOrEmpty(text) && font != null) {
				tmp = new GlyphChar[text.Length];
				int w = 0;
				for (int i = 0; i < text.Length; i++) {
					GlyphChar g = font.GetGlyph (text [i], flags);
					w += g.Width;
					tmp [i] = g;
				}
					
				Width = w;
				Length = text.Length;
			}

			if (tmp == null)
				tmp = new GlyphChar[0];

			if (m_Glyphs == null)	// called from constructor
				m_Glyphs = tmp;
			else
				Concurrency.LockFreeUpdate(ref m_Glyphs, tmp);
		}

		const int minextra = 31;	// minimum extra space to reserve

		/// <summary>
		/// This will reserve some extra free space for fast subsequent appends
		/// </summary>
		/// <returns><c>true</c>, if char was appended, <c>false</c> otherwise.</returns>
		/// <param name="c">The character to append</param>
		/// <param name="font">Font.</param>
		/// <param name="flags">Flags.</param>
		public int Append (char c, IGUIFont font, SpecialCharacterFlags flags = SpecialCharacterFlags.None)
		{
			if (c == '\r' || font == null)
				return 0;

			GlyphChar g = font.GetGlyph (c, flags);
			if (g.Glyph > 0) {
				try {
					int idx = 0;
					if (Length == 0)
						m_Glyphs = new GlyphChar[++Length];
					else {
						idx = Length++;
						if (idx >= m_Glyphs.Length) {
							Array.Resize (ref m_Glyphs, idx + Math.Max(minextra, Length / 3));
						}
					}
					Glyphs[idx] = g;
					Width += g.Width;
					return 1;
				} catch (Exception ex) {
					ex.LogError ();
				}					
			}
			return 0;
		}

		public int Append (string text, IGUIFont font, SpecialCharacterFlags flags = SpecialCharacterFlags.None)
		{
			int result = 0;
			for (int i = 0; i < text.Length; i++)
				result += Append (text [i], font, flags);
			return result;
		}

		public GlyphString SubString(int start)
		{
			return SubString (this, start, Length);
		}

		public GlyphString SubString(int start, int len)
		{
			return SubString (this, start, len);
		}			

		public int Insert(int index, char c, IGUIFont font, SpecialCharacterFlags flags = SpecialCharacterFlags.None)
		{
			if (c == '\r' || font == null)
				return 0;

			// we must handle 3 cases:
			// (1) insert at index 0, same implementation as (2) would lead to array-copy errors
			// (2) insert in the middle
			// (3) append at the end, same as with (1)

			if (index >= Length)
				return Append (c, font, flags);
						
			// first see, if we get a valid Glyph
			GlyphChar glyph	= font.GetGlyph(c, flags);
			if (glyph.Glyph == 0)
				return 0;
				
			int newLen = Glyphs.Length;
			if (Length + 1 >= newLen)
				newLen += minextra;

			GlyphChar[] arr = new GlyphChar[newLen];
			if (index <= 0) {
				Array.Copy (Glyphs, arr, 1);
				Glyphs [0] = glyph;
			} else {
				Array.Copy (Glyphs, 0, arr, 0, index);
				Glyphs [index] = glyph;
				Array.Copy (Glyphs, index + 1, arr, index, Length - index);
			}

			Length++;
			Width += glyph.Width;

			return 1;
		}

		public int Insert(int index, string text, IGUIFont font, SpecialCharacterFlags flags = SpecialCharacterFlags.None)
		{
			if (String.IsNullOrEmpty (text))
				return 0;

			// we must handle 3 cases:
			// (1) insert at index 0, same implementation as (2) would lead to array-copy errors
			// (2) insert in the middle
			// (3) append at the end, same as with (1)

			if (index >= Length)
				return Append (text, font, flags);

			List<GlyphChar> glyphs = new List<GlyphChar> (text.Length);
			for (int i = 0; i < text.Length; i++) {
				GlyphChar g = font.GetGlyph(text [i], flags);
				if (g.Glyph != 0)
					glyphs.Add (g);
			}

			if (glyphs.Count == 0)
				return 0;

			GlyphChar[] source = glyphs.ToArray ();
			int sourceLen = source.Length;

			int newLen = Glyphs.Length + sourceLen + 1;

			GlyphChar[] arr = new GlyphChar[newLen];
			if (index <= 0) {
				Array.Copy (source, arr, sourceLen);
				Array.Copy (Glyphs, 0, arr, sourceLen, Length);
			} else {
				Array.Copy (Glyphs, 0, arr, 0, index);
				Array.Copy (source, index, arr, 0, sourceLen);
				Array.Copy (Glyphs, index + sourceLen, arr, index, Length - index);
			}

			Length += sourceLen;
			Width += source.Sum(p => p.Width);

			return sourceLen;
		}

		public static GlyphString operator + (GlyphString g1, GlyphString g2)
		{
			return Concat (g1, g2);
		}

		public static bool operator == (GlyphString g1, GlyphString g2)
		{
			if (g1 == null && g2 == null)
				return true;
			if (g1 == null || g2 == null)
				return false;
			if (g1.Length != g2.Length || g1.Width != g2.Width)
				return false;
			for (int i = 0; i < g1.Length; i++) {
				if (g1.Glyphs [i].Char != g2.Glyphs [i].Char)
					return false;
			}
			return true;
		}

		public static bool operator != (GlyphString g1, GlyphString g2)
		{
			return !(g1 == g2);
		}

		public override bool Equals (object obj)
		{
			return (GlyphString)obj == this;
		}

		public override int GetHashCode ()
		{			
			return Glyphs.GetHashCode ();
		}			

		public static GlyphString SubString(GlyphString g, int start, int length)
		{
			if (g == null)
				return null;
			length = Math.Min (length, g.Length - (length - start + 1));
			if (length <= 0)
				return new GlyphString();

			GlyphChar[] arr = new GlyphChar[length];
			Array.Copy (g.Glyphs, arr, length);

			GlyphString result = new GlyphString (arr);
			result.Length = length;
			result.Width = arr.Sum(p => p.Width);
			return result;
		}

		public static GlyphString Concat(GlyphString g1, GlyphString g2)
		{
			if (g1 == null)
				return g2;
			if (g2 == null)
				return g1;

			int concatLen = g1.Length + g2.Length;
			GlyphChar[] arr = new GlyphChar[concatLen];
			Array.Copy (g1.Glyphs, arr, g1.Length);

			// Attention: Very confusing parameter order in the 
			// many overloads for the System.Array functions
			// here we use: SourceArray, SourceIndex, DestArray, DestIndex, DestLength (hopefully)
			Array.Copy (g2.Glyphs, 0, arr, g1.Length, g2.Length);

			GlyphString result = new GlyphString (arr);
			result.Length = concatLen;
			result.Width = g1.Width + g2.Width;
			return result;
		}
	}

}

