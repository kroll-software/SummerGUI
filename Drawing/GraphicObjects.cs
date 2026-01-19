using System;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace SummerGUI
{
	public interface IDrawingObject
	{
	}

	public enum LineStyles
	{
		Solid,
		Dotted,
		Dashed,
		DashDot
	}

	public class Pen : IDrawingObject, IDisposable
	{
		public Pen(System.Drawing.Color color) : this(color, 1, LineStyles.Solid) {}
		public Pen(System.Drawing.Color color, float width) : this(color, width, LineStyles.Solid) {}
		public Pen(System.Drawing.Color color, float width, LineStyles style)
		{
			Color = color;
			Width = width;
			LineStyle = style;
		}

		public System.Drawing.Color Color { get; set; }
		public float Width { get; set; }
		public LineStyles LineStyle { get; set; }		

		public void Dispose()
		{
			// do nothing
			GC.SuppressFinalize (this);
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
			GC.SuppressFinalize (this);
		}
	}			

	public class SolidBrush : Brush
	{
		public SolidBrush(System.Drawing.Color color)
			: base(color)
		{
			Color = color;
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
		public GradientDirections Direction  { get; set; }

		public LinearGradientBrush(System.Drawing.Color color, System.Drawing.Color gradientColor) : this(color, gradientColor, GradientDirections.Horizontal) {}
		public LinearGradientBrush(System.Drawing.Color color, System.Drawing.Color gradientColor, GradientDirections direction)
			: base(color)
		{
			GradientColor = gradientColor;
			Direction = direction;
		}
	}

	public class HatchBrush : Brush
	{
		public string HatchStyle { get; set; }

		public HatchBrush(string hatchStyle, System.Drawing.Color color, System.Drawing.Color hatchColor)
			: base(color)
		{
			HatchStyle = hatchStyle;
		}
	}
}

