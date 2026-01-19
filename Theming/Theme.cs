using System;
using System.Drawing;
using SummerGUI;

namespace SummerGUI
{
	public enum ColorContexts
	{
		Default,
		Active,
		Information,
		Success,
		Warning,
		Danger,
		Question,
		Outline
	}

	public enum ButtonStyles
	{
		Gradient,
		Glossy,
		Flat
	}

	public static class Theme
	{
		public static readonly Theming.SummerGUITheme DefaultTheme = new SummerGUI.Theming.SolarizedTheme();

		private static Theming.SummerGUITheme m_CurrentTheme;
		public static Theming.SummerGUITheme CurrentTheme
		{
			get{
				if (m_CurrentTheme == null)
					return DefaultTheme;
				return m_CurrentTheme;
			}
			set{
				m_CurrentTheme = value;
			}
		}

		public static bool LoadTheme(string fname)
		{
			return false;
		}

		public static ButtonStyles ButtonStyle
		{
			get{
				return CurrentTheme.ButtonStyle;
			}
		}

		public static class Colors
		{
			// from darkest to lightest
			// 3 dark back- or foreground colors
			//public static Color Base03 = Color.FromArgb (0, 43, 54);
			public static Color Base03
			{
				get{
					return CurrentTheme.Colors.Base03;
				}
			}

			public static Color Base02
			{
				get{
					return CurrentTheme.Colors.Base02;
				}
			}

			public static Color Base01
			{
				get{
					return CurrentTheme.Colors.Base01;
				}
			}


			// 2 content colors
			public static Color Base00
			{
				get{
					return CurrentTheme.Colors.Base00;
				}
			}

			public static Color Base0
			{
				get{
					return CurrentTheme.Colors.Base0;
				}
			}

			// 3 light back- or foreground colors
			public static Color Base1
			{
				get{
					return CurrentTheme.Colors.Base1;
				}
			}

			public static Color Base2
			{
				get{
					return CurrentTheme.Colors.Base2;
				}
			}

			public static Color Base3
			{
				get{
					return CurrentTheme.Colors.Base3;
				}
			}

			// pure colors
			public static Color Yellow
			{
				get{
					return CurrentTheme.Colors.Yellow;
				}
			}

			public static Color Orange
			{
				get{
					return CurrentTheme.Colors.Orange;
				}
			}

			public static Color Red
			{
				get{
					return CurrentTheme.Colors.Red;
				}
			}

			public static Color Magenta
			{
				get{
					return CurrentTheme.Colors.Magenta;
				}
			}

			public static Color Violet
			{
				get{
					return CurrentTheme.Colors.Violet;
				}
			}

			public static Color Blue
			{
				get{
					return CurrentTheme.Colors.Blue;
				}
			}

			public static Color Cyan
			{
				get{
					return CurrentTheme.Colors.Cyan;
				}
			}

			public static Color Green
			{
				get{
					return CurrentTheme.Colors.Green;
				}
			}

			// some more colors used through the application
			public static Color White
			{
				get{
					return CurrentTheme.Colors.White;
				}
			}

			public static Color Black
			{
				get{
					return CurrentTheme.Colors.Black;
				}
			}

			public static Color Silver
			{
				get{
					return CurrentTheme.Colors.Silver;
				}
			}				

			public static Color GrayButton
			{
				get{
					return CurrentTheme.Colors.GrayButton;
				}
			}

			public static Color LightGrayButton
			{
				get{
					return CurrentTheme.Colors.LightGrayButton;
				}
			}
				
			public static Color HighLightButton
			{
				get{
					return CurrentTheme.Colors.HighLightButton;
				}
			}

			public static Color LightHighLightButton
			{
				get{
					return CurrentTheme.Colors.LightHighLightButton;
				}
			}

			public static Color HighLightButtonBorder
			{
				get{
					return CurrentTheme.Colors.HighLightButtonBorder;
				}
			}

			public static Color HighLightYellow
			{
				get{
					return CurrentTheme.Colors.HighLightYellow;
				}
			}

			public static Color HighLightBlue
			{
				get{
					return CurrentTheme.Colors.HighLightBlue;
				}
			}

			public static Color HighLightBlueTransparent
			{
				get{
					return CurrentTheme.Colors.HighLightBlueTransparent;
				}
			}
		}

		public static class Pens
		{
			public static Pen Base03
			{
				get {
					return CurrentTheme.Pens.Base03;
				}
			}
				
			public static Pen Base02
			{
				get {
					return CurrentTheme.Pens.Base02;
				}
			}

			public static Pen Base01
			{
				get {
					return CurrentTheme.Pens.Base01;
				}
			}

			public static Pen Base00
			{
				get {
					return CurrentTheme.Pens.Base00;
				}
			}

			public static Pen Base0
			{
				get {
					return CurrentTheme.Pens.Base0;
				}
			}

			public static Pen Base1
			{
				get {
					return CurrentTheme.Pens.Base1;
				}
			}

			public static Pen Base2
			{
				get {
					return CurrentTheme.Pens.Base2;
				}
			}

