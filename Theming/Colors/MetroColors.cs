using System;
using System.Drawing;

namespace SummerGUI
{
	public static class MetroColors
	{		
		public static readonly Color Black = Color.FromArgb(0, 0, 0);
		public static readonly Color White = Color.FromArgb(255, 255, 255);
		public static readonly Color Silver = Color.FromArgb(85, 85, 85);
		public static readonly Color Blue = Color.FromArgb(0, 174, 219);
		public static readonly Color Green = Color.FromArgb(0, 177, 89);
		public static readonly Color Chartreuse = Color.FromArgb(142, 188, 0);
		public static readonly Color Cyan = Color.FromArgb(0, 170, 173);
		public static readonly Color Orange = Color.FromArgb(243, 119, 53);
		public static readonly Color Brown = Color.FromArgb(165, 81, 0);
		public static readonly Color Pink = Color.FromArgb(231, 113, 189);
		public static readonly Color Magenta = Color.FromArgb(255, 0, 148);
		public static readonly Color Violet = Color.FromArgb(124, 65, 153);
		public static readonly Color Red = Color.FromArgb(209, 17, 65);
		public static readonly Color Yellow = Color.FromArgb(255, 196, 37);

		public static readonly Color[] Colors = new Color[] 
			{ Blue, Green, Chartreuse, Cyan, Orange, Brown, Pink, Magenta, Violet, Red, Yellow };

		public static class Pens
		{
			public static readonly Pen Black = new Pen(MetroColors.Black);
			public static readonly Pen White = new Pen(MetroColors.White);
			public static readonly Pen Silver = new Pen(MetroColors.Silver);
			public static readonly Pen Blue = new Pen(MetroColors.Blue);
			public static readonly Pen Green = new Pen(MetroColors.Green);
			public static readonly Pen Chartreuse = new Pen(MetroColors.Chartreuse);
			public static readonly Pen Cyan = new Pen(MetroColors.Cyan);
			public static readonly Pen Orange = new Pen(MetroColors.Orange);
			public static readonly Pen Brown = new Pen(MetroColors.Brown);
			public static readonly Pen Pink = new Pen(MetroColors.Pink);
			public static readonly Pen Magenta = new Pen(MetroColors.Magenta);
			public static readonly Pen Violet = new Pen(MetroColors.Violet);
			public static readonly Pen Red = new Pen(MetroColors.Red);
			public static readonly Pen Yellow = new Pen(MetroColors.Yellow);
		}

		public static class Brushes
		{
			public static readonly Brush Black = new SolidBrush(MetroColors.Black);
			public static readonly Brush White = new SolidBrush(MetroColors.White);
			public static readonly Brush Silver = new SolidBrush(MetroColors.Silver);
			public static readonly Brush Blue = new SolidBrush(MetroColors.Blue);
			public static readonly Brush Green = new SolidBrush(MetroColors.Green);
			public static readonly Brush Chartreuse = new SolidBrush(MetroColors.Chartreuse);
			public static readonly Brush Cyan = new SolidBrush(MetroColors.Cyan);
			public static readonly Brush Orange = new SolidBrush(MetroColors.Orange);
			public static readonly Brush Brown = new SolidBrush(MetroColors.Brown);
			public static readonly Brush Pink = new SolidBrush(MetroColors.Pink);
			public static readonly Brush Magenta = new SolidBrush(MetroColors.Magenta);
			public static readonly Brush Violet = new SolidBrush(MetroColors.Violet);
			public static readonly Brush Red = new SolidBrush(MetroColors.Red);
			public static readonly Brush Yellow = new SolidBrush(MetroColors.Yellow);
		}
	}
}

