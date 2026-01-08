using System;
using System.Drawing;

namespace SummerGUI
{
	public class StatusBarStyle : WidgetStyle
	{
		public override void InitStyle ()
		{
			SetBackColor (Theme.CurrentTheme.StatusBar.BackColor);
			SetForeColor (Theme.CurrentTheme.StatusBar.ForeColor);
			SetBorderColor (Color.Empty);
			//SetBorderColor (Theme.CurrentTheme.StatusBar.LineColor);
			//BorderDistance = -1;
		}
	}

	public class StatusPanelStyle : WidgetStyle
	{
		public override void InitStyle ()
		{
			SetBackColor (Color.Empty);
			SetForeColor (Theme.CurrentTheme.StatusBar.ForeColor);
			SetBorderColor (Color.Empty);
		}
	}
}