			public static Pen Base3
			{
				get {
					return CurrentTheme.Pens.Base3;
				}
			}

			public static Pen Yellow
			{
				get {
					return CurrentTheme.Pens.Yellow;
				}
			}

			public static Pen Orange
			{
				get {
					return CurrentTheme.Pens.Orange;
				}
			}

			public static Pen Red
			{
				get {
					return CurrentTheme.Pens.Red;
				}
			}

			public static Pen Magenta
			{
				get {
					return CurrentTheme.Pens.Magenta;
				}
			}

			public static Pen Violet
			{
				get {
					return CurrentTheme.Pens.Violet;
				}
			}

			public static Pen Blue
			{
				get {
					return CurrentTheme.Pens.Blue;
				}
			}

			public static Pen Cyan
			{
				get {
					return CurrentTheme.Pens.Cyan;
				}
			}

			public static Pen Green
			{
				get {
					return CurrentTheme.Pens.Green;
				}
			}

			public static Pen White
			{
				get {
					return CurrentTheme.Pens.White;
				}
			}

			public static Pen Silver
			{
				get {
					return CurrentTheme.Pens.Silver;
				}
			}

			public static Pen HighLightYellow
			{
				get {
					return CurrentTheme.Pens.HighLightYellow;
				}
			}

			public static Pen HighLightBlue
			{
				get {
					return CurrentTheme.Pens.HighLightBlue;
				}
			}
		}

		public static class Brushes
		{
			public static Brush Base03
			{
				get {
					return CurrentTheme.Brushes.Base03;
				}
			}

			public static Brush Base02
			{
				get {
					return CurrentTheme.Brushes.Base02;
				}
			}

			public static Brush Base01
			{
				get {
					return CurrentTheme.Brushes.Base01;
				}
			}

			public static Brush Base00
			{
				get {
					return CurrentTheme.Brushes.Base00;
				}
			}

			public static Brush Base0
			{
				get {
					return CurrentTheme.Brushes.Base0;
				}
			}

			public static Brush Base1
			{
				get {
					return CurrentTheme.Brushes.Base1;
				}
			}

			public static Brush Base2
			{
				get {
					return CurrentTheme.Brushes.Base2;
				}
			}

			public static Brush Base3
			{
				get {
					return CurrentTheme.Brushes.Base3;
				}
			}

			public static Brush Yellow
			{
				get {
					return CurrentTheme.Brushes.Yellow;
				}
			}

			public static Brush Orange
			{
				get {
					return CurrentTheme.Brushes.Orange;
				}
			}

			public static Brush Red
			{
				get {
					return CurrentTheme.Brushes.Red;
				}
			}

			public static Brush Magenta
			{
				get {
					return CurrentTheme.Brushes.Magenta;
				}
			}

			public static Brush Violet
			{
				get {
					return CurrentTheme.Brushes.Violet;
				}
			}

			public static Brush Blue
			{
				get {
					return CurrentTheme.Brushes.Blue;
				}
			}

			public static Brush Cyan
			{
				get {
					return CurrentTheme.Brushes.Cyan;
				}
			}

			public static Brush Green
			{
				get {
					return CurrentTheme.Brushes.Green;
				}
			}

			public static Brush White
			{
				get {
					return CurrentTheme.Brushes.White;
				}
			}

			public static Brush Silver
			{
				get {
					return CurrentTheme.Brushes.Silver;
				}
			}

			public static Brush HighLightYellow
			{
				get {
					return CurrentTheme.Brushes.HighLightYellow;
				}
			}

			public static Brush HighLightBlue
			{
				get {
					return CurrentTheme.Brushes.HighLightBlue;
				}
			}
		}

		public static int DefaultButtonAlpha
		{
			get{
				return CurrentTheme.DefaultButtonAlpha;
			}
		}

		public static Color GetContextColor(ColorContexts colorContext)
		{
			return CurrentTheme.GetContextColor (colorContext);
		}

		public static Color GetContextGradientColor(ColorContexts colorContext)
		{
			return CurrentTheme.GetContextGradientColor (colorContext);
		}

		public static Color GetContextBorderColor(ColorContexts colorContext)
		{
			return CurrentTheme.GetContextBorderColor (colorContext);
		}

		public static Color GetContextForeColor(ColorContexts colorContext)
		{
			return CurrentTheme.GetContextForeColor (colorContext);
		}

		public static Color GetContextHoverColor(ColorContexts colorContext)
		{
			return CurrentTheme.GetContextColor (colorContext).Lerp (Color.White, 0.25f);
		}

		public static Color GetContextDisabledColor(ColorContexts colorContext)
		{
			return CurrentTheme.GetContextColor (colorContext).Lerp (Color.LightGray, 0.75f);
		}

		public static Color GetContextDisabledBorderColor(ColorContexts colorContext)
		{
			return CurrentTheme.GetContextBorderColor (colorContext).Lerp (Color.LightGray, 0.75f);
		}

		public static Color GetContextDisabledForeColor(ColorContexts colorContext)
		{
			return CurrentTheme.GetContextForeColor (colorContext).Lerp (Color.Gray, 0.5f);
		}
	}
}

