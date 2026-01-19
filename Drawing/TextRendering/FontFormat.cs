using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using Pfz.Collections;	// TreadSafeDictionary
using KS.Foundation;
//using SharpFont;

namespace SummerGUI
{
	[Flags]
	public enum FontFormatFlags
	{
		None = 0,
		WrapText = 1,
		Elipsis = 2,
		Mnemonics = 4,
		Underline = 8
	}

	public struct FontFormat : IEquatable<FontFormat>
	{
		public static readonly FontFormat DefaultSingleLine = new FontFormat (Alignment.Near, Alignment.Center, FontFormatFlags.Elipsis);
		public static readonly FontFormat DefaultSingleBaseLine = new FontFormat (Alignment.Near, Alignment.Baseline, FontFormatFlags.None);
		public static readonly FontFormat DefaultSingleBaseLineCentered = new FontFormat (Alignment.Center, Alignment.Baseline, FontFormatFlags.None);
		public static readonly FontFormat DefaultSingleLineCentered = new FontFormat (Alignment.Center, Alignment.Center, FontFormatFlags.Elipsis);
		public static readonly FontFormat DefaultSingleLineFar = new FontFormat (Alignment.Far, Alignment.Center, FontFormatFlags.None);
		public static readonly FontFormat DefaultMultiLine = new FontFormat (Alignment.Near, Alignment.Near, FontFormatFlags.WrapText);
		public static readonly FontFormat DefaultMultiLineVerticalCentered = new FontFormat (Alignment.Near, Alignment.Center, FontFormatFlags.WrapText);
		public static readonly FontFormat DefaultMultiLineCentered = new FontFormat (Alignment.Center, Alignment.Center, FontFormatFlags.WrapText);
		public static readonly FontFormat DefaultMnemonicLine = new FontFormat (Alignment.Near, Alignment.Center, FontFormatFlags.Mnemonics);
		public static readonly FontFormat DefaultMnemonicLineCentered = new FontFormat (Alignment.Center, Alignment.Center, FontFormatFlags.Mnemonics);
		public static readonly FontFormat DefaultIconFontFormatLeft = new FontFormat (Alignment.Near, Alignment.Center, FontFormatFlags.None);
		public static readonly FontFormat DefaultIconFontFormatCenter = new FontFormat (Alignment.Center, Alignment.Center, FontFormatFlags.None);
		public static readonly FontFormat DefaultIconFontFormatFar = new FontFormat (Alignment.Far, Alignment.Center, FontFormatFlags.None);

		public readonly Alignment HAlign;
		public readonly Alignment VAlign;
		public readonly FontFormatFlags Flags;

		public bool HasFlag(FontFormatFlags flag)
		{
			return Flags.HasFlag (flag);
		}

		public FontFormat(Alignment halign, Alignment valign, FontFormatFlags flags = FontFormatFlags.None)
		{
			HAlign = halign;
			VAlign = valign;
			Flags = flags;
		}

		public override int GetHashCode ()
		{
			return Flags.GetHashCode().CombineHash(VAlign.GetHashCode(), HAlign.GetHashCode());
		}

		public static int GetHashCode(FontFormat value)
		{
			return value.GetHashCode ();
		}

		public override bool Equals (object obj)
		{
			return Equals ((FontFormat)obj);
		}

		public bool Equals(FontFormat other)
		{
			return Flags == other.Flags && HAlign == other.HAlign && VAlign == other.VAlign;
		}

		public static bool operator ==(FontFormat f1, FontFormat f2)
		{
			return f1.Equals (f2);
		}

		public static bool operator !=(FontFormat f1, FontFormat f2)
		{
			return !(f1.Equals (f2));
		}
	}
		
	public static class BoxAlignment
	{
		/// <summary>
		/// Return the upper-left corner
		/// </summary>
		/// <returns>The boxes.</returns>
		/// <param name="rContent">R content.</param>
		/// <param name="rBoundingBox">R bounding box.</param>
		/// <param name="SF">S.</param>
		/// <param name="Ascender">Ascender.</param>
		/// <param name="Descender">Descender.</param>
		public static PointF AlignBoxes(RectangleF rContent, RectangleF rBoundingBox, FontFormat SF, float Ascender, float Descender)
		{
			if (rContent.Equals (Rectangle.Empty) || rBoundingBox.Equals (Rectangle.Empty))
				return Point.Empty;

			float x = 0;
			float y = 0;

			switch (SF.HAlign) {
			case Alignment.Near:					
				//x = 0;
				break;

			case Alignment.Center:
				x = MathF.Max(0, (rBoundingBox.Width - rContent.Width) / 2f);
				break;

			case Alignment.Far:
				x = rBoundingBox.Width - rContent.Width;
				break;
			}

			switch (SF.VAlign) {
			case  Alignment.Near:
				//y = 0;
				break;

			case Alignment.Center:
				y = MathF.Max(0, (rBoundingBox.Height - rContent.Height) / 2f);
				break;

			case Alignment.Baseline:
				y = MathF.Max(0, (rBoundingBox.Height - (rContent.Height + Descender)) / 2f);
				break;

			case Alignment.Far:
				y = rBoundingBox.Height - rContent.Height;
				break;
			}
			
			return new PointF (x, y);
		}
	}
}

