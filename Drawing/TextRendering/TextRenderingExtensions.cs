using System;
using System.Linq;
using KS.Foundation;

namespace SummerGUI
{
	public static class TextRenderingExtensions
	{
		public static bool IsValid(this GlyphFilterFlags filter, char c)
		{
			//return true;
			// simple stupid but shortest and fastest solution..

			if (filter <= GlyphFilterFlags.OnDemand)
				return true;

			if (filter.HasFlag (GlyphFilterFlags.Lower) && !Char.IsLower (c))
				return false;
			if (filter.HasFlag(GlyphFilterFlags.Upper) && !Char.IsUpper(c))
				return false;
			if (filter.HasFlag(GlyphFilterFlags.Symbol) && Char.IsLetterOrDigit(c))
				return false;

			bool hasFilter = false;
			if (filter.HasFlag(GlyphFilterFlags.Punctation)) {
				hasFilter = true;
				if (c.IsPunctation())
					return true;
			}

			if (filter.HasFlag(GlyphFilterFlags.Numeric)) {
				hasFilter = true;
				if (Char.IsNumber (c))
					return true;
			}

			if (filter.HasFlag(GlyphFilterFlags.Alpha)) {
				hasFilter = true;
				if (Char.IsLetter(c))
					return true;
			}

			if (filter.HasFlag(GlyphFilterFlags.Ascii)) {
				hasFilter = true;
				if (c.ToString().Is7Bit())
					return true;
			}

			return !hasFilter;				
		}
	}
}

