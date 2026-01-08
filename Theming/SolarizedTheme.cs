using System;
using System.Drawing;

namespace SummerGUI.Theming
{
	public class SolarizedTheme : SummerGUITheme
	{
		public SolarizedTheme ()
		{
			// Intentionally left blank
		}

		protected override void InitColors ()
		{
			Colors = new ThemeColors {
				Base03 = Color.FromArgb (0, 43, 54),
				Base02 = Color.FromArgb (7, 54, 66),
				Base01 = Color.FromArgb (88, 110, 117),

				// 2 content colors
				Base00 = Color.FromArgb (101, 123, 131),
				Base0 = Color.FromArgb (131, 148, 150),

				// 3 light back- or foreground colors
				Base1 = Color.FromArgb (147, 161, 161),
				Base2 = Color.FromArgb (238, 232, 213),
				Base3 = Color.FromArgb (253, 246, 227),

				// pure colors
				Yellow = Color.FromArgb (181, 137, 0),
				Orange = Color.FromArgb (203, 75, 22),
				Red = Color.FromArgb (220, 50, 47),
				Magenta = Color.FromArgb (211, 54, 130),
				Violet = Color.FromArgb (108, 113, 196),
				Blue = Color.FromArgb (38, 139, 210),
				Cyan = Color.FromArgb (42, 161, 152),
				Green = Color.FromArgb (133, 153, 0),

				// some more colors used through the application
				White = Color.FromArgb (255, 255, 255),
				Black = Color.FromArgb (0, 0, 0),
				Silver = Color.FromArgb (206, 212, 223),

				GrayButton = Color.FromArgb (206, 212, 223),
				LightGrayButton = Color.FromArgb (237, 239, 243),

				HighLightButton = Color.FromArgb (255, 236, 181),
				LightHighLightButton = Color.FromArgb (255, 250, 237),
				HighLightButtonBorder = Color.FromArgb (229, 195, 101),

				HighLightYellow = Color.FromArgb (241, 243, 248),
				//HighLightBlue = Color.FromArgb (50, 150, 250),
				//HighLightBlueTransparent = Color.FromArgb (100, 50, 150, 250),

				//HighLightBlue = Color.FromArgb (235, 38, 139, 210),
				//HighLightBlue = Color.FromArgb (245, 38, 139, 210),
				HighLightBlue = Color.FromArgb (38, 139, 210),
				HighLightBlueTransparent = Color.FromArgb (100, 38, 139, 210),
			};
		}
	}
}

