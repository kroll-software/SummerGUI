using System;
using System.Drawing;

namespace SummerGUI
{
	public class MenuBarStyle : WidgetStyle
	{
		public override void InitStyle ()
		{
			SetBackColor (Theme.Colors.Base01);
			SetForeColor (Theme.Colors.Base3);
			SetBorderColor (Theme.Colors.Base02);
		}

		public override void DrawBorder (IGUIContext ctx, Widget widget)
		{
			RectangleF bounds = widget.Bounds;
			ctx.DrawLine (BorderColorPen, bounds.Left, bounds.Bottom, bounds.Right, bounds.Bottom);
		}
	}		

	public class MenuBarDisabledStyle : MenuBarStyle
	{
		public override void InitStyle ()
		{
			SetBackColor (Theme.Colors.Base01);
			SetForeColor (Theme.Colors.Base0);
			SetBorderColor (Theme.Colors.Base02);
		}
	}

	public class MenuBarSelectedStyle : MenuBarStyle
	{
		public override void InitStyle ()
		{
			SetBackColor (Theme.Colors.Base02);
			SetForeColor (Theme.Colors.Base3);
			SetBorderColor (Theme.Colors.Base02);
		}
	}

	public class MenuBarActiveStyle : MenuBarStyle
	{
		public override void InitStyle ()
		{
			SetBackColor (Theme.Colors.Orange);
			SetForeColor (Theme.Colors.White);
			SetBorderColor (Theme.Colors.Base02);
		}
	}

	// *** SubMenus ***

	public class SubMenuWidgetStyle : WidgetStyle
	{
		public override void InitStyle ()
		{			
			SetBackColor (Color.FromArgb(213, Theme.Colors.Base02));
			SetForeColor (Theme.Colors.Base3);
			SetBorderColor (Theme.Colors.Base01);

			IconBackgroundBrush = new SolidBrush (Theme.Colors.Base02);
		}

		public Brush IconBackgroundBrush { get; private set; }
		public float IconColumnWidth  { get; set; }

		public override void FillRectangle (IGUIContext ctx, Widget widget)
		{
			base.FillRectangle (ctx, widget);
			if (IconColumnWidth > 0 && IconBackgroundBrush.Color != Color.Empty)
				ctx.FillRectangle (IconBackgroundBrush, 
					new RectangleF (widget.Bounds.Left, widget.Bounds.Top, IconColumnWidth, widget.Height));			
		}
	}

	public class SubMenuDisabledItemStyle : SubMenuWidgetStyle
	{
		public override void InitStyle ()
		{
			SetBackColor (Theme.Colors.Base01);
			SetForeColor (Theme.Colors.Base0);
			SetBorderColor (Theme.Colors.Base01);
		}
	}

	public class SubMenuSelectedItemStyle : SubMenuWidgetStyle
	{
		public override void InitStyle ()
		{
			SetBackColor (Theme.Colors.Base02);
			SetForeColor (Theme.Colors.Base3);
			SetBorderColor (Theme.Colors.Base01);
		}
	}

	public class SubMenuActiveItemStyle : SubMenuWidgetStyle
	{
		public override void InitStyle ()
		{
			//SetBackColor (Color.FromArgb(238, Theme.Colors.Orange));
			SetBackColor (Color.FromArgb(250, Theme.Colors.Orange));
			SetForeColor (Theme.Colors.Base3);
			SetBorderColor (Theme.Colors.Base01);
		}
	}
}

