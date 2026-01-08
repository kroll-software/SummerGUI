using System;
using System.Drawing;

namespace SummerGUI
{
	public class DefaultTextWidgetStyle : WidgetStyle
	{
		public override void InitStyle ()
		{
			SetBackColor (Color.Empty);
			SetForeColor (Theme.Colors.Base02);
			SetBorderColor (Color.Empty);
		}
	}

	public class ActiveTextWidgetStyle : WidgetStyle
	{
		public override void InitStyle ()
		{
			//SetBackColor (Color.FromArgb(64, NcsColors.Yellow));
			SetBackColor (Color.Empty);
			SetForeColor (Theme.Colors.Base02);
			SetBorderColor (Theme.Colors.Base0);
			BorderColorPen.LineStyle = LineStyles.Dotted;
		}
	}

	public class HoverTextWidgetStyle : WidgetStyle
	{
		public override void InitStyle ()
		{
			SetBackColor (Theme.Colors.Base2);
			SetForeColor (Theme.Colors.Base02);
			SetBorderColor (Color.Empty);
		}
	}

	public class DisabledTextWidgetStyle : WidgetStyle
	{
		public override void InitStyle ()
		{
			SetBackColor (Color.Empty);
			SetForeColor (Theme.Colors.Base0);
			SetBorderColor (Color.Empty);
		}
	}

	//*** Form Label Style

	public class DefaultFormLabelWidgetStyle : WidgetStyle
	{
		public override void InitStyle ()
		{
			SetBackColor (Color.Empty);
			SetForeColor (Theme.Colors.Base01);
			SetBorderColor (Color.Empty);
		}
	}

	public class DisabledFormLabelWidgetStyle : WidgetStyle
	{
		public override void InitStyle ()
		{
			SetBackColor (Color.Empty);
			SetForeColor (Theme.Colors.Base0);
			SetBorderColor (Color.Empty);
		}
	}
	 
}

