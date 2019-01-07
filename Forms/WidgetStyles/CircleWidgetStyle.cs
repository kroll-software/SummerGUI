using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using KS.Foundation;

namespace SummerGUI
{
	public class CircleWidgetStyle : WidgetStyle
	{
		public override void InitStyle ()
		{
			SetBackColor (Theme.Colors.Base00);
			SetForeColor (Color.Empty);
			SetBorderColor (Color.Empty);
		}

		public override void PaintBackground (IGUIContext ctx, Widget widget)
		{
			RectangleF bounds = widget.Bounds;
			bounds.Offset (-1, -1);
			float radius = bounds.Width / 2f;

			// this is not the final wisdom..
			//PointF center = new PointF (bounds.Left + radius - widget.Margin.Left, bounds.Top + radius - widget.Margin.Top);
			PointF center = new PointF (bounds.Left + radius, bounds.Top + radius);
			radius -= 2f;

			if (BackColorBrush.Color != Color.Empty) {				
				ctx.FillCircle (BackColorBrush, center.X, center.Y, radius);
			}

			if (BorderColorPen.Color != Color.Empty && BorderColorPen.Width > 0) {
				ctx.DrawCircle (BorderColorPen, center.X, center.Y, radius);
			}
		}
	}

	public class CircleWidgetHoverStyle : CircleWidgetStyle
	{
		public override void InitStyle ()
		{
			SetBackColor (Theme.Colors.Base01);
			SetForeColor (Color.Empty);
			SetBorderColor (Color.Empty);
		}
	}

	public class CircleWidgetPressedStyle : CircleWidgetStyle
	{
		public override void InitStyle ()
		{
			SetBackColor (Theme.Colors.Base02);
			SetForeColor (Color.Empty);
			SetBorderColor (Color.Empty);
		}
	}

	public class CircleWidgetDisabledStyle : CircleWidgetStyle
	{
		public override void InitStyle ()
		{
			SetBackColor (Theme.Colors.Base0);
			SetForeColor (Color.Empty);
			SetBorderColor (Color.Empty);
		}
	}
}

