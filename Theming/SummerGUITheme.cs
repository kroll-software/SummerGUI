using System;
using System.Drawing;
using KS.Foundation;

namespace SummerGUI.Theming
{	
	public abstract class SummerGUITheme
	{
		public string Name { get; set; }
		public string Description { get; set; }
		public string Credits { get; set; }
		public Version Version { get; set; }

		protected SummerGUITheme ()
		{
			Version = new Version (1, 0, 0);
			Name = "Generic Theme";
			Description = "No description available";
			Credits = "Summer GUI by John Summer";

			InitColors ();
			OnInit ();
		}

		protected abstract void InitColors();

		protected virtual void OnInit()
		{
			DefaultButtonAlpha = 245;
			ButtonStyle = ButtonStyles.Glossy;

			if (Colors == null) {
				this.LogError ("Please initialize Theme.Colors before calling Theme.OnInit().");
				return;
			}

			Pens = new ThemePens {
				Base03 = new Pen(Colors.Base03),
				Base02 = new Pen(Colors.Base02),
				Base01 = new Pen(Colors.Base01),
				Base00 = new Pen(Colors.Base00),

				Base0 = new Pen(Colors.Base0),
				Base1 = new Pen(Colors.Base1),
				Base2 = new Pen(Colors.Base2),
				Base3 = new Pen(Colors.Base3),

				Yellow = new Pen(Colors.Yellow),
				Orange = new Pen(Colors.Orange),
				Red = new Pen(Colors.Red),
				Magenta = new Pen(Colors.Magenta),
				Violet = new Pen(Colors.Violet),
				Blue = new Pen(Colors.Blue),
				Cyan = new Pen(Colors.Cyan),
				Green = new Pen(Colors.Green),

				White = new Pen(Colors.White),
				Silver = new Pen(Colors.Silver),
				HighLightYellow = new Pen(Colors.HighLightYellow),
				//HighLightBlue = new Pen(Colors.HighLightBlue)
				//HighLightBlue = new Pen(Colors.Blue)
				//HighLightBlue = new Pen(Color.FromArgb (245, 38, 139, 210))
				HighLightBlue = new Pen(Color.FromArgb (38, 139, 210))
			};

			Brushes = new ThemeBrushes {
				Base03 = new SolidBrush(Colors.Base03),
				Base02 = new SolidBrush(Colors.Base02),
				Base01 = new SolidBrush(Colors.Base01),
				Base00 = new SolidBrush(Colors.Base00),

				Base0 = new SolidBrush(Colors.Base0),
				Base1 = new SolidBrush(Colors.Base1),
				Base2 = new SolidBrush(Colors.Base2),
				Base3 = new SolidBrush(Colors.Base3),

				Yellow = new SolidBrush(Colors.Yellow),
				Orange = new SolidBrush(Colors.Orange),
				Red = new SolidBrush(Colors.Red),
				Magenta = new SolidBrush(Colors.Magenta),
				Violet = new SolidBrush(Colors.Violet),
				Blue = new SolidBrush(Colors.Blue),
				Cyan = new SolidBrush(Colors.Cyan),
				Green = new SolidBrush(Colors.Green),

				White = new SolidBrush(Colors.White),
				Silver = new SolidBrush(Colors.Silver),
				HighLightYellow = new SolidBrush(Colors.HighLightYellow),
				//HighLightBlue = new SolidBrush(Colors.HighLightBlue)
				//HighLightBlue = new SolidBrush(Color.FromArgb (245, 38, 139, 210))
				HighLightBlue = new SolidBrush(Color.FromArgb (38, 139, 210))
			};

			ScrollBar = new ThemeScrollBar {
				BackColor = Color.FromArgb (241, 241, 241),
				GripColor = Color.FromArgb (188, 188, 188),
				GripBorderColor = Color.FromArgb (168, 168, 168),

				GripColorHover = Color.FromArgb (168, 168, 168),
				GripBorderColorHover = Color.FromArgb (154, 154, 154),

				GripColorDrag = Color.FromArgb (141, 141, 141),
				GripBorderColorDrag = Color.FromArgb (120, 120, 120),

				ButtonForeColor = Color.FromArgb (80, 80, 80),
				ButtonForeColorDisabled = Color.FromArgb (164, 164, 164),

				ButtonColorHover = Color.FromArgb (210, 210, 210),
				ButtonColorPressed = Color.FromArgb (120, 120, 120),
				ButtonForeColorPressed = Color.FromArgb (255, 255, 255)
			};

			DarkScrollBar = new ThemeScrollBar {
				BackColor = Colors.Base03,
				GripColor = Colors.Base00,
				GripBorderColor = Colors.Base02,

				GripColorHover = Colors.Base0,
				GripBorderColorHover = Colors.Base01,

				GripColorDrag = Colors.Base1,
				GripBorderColorDrag = Colors.Base00,

				ButtonForeColor = Colors.Base1,
				ButtonForeColorDisabled = Color.DimGray,

				ButtonColorHover = Colors.Base02,
				ButtonColorPressed = Colors.Base00,
				ButtonForeColorPressed = Colors.Base3
			};

			ToolBar = new ThemeToolBar {
				BackColor1 = Colors.Base1,
				BackColor2 = Colors.Base0,
				ForeColor = Colors.Base2,
				TooltipBackColor = Color.FromArgb(245, 255, 250, 180),
				TooltipForeColor = Color.FromArgb(255, 20, 10, 0),
				TooltipBorderColor = Color.FromArgb(128, 30, 20, 10),
			};

			StatusBar = new ThemeStatusBar {
				BackColor = Colors.Base03,
				ForeColor = Colors.Base2,
				LineColor = Color.Empty
			};

			ProgressBar = new ThemeProgressBar {				
				ProgressBackColor = Colors.Base00,
				ProgressForeColor = Colors.White,
				DefaultProgressColor = Colors.Orange
			};
		}

