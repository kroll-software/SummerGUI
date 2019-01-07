using System;
using System.Drawing;

namespace SummerGUI
{
	public static class SolarizedColors
	{		
		public static readonly Color Base03 = Color.FromArgb (0, 43, 54);
		public static readonly Color Base02 = Color.FromArgb (7, 54, 66);
		public static readonly Color Base01 = Color.FromArgb (88, 110, 117);
		public static readonly Color Base00 = Color.FromArgb (101, 123, 131);

		public static readonly Color Base0 = Color.FromArgb (131, 148, 150);
		public static readonly Color Base1 = Color.FromArgb (147, 161, 161);
		public static readonly Color Base2 = Color.FromArgb (238, 232, 213);
		public static readonly Color Base3 = Color.FromArgb (253, 246, 227);

		public static readonly Color Yellow = Color.FromArgb (181, 137, 0);
		public static readonly Color Orange = Color.FromArgb (203, 75, 22);
		public static readonly Color Red = Color.FromArgb (220, 50, 47);
		public static readonly Color Magenta = Color.FromArgb (211, 54, 130);
		public static readonly Color Violet = Color.FromArgb (108, 113, 196);
		public static readonly Color Blue = Color.FromArgb (38, 139,210);
		public static readonly Color Cyan = Color.FromArgb (42, 161, 152);
		public static readonly Color Green = Color.FromArgb (133, 153, 0);

		// some more colors added by Kroll
		public static readonly Color White = Color.FromArgb (255, 255, 255);
		public static readonly Color Silver = Color.FromArgb (206, 212, 223);
		public static readonly Color HighLightYellow = Color.FromArgb (241, 243, 248);
		public static readonly Color HighLightBlue = Color.FromArgb (50, 150, 250);
		public static readonly Color HighLightBlueTransparent = Color.FromArgb (100, 50, 150, 250);

		public static class Pens
		{	
			public static readonly Pen Base03 = new Pen (SolarizedColors.Base03);
			public static readonly Pen Base02 = new Pen (SolarizedColors.Base02);
			public static readonly Pen Base01 = new Pen (SolarizedColors.Base01);
			public static readonly Pen Base00 = new Pen (SolarizedColors.Base00);

			public static readonly Pen Base0 = new Pen (SolarizedColors.Base0);			
			public static readonly Pen Base1 = new Pen (SolarizedColors.Base1);
			public static readonly Pen Base2 = new Pen (SolarizedColors.Base2);
			public static readonly Pen Base3 = new Pen (SolarizedColors.Base3);

			public static readonly Pen Yellow = new Pen (SolarizedColors.Yellow);
			public static readonly Pen Orange = new Pen (SolarizedColors.Orange);
			public static readonly Pen Red = new Pen (SolarizedColors.Red);
			public static readonly Pen Magenta = new Pen (SolarizedColors.Red);
			public static readonly Pen Violet = new Pen (SolarizedColors.Violet);
			public static readonly Pen Blue = new Pen (SolarizedColors.Violet);
			public static readonly Pen Cyan = new Pen (SolarizedColors.Cyan);
			public static readonly Pen Green = new Pen (SolarizedColors.Green);

			public static readonly Pen White = new Pen (SolarizedColors.White);
			public static readonly Pen Silver = new Pen (SolarizedColors.Silver);
			public static readonly Pen HighLightYellow = new Pen (SolarizedColors.HighLightYellow);
			public static readonly Pen HighLightBlue = new Pen (SolarizedColors.HighLightBlue);
		}	

		public static class Brushes
		{
			public static readonly Brush Base03 = new SolidBrush (SolarizedColors.Base03);
			public static readonly Brush Base02 = new SolidBrush (SolarizedColors.Base02);
			public static readonly Brush Base01 = new SolidBrush (SolarizedColors.Base01);
			public static readonly Brush Base00 = new SolidBrush (SolarizedColors.Base00);

			public static readonly Brush Base0 = new SolidBrush (SolarizedColors.Base0);
			public static readonly Brush Base1 = new SolidBrush (SolarizedColors.Base1);
			public static readonly Brush Base2 = new SolidBrush (SolarizedColors.Base2);
			public static readonly Brush Base3 = new SolidBrush (SolarizedColors.Base3);

			public static readonly Brush Yellow = new SolidBrush (SolarizedColors.Yellow);
			public static readonly Brush Orange = new SolidBrush (SolarizedColors.Orange);
			public static readonly Brush Red = new SolidBrush (SolarizedColors.Red);
			public static readonly Brush Magenta = new SolidBrush (SolarizedColors.Magenta);
			public static readonly Brush Violet = new SolidBrush (SolarizedColors.Violet);
			public static readonly Brush Blue = new SolidBrush (SolarizedColors.Blue);
			public static readonly Brush Cyan = new SolidBrush (SolarizedColors.Cyan);
			public static readonly Brush Green = new SolidBrush (SolarizedColors.Green);

			public static readonly Brush White = new SolidBrush (SolarizedColors.White);
			public static readonly Brush Silver = new SolidBrush (SolarizedColors.Silver);

			public static readonly Brush HighLightYellow = new SolidBrush (SolarizedColors.HighLightYellow);
			public static readonly Brush HighLightBlue = new SolidBrush (SolarizedColors.Blue);

			/**
			public static readonly Brush HighLightYellow = new SolidBrush (SolarizedColors.HighLightYellow);
			public static readonly Brush HighLightBlue = new SolidBrush (SolarizedColors.HighLightBlue);
			public static readonly Brush HighLightGray = new SolidBrush (SolarizedColors.HighLightGray);
			**/
		}
	}		
}

