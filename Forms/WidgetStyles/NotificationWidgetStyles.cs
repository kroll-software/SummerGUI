using System;
using System.Drawing;


namespace SummerGUI
{
	public class NotificationWidgetStyle : WidgetStyle
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

		public NotificationWidgetStyle(ColorContexts colorContext)
		{
			ColorContext = colorContext;
		}

		public override void InitStyle ()
		{
			SetBackColor (Theme.GetContextColor(ColorContext));
			SetForeColor(Theme.GetContextForeColor(ColorContext));
			SetBorderColor (Color.Empty);
		}
	}
}