		public ThemeColors Colors { get; set; }
		public ThemeBrushes Brushes { get; set; }
		public ThemePens Pens { get; set; }
		public ThemeScrollBar ScrollBar { get; set; }
		public ThemeScrollBar DarkScrollBar { get; set; }
		public ThemeToolBar ToolBar { get; set; }
		public ThemeStatusBar StatusBar { get; set; }
		public ThemeProgressBar ProgressBar { get; set; }

		public class ThemeColors
		{
			// from darkest to lightest
			// 3 dark back- or foreground colors
			public Color Base03 { get; set; }
			public Color Base02 { get; set; }
			public Color Base01 { get; set; }

			// 2 content colors
			public Color Base00 { get; set; }
			public Color Base0 { get; set; }

			// 3 light back- or foreground colors
			public Color Base1 { get; set; }
			public Color Base2 { get; set; }
			public Color Base3 { get; set; }

			// pure colors
			public Color Yellow { get; set; }
			public Color Orange { get; set; }
			public Color Red { get; set; }
			public Color Magenta { get; set; }
			public Color Violet { get; set; }
			public Color Blue { get; set; }
			public Color Cyan { get; set; }
			public Color Green { get; set; }

			// some more colors used through the application
			public Color White { get; set; }
			public Color Black { get; set; }
			public Color Silver { get; set; }

			public Color GrayButton { get; set; }
			public Color LightGrayButton { get; set; }

			public Color HighLightButton { get; set; }
			public Color LightHighLightButton { get; set; }
			public Color HighLightButtonBorder { get; set; }

			public Color HighLightYellow { get; set; }
			public Color HighLightBlue { get; set; }
			public Color HighLightBlueTransparent { get; set; }
		}

		public class ThemePens
		{
			public Pen Base03 { get; set; }
			public Pen Base02 { get; set; }
			public Pen Base01 { get; set; }
			public Pen Base00 { get; set; }

			public Pen Base0 { get; set; }
			public Pen Base1 { get; set; }
			public Pen Base2 { get; set; }
			public Pen Base3 { get; set; }

			public Pen Yellow { get; set; }
			public Pen Orange { get; set; }
			public Pen Red { get; set; }
			public Pen Magenta { get; set; }
			public Pen Violet { get; set; }
			public Pen Blue { get; set; }
			public Pen Cyan { get; set; }
			public Pen Green { get; set; }

			public Pen White { get; set; }
			public Pen Silver { get; set; }
			public Pen HighLightYellow { get; set; }
			public Pen HighLightBlue { get; set; }
		}

		public class ThemeBrushes
		{
			public Brush Base03 { get; set; }
			public Brush Base02 { get; set; }
			public Brush Base01 { get; set; }
			public Brush Base00 { get; set; }

