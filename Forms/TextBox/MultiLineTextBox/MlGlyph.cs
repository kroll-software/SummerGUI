using System;
using System.Text;
using KS.Foundation;

namespace SummerGUI.Editor
{
	/// <summary>
	/// Widget-local glyph model used by <see cref="MultiLineTextBox"/> only.
	///
	/// Why a separate struct from the framework's <c>GlyphChar</c> (SummerGUI):
	/// <c>GlyphChar.Char</c> is a <see cref="char"/>, i.e. a single UTF-16 code
	/// unit.  Astral characters (em-emojis, CJK ext-B…) are encoded as UTF-16
	/// surrogate pairs, so they cannot be represented in a single <see cref="char"/>
	/// without breaking.  This struct stores the Unicode **code point** as a
	/// signed <see cref="int"/>; the rest of the layout (a single <see cref="float"/>)
	/// keeps the structure at 8 bytes on 64-bit targets, the same as
	/// <c>GlyphChar</c> — so long documents don't see more memory pressure
	/// than before.
	///
	/// The framework (<c>GlyphChar</c>, <c>IGUIFont</c>, <c>SpecialCharacters</c>)
	/// is intentionally NOT touched; this struct exists only in
	/// <c>Forms/TextBox/MultiLineTextBox/</c>  and only the MultiLineTextBox
	/// widget consumes it.
	/// </summary>
	public struct MlGlyph
	{
		/// <summary>The Unicode code point (<c>0..0x10FFFF</c>), or <c>0</c> for "empty".</summary>
		public int Char;

		/// <summary>Layout advance width (pixels at the current font size).</summary>
		public readonly float Advance;

		public MlGlyph(int cp, float advance)
		{
			Char = cp;
			Advance = advance;
		}

		public static readonly MlGlyph Empty = new MlGlyph(0, 0f);

		/// <summary>True if the struct represents "no character" (0).</summary>
		public static bool IsEmpty(MlGlyph g)
		{
			return g.Char == 0;
		}
	}
}
