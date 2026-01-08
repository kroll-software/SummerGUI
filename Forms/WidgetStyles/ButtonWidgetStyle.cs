using System;
using System.Drawing;

namespace SummerGUI
{	
	public class ButtonStyle : WidgetStyle
	{		
		public override void InitStyle ()
		{			
			SetBackColor (Color.Empty);
			SetForeColor (Theme.Colors.White);
			SetBorderColor (Color.Empty);
		}
	}

	public class ButtonWidgetStyle : GradientWidgetStyle
	{
		private ColorContexts m_ColorContext;
		public ColorContexts ColorContext
		{
			get{
				return m_ColorContext;
			}
			set{
				if (m_ColorContext != value) {
					m_ColorContext = value;
					InitStyle ();
				}
			}
		}

		public ButtonWidgetStyle(ColorContexts colorContext)
		{
			ColorContext = colorContext;
		}

		public override void InitStyle ()
		{
			SetBackColor (Color.FromArgb(Theme.DefaultButtonAlpha, Theme.GetContextColor(ColorContext)), Color.FromArgb(Theme.DefaultButtonAlpha, Theme.GetContextGradientColor(ColorContext)));
			SetForeColor(Theme.GetContextForeColor(ColorContext));
			SetBorderColor (Theme.GetContextBorderColor(ColorContext));
			ButtonStyle = Theme.ButtonStyle;
		}

		/***
		public override void DrawBorder (IGUIContext ctx, Widget widget)
		{			
			if (widget == null || Border <= 0.001)
				return;
			
			RectangleF bounds = widget.Bounds;
			float border = Border * 2f;
			bounds.Offset (border, border);
			using (var brush = new SolidBrush(BorderColorPen.Color))
				ctx.FillRectangle (brush, bounds, 0.5f);
		}
		***/
	}

	public class ButtonHoverWidgetStyle : ButtonWidgetStyle
	{
		public ButtonHoverWidgetStyle(ColorContexts colorContext) : base(colorContext) {}

		public override void InitStyle ()
		{
			SetBackColor (Color.FromArgb(Theme.DefaultButtonAlpha, Theme.GetContextHoverColor(ColorContext)), Color.FromArgb(Theme.DefaultButtonAlpha, Theme.GetContextGradientColor(ColorContext)));
			SetForeColor(Theme.GetContextForeColor(ColorContext));
			SetBorderColor (Theme.GetContextBorderColor(ColorContext));
			ButtonStyle = Theme.ButtonStyle;
		}
	}

	public class ButtonPressedWidgetStyle : ButtonWidgetStyle
	{
		public ButtonPressedWidgetStyle(ColorContexts colorContext) : base(colorContext) {}

		public override void InitStyle ()
		{
			SetBackColor (Color.FromArgb(Theme.DefaultButtonAlpha, Theme.GetContextGradientColor(ColorContext)), Color.FromArgb(Theme.DefaultButtonAlpha, Theme.GetContextGradientColor(ColorContext)));
			SetForeColor(Theme.GetContextForeColor(ColorContext));
			SetBorderColor (Theme.GetContextBorderColor(ColorContext));
			ButtonStyle = Theme.ButtonStyle;
		}
	}

	public class ButtonDisabledWidgetStyle : ButtonWidgetStyle
	{
		public ButtonDisabledWidgetStyle(ColorContexts colorContext) : base(colorContext) {}

		public override void InitStyle ()
		{
			//SetBackColor (Color.FromArgb(Theme.DefaultButtonAlpha, Theme.GetContextDisabledColor(ColorContext)), Color.FromArgb(Theme.DefaultButtonAlpha, Theme.GetContextDisabledColor(ColorContext)));
			SetBackColor (Color.FromArgb(Theme.DefaultButtonAlpha, Theme.GetContextColor(ColorContext)), Color.FromArgb(Theme.DefaultButtonAlpha, Theme.GetContextGradientColor(ColorContext)));
            SetForeColor(Theme.Colors.Base1);
            //SetBorderColor (Theme.GetContextDisabledBorderColor(ColorContext));
            SetBorderColor (Theme.GetContextBorderColor(ColorContext));
			ButtonStyle = Theme.ButtonStyle;
		}
	}


	// **** OUTLINE Button ****

	public class OutlineButtonWidgetStyle : WidgetStyle
	{		
		private ColorContexts m_ColorContext;
		public ColorContexts ColorContext
		{
			get{
				return m_ColorContext;
			}
			set{
				if (m_ColorContext != value) {
					m_ColorContext = value;
					InitStyle ();
				}
			}
		}

		public OutlineButtonWidgetStyle(ColorContexts colorContext)
		{
			ColorContext = colorContext;
		}

		public override void InitStyle ()
		{			
			SetBackColor (Color.Empty);
			SetForeColor(Color.White);
			SetBorderColor(Color.White);
			//SetBorderColor (Theme.Colors.Base2);
			Border = 3f;
		}			
	}

	public class OutlineButtonInvertedWidgetStyle : OutlineButtonWidgetStyle
	{		
		public OutlineButtonInvertedWidgetStyle(ColorContexts colorContext)
			: base(colorContext)
		{			
		}