			public Brush Base0 { get; set; }
			public Brush Base1 { get; set; }
			public Brush Base2 { get; set; }
			public Brush Base3 { get; set; }

			public Brush Yellow { get; set; }
			public Brush Orange { get; set; }
			public Brush Red { get; set; }
			public Brush Magenta { get; set; }
			public Brush Violet { get; set; }
			public Brush Blue { get; set; }
			public Brush Cyan { get; set; }
			public Brush Green { get; set; }

			public Brush White { get; set; }
			public Brush Silver { get; set; }

			public Brush HighLightYellow { get; set; }
			public Brush HighLightBlue { get; set; }
		}

		public class ThemeProgressBar
		{
			public Color InnerProgressColor { get; set; }
			public Color ProgressBackColor { get; set; }
			public Color ProgressForeColor { get; set; }
			public Color DefaultProgressColor { get; set; }
		}

		public class ThemeScrollBar
		{
			public Color BackColor { get; set; }
			public Color GripColor { get; set; }
			public Color GripBorderColor { get; set; }

			public Color GripColorHover { get; set; }
			public Color GripBorderColorHover { get; set; }

			public Color GripColorDrag { get; set; }
			public Color GripBorderColorDrag { get; set; }

			public Color ButtonForeColor { get; set; }
			public Color ButtonForeColorDisabled { get; set; }

			public Color ButtonColorHover { get; set; }
			public Color ButtonColorPressed { get; set; }
			public Color ButtonForeColorPressed { get; set; }
		}			

		public class ThemeToolBar
		{
			public Color BackColor1 { get; set; }
			public Color BackColor2 { get; set; }
			public Color ForeColor { get; set; }
			//public static Color ToolBarLineColor = SolarizedColors.SolarizedBase01;
			public Color TooltipBackColor { get; set; }
			public Color TooltipForeColor { get; set; }
			public Color TooltipBorderColor { get; set; }
		}			

		public class ThemeStatusBar
		{
			public Color BackColor { get; set; }
			public Color ForeColor { get; set; }
			public Color LineColor { get; set; }
		}			


		public int DefaultButtonAlpha { get; set; }
		public ButtonStyles ButtonStyle { get; set; }

		public virtual Color GetContextColor(ColorContexts colorContext)
		{
			switch (colorContext) {
			case ColorContexts.Default:
				return Colors.Base01;
			case ColorContexts.Active:
				return Colors.Orange;
			case ColorContexts.Information:
				return Colors.Blue;
			case ColorContexts.Success:
				return Colors.Green;
			case ColorContexts.Warning:
				return Colors.Yellow;
			case ColorContexts.Danger:
				return Colors.Red;
			case ColorContexts.Question:
				return Colors.Cyan;			
			case ColorContexts.Outline:
				return Color.Empty;
			default:
				return Colors.Base01;
			}
		}

		public virtual Color GetContextGradientColor(ColorContexts colorContext)
		{
			switch (colorContext) {
			case ColorContexts.Default:
				return Colors.Base02;		
			case ColorContexts.Outline:
				return Color.Empty;
			default:
				return GetContextColor (colorContext).Lerp (Color.Black, 0.15f);
			}
		}

		public virtual Color GetContextBorderColor(ColorContexts colorContext)
		{
			switch (colorContext) {
			case ColorContexts.Default:
				return Colors.Base03;			
			case ColorContexts.Outline:
				return Color.White;
			default:
				return GetContextColor (colorContext).Lerp (Color.Black, 0.25f);
			}
		}

		public virtual Color GetContextForeColor(ColorContexts colorContext)
		{
			switch (colorContext) {
			case ColorContexts.Default:
				return Colors.Base3;			
			case ColorContexts.Outline:
				return Color.White;
			default:				
				if (GetContextColor (colorContext).IsDark())
					return Colors.Base3;
				else
					return Colors.Base03;
			}
		}			
	}

	public static class ThemeExtensions
	{
		public static byte Brightnes(this Color color)
		{
			return (byte)(((int)color.R + (int)color.G + (int)color.B) / 3);
		}

		public static bool IsDark(this Color color)
		{
			return color.Brightnes() < 160;
		}

		public static bool IsLight(this Color color)
		{
			return !color.IsDark();
		}
	}
}

