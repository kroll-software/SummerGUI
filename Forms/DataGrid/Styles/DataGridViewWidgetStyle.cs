using System;
using System.Drawing;

namespace SummerGUI
{	
	public class DataGridViewWidgetStyle : WidgetStyle
	{
		public override void InitStyle ()
		{
			//SetBackColor (Theme.Colors.Base1, Theme.Colors.Base0);
			SetBackColor (Color.GhostWhite);
			//SetBackColor (Color.Empty);
			SetForeColor (SolarizedColors.Blue);
			SetBorderColor (Color.Empty);
		}
	}
}

