using System;

namespace SummerGUI
{
	public interface IDrawingObject
	{
	}

	/// <summary>
	/// The dash families for <see cref="Pen"/> — names and numeric values are 1:1
	/// compatible with <see cref="System.Drawing.Drawing2D.DashStyle"/> (WinForms).
	/// SummerGUI declares it in-house (no System.Drawing.Common reference) so it works
	/// on every platform.
	/// </summary>
	public enum DashStyle
	{
		Solid = 0,
		Dash = 1,
		Dot = 2,
		DashDot = 3,
		DashDotDot = 4,
		Custom = 5
	}

	/// <summary>
	/// The 53 fill textures for <see cref="HatchBrush"/> — names and numeric values are
	/// 1:1 compatible with <see cref="System.Drawing.Drawing2D.HatchStyle"/> (WinForms).
	/// </summary>
	public enum HatchStyle
	{
		Horizontal = 0,
		Vertical = 1,
		ForwardDiagonal = 2,
		BackwardDiagonal = 3,
		Cross = 4,
		DiagonalCross = 5,
		Percent05 = 6,
		Percent10 = 7,
		Percent20 = 8,
		Percent25 = 9,
		Percent30 = 10,
		Percent40 = 11,
		Percent50 = 12,
		Percent60 = 13,
		Percent70 = 14,
		Percent75 = 15,
		Percent80 = 16,
		Percent90 = 17,
		LightDownwardDiagonal = 18,
		LightUpwardDiagonal = 19,
		DarkDownwardDiagonal = 20,
		DarkUpwardDiagonal = 21,
		WideDownwardDiagonal = 22,
		WideUpwardDiagonal = 23,
		LightVertical = 24,
		LightHorizontal = 25,
		NarrowVertical = 26,
		NarrowHorizontal = 27,
		DarkVertical = 28,
		DarkHorizontal = 29,
		DashedDownwardDiagonal = 30,
		DashedUpwardDiagonal = 31,
		DashedHorizontal = 32,
		DashedVertical = 33,
		SmallConfetti = 34,
		LargeConfetti = 35,
		ZigZag = 36,
		Wave = 37,
		DiagonalBrick = 38,
		HorizontalBrick = 39,
		Weave = 40,
		Plaid = 41,
		Divot = 42,
		DottedGrid = 43,
		DottedDiamond = 44,
		Shingle = 45,
		Trellis = 46,
		Sphere = 47,
		SmallGrid = 48,
		SmallCheckerBoard = 49,
		LargeCheckerBoard = 50,
		OutlinedDiamond = 51,
		SolidDiamond = 52,

		LargeGrid = Cross,
		Min = Horizontal,
		Max = SolidDiamond
	}

	/// <summary>
	/// Zeichnet eine Linie oder einen Rahmen um eine Form.
	/// Mirrors WinForms <c>Pen</c>: <see cref="DashStyle"/> picks the dash family;
	/// for <see cref="DashStyle.Custom"/> <see cref="DashPattern"/> supplies the pattern
	/// (even entries = dashes, odd entries = gaps, both in multiples of the pen width —
	/// exactly like GDI+'s <c>DashPattern</c>).
	/// </summary>
	public class Pen : IDrawingObject, IDisposable
	{
		public Pen(System.Drawing.Color color) : this(color, 1f, DashStyle.Solid, null) { }
		public Pen(System.Drawing.Color color, float width) : this(color, width, DashStyle.Solid, null) { }
		public Pen(System.Drawing.Color color, float width, DashStyle dashStyle) : this(color, width, dashStyle, null) { }
		public Pen(System.Drawing.Color color, float width, DashStyle dashStyle, float[] dashPattern)
		{
			Color = color;
			Width = Math.Max(1f, width);
			DashStyle = dashStyle;
			if (dashStyle == DashStyle.Custom && (dashPattern == null || dashPattern.Length == 0))
				throw new ArgumentException("A non-empty DashPattern is required for DashStyle.Custom", nameof(dashPattern));
			DashPattern = dashPattern;
		}

		public System.Drawing.Color Color { get; set; }
		public float Width { get; set; }
		public DashStyle DashStyle { get; set; }

		/// <summary>
		/// The dash pattern in multiples of <see cref="Width"/> — used only for
		/// <see cref="DashStyle.Custom"/>. Even-indexed entries are dashes, odd ones are gaps.
		/// </summary>
		public float[] DashPattern { get; set; }

		public void Dispose()
		{
			// do nothing
			GC.SuppressFinalize(this);
		}
	}

	public abstract class Brush : IDrawingObject, IDisposable
	{
		protected Brush(System.Drawing.Color color)
		{
			Color = color;
		}

		public System.Drawing.Color Color { get; set; }

		public virtual void Dispose()
		{
			// do nothing
			GC.SuppressFinalize(this);
		}
	}

	public class SolidBrush : Brush
	{
		public SolidBrush(System.Drawing.Color color)
			: base(color)
		{
		}
	}

	public enum GradientDirections
	{
		Horizontal,
		Vertical,
		ForwardDiagonal,
		BackwardDiagonal,
		TopLeft
	}

	public class LinearGradientBrush : Brush
	{
		public System.Drawing.Color GradientColor { get; set; }
		public GradientDirections Direction { get; set; }

		public LinearGradientBrush(System.Drawing.Color color, System.Drawing.Color gradientColor)
			: this(color, gradientColor, GradientDirections.Horizontal) { }

		public LinearGradientBrush(System.Drawing.Color color, System.Drawing.Color gradientColor, GradientDirections direction)
			: base(color)
		{
			GradientColor = gradientColor;
			Direction = direction;
		}
	}

	/// <summary>
	/// A two-coloured hatch texture brush. <see cref="Color"/> is the base/background fill,
	/// <see cref="HatchColor"/> is the motif (bars/dots) colour — the same convention as
	/// WinForms <c>HatchBrush</c> (backing color + hatch color).
	/// </summary>
	public class HatchBrush : Brush
	{
		public HatchStyle HatchStyle { get; set; }

		/// <summary>Motif (stroke) colour drawn on top of <see cref="Color"/>.</summary>
		public System.Drawing.Color HatchColor { get; set; }

		/// <summary>
		/// Creates the brush. <paramref name="background"/> fills the area behind the motif,
		/// <paramref name="hatchColor"/> colours the motif itself.
		/// </summary>
		public HatchBrush(HatchStyle hatchStyle, System.Drawing.Color background, System.Drawing.Color hatchColor)
			: base(background)
		{
			HatchStyle = hatchStyle;
			HatchColor = hatchColor;
		}

		/// <summary>
		/// Backwards-compatible name-based ctor (old SummerGUI HatchBrush took a string).
		/// The name is resolved against the <see cref="HatchStyle"/> enum (e.g. "LargeConfetti").
		/// Unknown names fall back to <see cref="HatchStyle.LargeConfetti"/>.
		/// </summary>
		public HatchBrush(string hatchStyleName, System.Drawing.Color background, System.Drawing.Color hatchColor)
			: this(ResolveName(hatchStyleName), background, hatchColor)
		{
		}

		static HatchStyle ResolveName(string name)
		{
			try
			{
				return (HatchStyle)Enum.Parse(typeof(HatchStyle), name, true);
			}
			catch (Exception)
			{
				return HatchStyle.LargeConfetti; // sensibler Fallback, war der alte Standard
			}
		}
	}
}
