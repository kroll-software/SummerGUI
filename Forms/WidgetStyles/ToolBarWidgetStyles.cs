using System;
using System.Drawing;

namespace SummerGUI
{
	public class MainToolBarStyle : GradientWidgetStyle
	{
		public override void InitStyle ()
		{	
			SetBackColor (Theme.CurrentTheme.ToolBar.BackColor1, Theme.CurrentTheme.ToolBar.BackColor2);
			SetForeColor (Theme.CurrentTheme.ToolBar.ForeColor);
			SetBorderColor (Color.Empty);
		}
	}

	public class ComponentToolBarStyle : WidgetStyle
	{
		public override void InitStyle ()
		{
			SetBackColor (Theme.Colors.Silver);
			SetForeColor (Theme.Colors.Base02);
			SetBorderColor (Color.Empty);
		}

		public override void DrawBorder (IGUIContext ctx, Widget widget)
		{
			if (Border > 0) {
				RectangleF bounds = widget.Bounds;
				ctx.DrawLine (BorderColorPen, bounds.Left, bounds.Bottom, bounds.Right, bounds.Bottom);
			}
		}
	}

	public class TooltipWidgetStyle : WidgetStyle
	{
		public override void InitStyle ()
		{
			SetBackColor (Theme.CurrentTheme.ToolBar.TooltipBackColor);
			SetForeColor (Theme.CurrentTheme.ToolBar.TooltipForeColor);
			SetBorderColor (Theme.CurrentTheme.ToolBar.TooltipBorderColor);
		}
	}

	// ******* Main Toolbar Buttons *******

	public class MainToolBarButtonStyle : WidgetStyle
	{
		public override void InitStyle ()
		{
			SetBackColor (Color.Empty);
			SetForeColor (Theme.Colors.Base3);
			SetBorderColor (Color.Empty);
		}
	}		

	public class MainToolBarButtonHoverStyle : WidgetStyle
	{
		public override void InitStyle ()
		{
			SetBackColor (Theme.Colors.Base01);
			SetForeColor (Theme.Colors.Base3);
			SetBorderColor (Color.Empty);
		}
	}

	public class MainToolBarButtonPressedStyle : WidgetStyle
	{
		public override void InitStyle ()
		{
			//SetBackColor (Theme.GetContextColor(ColorContexts.Active));
			SetBackColor (Theme.Colors.Blue);
			SetForeColor (Theme.GetContextForeColor(ColorContexts.Active));
			SetBorderColor (Color.Empty);
		}
	}

	public class MainToolBarButtonDisabledStyle : WidgetStyle
	{
		public override void InitStyle ()
		{
			SetBackColor (Color.Empty);
			SetForeColor (Theme.Colors.Base01);
			SetBorderColor (Color.Empty);
		}
	}
		
	public class MainToolBarSeparatorStyle : WidgetStyle
	{
		public override void PaintBackground (IGUIContext ctx, Widget widget)
		{
			if (widget == null || !widget.CanPaint)
				return;

			float vmargin = widget.Padding.Height / 2;
			RectangleF bounds = widget.Bounds;
			float xh = bounds.Left + (bounds.Width / 2);

			float lineWidth = widget.ScaleFactor;
			using (var pen = new Pen (Theme.Colors.Base01, lineWidth)) {
				ctx.DrawLine (pen, xh, bounds.Top + vmargin, xh, bounds.Bottom - vmargin);
			}
			using (var pen = new Pen (Theme.Colors.Base1, lineWidth)) {
				ctx.DrawLine (pen, xh + lineWidth, bounds.Top + vmargin, xh + lineWidth, bounds.Bottom - vmargin);
			}
		}
	}

	// ******* Component Toolbar Buttons *******

	public class ComponentToolBarButtonStyle : WidgetStyle
	{
		public override void InitStyle ()
		{
			SetBackColor (Color.Empty);
			SetForeColor (Theme.Colors.Base02);
			SetBorderColor (Color.Empty);
		}
	}		

	public class ComponentToolBarButtonHoverStyle : WidgetStyle
	{
		public override void InitStyle ()
		{
			//SetBackColor (Theme.Colors.Silver.Lerp(Color.Black, 0.15));
			SetBackColor (Theme.Colors.LightHighLightButton);
			SetForeColor (Theme.Colors.Base02);
			SetBorderColor (Color.Empty);
		}
	}

	public class ComponentToolBarButtonPressedStyle : WidgetStyle
	{
		public override void InitStyle ()
		{			
			SetBackColor (Theme.Colors.HighLightButton);
			SetForeColor (Theme.Colors.Base02);
			SetBorderColor (Theme.Colors.HighLightButtonBorder);
		}
	}

	public class ComponentToolBarButtonDisabledStyle : WidgetStyle
	{
		public override void InitStyle ()
		{
			SetBackColor (Color.Empty);
			SetForeColor (Theme.Colors.Base00);
			SetBorderColor (Color.Empty);
		}
	}

	public class ComponentToolBarSeparatorStyle : WidgetStyle
	{
		public override void PaintBackground (IGUIContext ctx, Widget widget)
		{
			if (widget == null || !widget.CanPaint)
				return;

			float vmargin = 5;
			RectangleF bounds = widget.Bounds;
			float xh = bounds.Left + (bounds.Width / 2);

			float lineWidth = widget.ScaleFactor;
			using (var pen = new Pen (Theme.Colors.Base1, lineWidth)) {
				ctx.DrawLine (pen, xh, bounds.Top + vmargin, xh, bounds.Bottom - vmargin);
			}
			using (var pen = new Pen (Theme.Colors.Base2, lineWidth)) {
				ctx.DrawLine (pen, xh + lineWidth, bounds.Top + vmargin, xh + lineWidth, bounds.Bottom - vmargin);
			}
		}
	}
}

