using System;
using System.Drawing;

namespace SummerGUI
{
	public static class MaterialColors
	{
		public static readonly Color White = Color.FromArgb(255, 255, 255);
		public static readonly Color Silver = Color.FromArgb(238, 238, 238);
		public static readonly Color Black = Color.FromArgb(85, 85, 85);
		public static readonly Color Red = Color.FromArgb(247, 83, 83);
		public static readonly Color Green = Color.FromArgb(81, 212, 102);
		public static readonly Color LightBlue = Color.FromArgb(50, 200, 222);
		public static readonly Color Blue = Color.FromArgb(96, 156, 236);
		public static readonly Color Orange = Color.FromArgb(247, 129, 83);
		public static readonly Color Yellow = Color.FromArgb(252, 212, 25);
		public static readonly Color Purple = Color.FromArgb(203, 121, 230);
		public static readonly Color Rose = Color.FromArgb(255, 97, 231);
		public static readonly Color Brown = Color.FromArgb(208, 129, 102);		

		public static readonly Color[] Colors = new Color[] 
		{ Red, Green, LightBlue, Blue, Orange, Yellow, Purple, Rose };

		public static class Pens
		{
			public static readonly Pen White = new Pen(MaterialColors.White);
			public static readonly Pen Silver = new Pen(MaterialColors.Silver);
			public static readonly Pen Black = new Pen(MaterialColors.Black);
			public static readonly Pen Red = new Pen(MaterialColors.Red);
			public static readonly Pen Green = new Pen(MaterialColors.Green);
			public static readonly Pen LightBlue = new Pen(MaterialColors.LightBlue);
			public static readonly Pen Blue = new Pen(MaterialColors.Blue);
			public static readonly Pen Orange = new Pen(MaterialColors.Orange);
			public static readonly Pen Yellow = new Pen(MaterialColors.Yellow);
			public static readonly Pen Purple = new Pen(MaterialColors.Purple);
			public static readonly Pen Rose = new Pen(MaterialColors.Rose);
			public static readonly Pen Brown = new Pen(MaterialColors.Brown);
		}

		public static class Brushes
		{			
			public static readonly Brush White = new SolidBrush(MaterialColors.White);
			public static readonly Brush Silver = new SolidBrush(MaterialColors.Silver);
			public static readonly Brush Black = new SolidBrush(MaterialColors.Black);
			public static readonly Brush Red = new SolidBrush(MaterialColors.Red);
			public static readonly Brush Green = new SolidBrush(MaterialColors.Green);
			public static readonly Brush LightBlue = new SolidBrush(MaterialColors.LightBlue);
			public static readonly Brush Blue = new SolidBrush(MaterialColors.Blue);
			public static readonly Brush Orange = new SolidBrush(MaterialColors.Orange);
			public static readonly Brush Yellow = new SolidBrush(MaterialColors.Yellow);
			public static readonly Brush Purple = new SolidBrush(MaterialColors.Purple);
			public static readonly Brush Rose = new SolidBrush(MaterialColors.Rose);
			public static readonly Brush Brown = new SolidBrush(MaterialColors.Brown);
		}
	}
}

