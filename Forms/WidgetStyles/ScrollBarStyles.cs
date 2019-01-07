using System;
using System.Linq;
using System.Drawing;
using KS.Foundation;

namespace SummerGUI
{	
	public enum ScrollBarColorSchemes
	{
		Default,
		Dark
	}

	public static class ScollBarWidgetStyleExtensions
	{
		public static void SetColorScheme(this Scrolling.ScrollBar sb, ScrollBarColorSchemes scheme)
		{
			sb.Styles.Styles.OfType<ScrollBarWidgetStyleBase>().ForEach (style => style.ColorScheme = scheme);
			sb.Children.SelectMany(c => c.Styles.Styles.OfType<ScrollBarWidgetStyleBase>())
				.ForEach (style => style.ColorScheme = scheme);
		}
	}

	public abstract class ScrollBarWidgetStyleBase : WidgetStyle
	{
		private ScrollBarColorSchemes m_ColorScheme;
		public ScrollBarColorSchemes ColorScheme
		{
			get{
				return m_ColorScheme;
			}
			set{
				if (m_ColorScheme != value) {
					m_ColorScheme = value;
					InitStyle ();
				}
			}
		}

		protected Theming.SummerGUITheme.ThemeScrollBar SbTheme
		{
			get{
				if (ColorScheme == ScrollBarColorSchemes.Dark)
					return Theme.CurrentTheme.DarkScrollBar;
				return
					Theme.CurrentTheme.ScrollBar;
			}
		}
	}


	public class ScrollChildStyle : ScrollBarWidgetStyleBase
	{
		public override void InitStyle ()
		{
			SetBackColor (SbTheme.BackColor);
			SetForeColor (SbTheme.ButtonForeColor);
			SetBorderColor (Color.Empty);
		}
	}

	public class ScrollButtonStyleDisabled : ScrollBarWidgetStyleBase
	{
		public override void InitStyle ()
		{
			SetBackColor (SbTheme.BackColor);
			SetForeColor (SbTheme.ButtonForeColorDisabled);
			SetBorderColor (Color.Empty);
		}
	}

	public class ScrollButtonStyleHover : ScrollBarWidgetStyleBase
	{
		public override void InitStyle ()
		{
			SetBackColor (SbTheme.ButtonColorHover);
			SetForeColor (SbTheme.ButtonForeColor);
			SetBorderColor (Color.Empty);
		}
	}

	public class ScrollButtonStylePressed : ScrollBarWidgetStyleBase
	{
		public override void InitStyle ()
		{
			SetBackColor (SbTheme.ButtonColorPressed);
			SetForeColor (SbTheme.ButtonForeColorPressed);
			SetBorderColor (Color.Empty);
		}
	}

	public class ScrollGripStyle : ScrollBarWidgetStyleBase
	{
		public override void InitStyle ()
		{
			SetBackColor (SbTheme.GripColor);
			SetForeColor (SbTheme.ButtonForeColor);
			SetBorderColor (SbTheme.GripBorderColor);
			BorderDistance = 0;
		}
	}

	public class ScrollGripHoverStyle : ScrollBarWidgetStyleBase
	{
		public override void InitStyle ()
		{
			SetBackColor (SbTheme.GripColorHover);
			SetForeColor (SbTheme.ButtonForeColor);
			SetBorderColor (SbTheme.GripBorderColorHover);
			BorderDistance = 0;
		}
	}

	public class ScrollGripMovingStyle : ScrollBarWidgetStyleBase
	{
		public override void InitStyle ()
		{
			SetBackColor (SbTheme.GripColorDrag);
			SetForeColor (SbTheme.ButtonForeColor);
			SetBorderColor (SbTheme.GripBorderColorDrag);
			BorderDistance = 0;
		}
	}

	public class ScrollBarStyle : ScrollBarWidgetStyleBase
	{
		public override void InitStyle ()
		{
			SetBackColor (SbTheme.BackColor);
			SetForeColor (SbTheme.ButtonForeColor);
			SetBorderColor (SbTheme.BackColor);
		}
	}
}

