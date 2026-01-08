using System;
using System.Drawing;

namespace SummerGUI
{
	public class TabContainerStyle : WidgetStyle
	{
		public override void InitStyle ()
		{			
			SetBackColor (Color.Empty);
			SetForeColor (Color.Empty);
			SetBorderColor (Color.Empty);
		}
	}

	public class TabBarStyle : GradientWidgetStyle
	{
		public override void InitStyle ()
		{
			SetBackColor (Theme.Colors.Base01, Theme.Colors.Base02);
			SetForeColor (Theme.Colors.Base3);
			SetBorderColor (Theme.Colors.Base03);
		}
			
		public override void DrawBorder (IGUIContext ctx, Widget widget)
		{
			if (BorderColorPen.Width > 0 && BorderColorPen.Color != Color.Empty) {
				RectangleF rBorder = widget.Bounds;
				if (Math.Abs(BorderDistance) > float.Epsilon)
					rBorder.Inflate (BorderDistance, BorderDistance);

				float top = rBorder.Top.Ceil() + 0.5f;
				ctx.DrawLine (Theme.Pens.Base01, rBorder.Left, top, rBorder.Right, top);
				float bottom = rBorder.Bottom.Ceil() - 0.5f;
				ctx.DrawLine (BorderColorPen, rBorder.Left, bottom, rBorder.Right, bottom);
			}
		}
	}

	public class TabPageStyle : WidgetStyle
	{
		public override void InitStyle ()
		{			
			SetBackColor (SystemColors.Window);
			SetForeColor (Theme.Colors.Base03);
			SetBorderColor (Theme.Colors.Base00);
		}
	}

	// *************** Tab-Button Styles ****************


	public class TabButtonStyle : GradientWidgetStyle
	{
		public Brush HighlightBrush { get; private set; }

		[DpiScalable]
		public int HighlightWidth { get; set; }

		public override void InitStyle ()
		{
			SetBackColor (Color.Empty, Color.Empty);
			SetForeColor (Theme.Colors.Base3);
			//SetBorderColor (Theme.Colors.Base00);
			SetBorderColor (Color.Transparent);

			SetHighlightStyle ();
		}

		public virtual void SetHighlightStyle()
		{
			HighlightBrush = new SolidBrush (Theme.Colors.Magenta);
			HighlightWidth = 3;
		}

		public override void PaintBackground (IGUIContext ctx, Widget widget)
		{
			base.PaintBackground (ctx, widget);

			if (widget.Selected) {
				RectangleF bounds = widget.Bounds;
				//bounds.Inflate (-1, 0);
				bounds.Offset (0, bounds.Height - HighlightWidth - 1);
				bounds.Height = HighlightWidth;
				ctx.FillRectangle (HighlightBrush, bounds);
			} else {				
				//Pen leftBorderPen = Theme.Pens.Base01;
				//Pen rightBorderPen = Theme.Pens.Base02;

				RectangleF b = widget.Bounds;
				float f = widget.ScaleFactor;

				float left = b.Left + f;
				using (Pen leftBorderPen = new Pen (Theme.Colors.Base01, f)) {
					ctx.DrawLine (leftBorderPen, left, b.Top + Border, left, b.Bottom - Border);
				}
				float right = b.Right;
				using (Pen rightBorderPen = new Pen (Theme.Colors.Base02, f)) {
					ctx.DrawLine (rightBorderPen, right, b.Top + Border, right, b.Bottom - Border);
				}
			}								
		}
	}

	public class TabButtonDisabledStyle : TabButtonStyle
	{
		public override void InitStyle ()
		{
			SetBackColor (Color.Empty, Color.Empty);
			SetForeColor (Theme.Colors.Base00);
			SetBorderColor (Color.Transparent);
			SetHighlightStyle ();
		}
	}

	public class TabButtonHoverStyle : TabButtonStyle
	{
		public override void InitStyle ()
		{
			SetBackColor (Theme.Colors.Base02, Theme.Colors.Base02);
			SetForeColor (Theme.Colors.Base3);
			SetBorderColor (Color.Transparent);

			SetHighlightStyle ();
		}

		public override void PaintBackground (IGUIContext ctx, Widget widget)
		{
			if (widget.Selected)
				BackColorBrush.Color = Theme.Colors.Base02;
			else
				BackColorBrush.Color = Theme.Colors.Base00;

			base.PaintBackground (ctx, widget);
		}
	}

	public class TabButtonPressedStyle : TabButtonStyle
	{
		public override void InitStyle ()
		{
			SetBackColor (Theme.Colors.Base01, Theme.Colors.Base02);
			SetForeColor (Theme.Colors.Base3);
			SetBorderColor (Color.Transparent);

			SetHighlightStyle ();
		}

		public override void PaintBackground (IGUIContext ctx, Widget widget)
		{			
			if (widget.Selected) {
				BackColorBrush.Color = Theme.Colors.Base03;
				(BackColorBrush as LinearGradientBrush).GradientColor = Theme.Colors.Base03;
			} else {
				BackColorBrush.Color = Theme.Colors.Base01;
				(BackColorBrush as LinearGradientBrush).GradientColor = Theme.Colors.Base03;
			}

			base.PaintBackground (ctx, widget);
		}
	}

	public class TabButtonSelectedStyle : TabButtonStyle
	{
		public override void InitStyle ()
		{
			SetBackColor (Theme.Colors.Base03, Theme.Colors.Base02);
			SetForeColor (Theme.Colors.Base2);
			SetBorderColor (Theme.Colors.Base03);

			SetHighlightStyle ();
		}
	}		


}

