using System;
using System.Drawing;

namespace SummerGUI
{	
	public class BrightPanelWidgetStyle : WidgetStyle
	{
		public override void InitStyle ()
		{
			SetBackColor (Theme.Colors.Base3);
			SetForeColor (Theme.Colors.Base03);
			SetBorderColor (Color.Empty);
		}
	}

	public class LightPanelWidgetStyle : WidgetStyle
	{
		public override void InitStyle ()
		{
			SetBackColor (Theme.Colors.Base2);
			SetForeColor (Theme.Colors.Base03);
			SetBorderColor (Color.Empty);
		}
	}

	public class DarkPanelWidgetStyle : WidgetStyle
	{
		public override void InitStyle ()
		{
			SetBackColor (Theme.Colors.Base02);
			SetForeColor (Theme.Colors.Base3);
			SetBorderColor (Color.Empty);
		}
	}

	public class DarkGradientPanelWidgetStyle : GradientWidgetStyle
	{
		public override void InitStyle ()
		{			
			SetBackColor (Theme.Colors.Base02, Theme.Colors.Base03);
			SetForeColor (Theme.Colors.Base1);
			SetBorderColor (Color.Empty);
		}
	}

	public class SilverPanelWidgetStyle : WidgetStyle
	{
		public override void InitStyle ()
		{
			SetBackColor (Theme.Colors.Silver);
			SetForeColor (Theme.Colors.Base03);
			SetBorderColor (Color.Empty);
		}
	}

	public class GradientLightFormWidgetStyle : GradientWidgetStyle
	{
		public override void InitStyle ()
		{
			SetBackColor (Theme.Colors.Base3, Theme.Colors.Base2);
			SetForeColor (Theme.Colors.Base03);
			SetBorderColor (Color.Empty);
		}
	}
}