		public override void InitStyle ()
		{			
			SetBackColor (Theme.Colors.White);
			SetForeColor(Theme.GetContextColor(ColorContext));
			SetBorderColor(Theme.GetContextColor(ColorContext));
			Border = 3f;
			this.BorderDistance = -3.5f;
		}			
	}

	public class OutlineButtonPressedWidgetStyle : OutlineButtonWidgetStyle
	{		
		public OutlineButtonPressedWidgetStyle(ColorContexts colorContext)
			: base(colorContext)
		{			
		}

		public override void InitStyle ()
		{			
			SetBackColor (Color.FromArgb(178, Theme.Colors.White));
			SetForeColor(Theme.GetContextColor(ColorContext));
			SetBorderColor(Color.FromArgb(128, Theme.GetContextColor(ColorContext)));
			Border = 3f;
		}			
	}

	public class OutlineButtonDisabledWidgetStyle : OutlineButtonWidgetStyle
	{		
		public OutlineButtonDisabledWidgetStyle(ColorContexts colorContext)
			: base(colorContext)
		{			
		}

		public override void InitStyle ()
		{			
			SetBackColor (Color.Empty);
			SetForeColor(Theme.Colors.Silver);
			SetBorderColor (Theme.Colors.Silver);
			Border = 3f;
		}			
	}

	// *** Silver / Yellow Dialog Button ***

	public class SilverButtonWidgetStyle : GradientWidgetStyle
	{		
		public override void InitStyle ()
		{			
			SetBackColor (Color.FromArgb(Theme.DefaultButtonAlpha, Theme.GetContextColor(ColorContexts.Default)), Color.FromArgb(Theme.DefaultButtonAlpha, Theme.GetContextGradientColor(ColorContexts.Default)));
			SetForeColor(Theme.GetContextForeColor(ColorContexts.Default));
			SetBorderColor (Theme.GetContextBorderColor(ColorContexts.Default));
			ButtonStyle = Theme.ButtonStyle;		
		}
	}

	public class SilverButtonActiveWidgetStyle : GradientWidgetStyle
	{		
		public override void InitStyle ()
		{
			SetBackColor (Color.FromArgb(Theme.DefaultButtonAlpha, Theme.GetContextColor(ColorContexts.Active)), Color.FromArgb(Theme.DefaultButtonAlpha, Theme.GetContextGradientColor(ColorContexts.Active)));
			SetForeColor(Theme.GetContextForeColor(ColorContexts.Active));
			SetBorderColor (Theme.GetContextBorderColor(ColorContexts.Active));
			ButtonStyle = Theme.ButtonStyle;
		}
	}

	public class SilverButtonPressedWidgetStyle : GradientWidgetStyle
	{		
		public override void InitStyle ()
		{
			SetBackColor (Color.FromArgb(Theme.DefaultButtonAlpha, Theme.GetContextGradientColor(ColorContexts.Active)), Color.FromArgb(Theme.DefaultButtonAlpha, Theme.GetContextGradientColor(ColorContexts.Active)));
			SetForeColor(Theme.GetContextForeColor(ColorContexts.Active));
			SetBorderColor (Theme.GetContextBorderColor(ColorContexts.Active));
			ButtonStyle = Theme.ButtonStyle;
		}
	}

	public class SilverButtonDisabledWidgetStyle : GradientWidgetStyle
	{		
		public override void InitStyle ()
		{
			SetBackColor (Color.FromArgb(Theme.DefaultButtonAlpha, Theme.GetContextDisabledColor(ColorContexts.Default)), Color.FromArgb(Theme.DefaultButtonAlpha, Theme.GetContextDisabledColor(ColorContexts.Default)));
			SetForeColor(Theme.Colors.Base3);
			SetBorderColor (Theme.GetContextDisabledBorderColor(ColorContexts.Default));
			ButtonStyle = Theme.ButtonStyle;
		}
	}

	public class SilverButtonDefaultHoverWidgetStyle : GradientWidgetStyle
	{		
		public override void InitStyle ()
		{
			SetBackColor (Color.FromArgb(Theme.DefaultButtonAlpha, Theme.GetContextHoverColor(ColorContexts.Default)), Color.FromArgb(Theme.DefaultButtonAlpha, Theme.GetContextGradientColor(ColorContexts.Default)));
			SetForeColor(Theme.GetContextForeColor(ColorContexts.Default));
			SetBorderColor (Theme.GetContextBorderColor(ColorContexts.Default));
			ButtonStyle = Theme.ButtonStyle;
		}
	}

	public class SilverButtonActiveHoverWidgetStyle : GradientWidgetStyle
	{		
		public override void InitStyle ()
		{
			SetBackColor (Color.FromArgb(Theme.DefaultButtonAlpha, Theme.GetContextHoverColor(ColorContexts.Active)), Color.FromArgb(Theme.DefaultButtonAlpha, Theme.GetContextGradientColor(ColorContexts.Active)));
			SetForeColor(Theme.GetContextForeColor(ColorContexts.Active));
			SetBorderColor (Theme.GetContextBorderColor(ColorContexts.Active));
			ButtonStyle = Theme.ButtonStyle;
		}
	}

}

