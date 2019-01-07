using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using KS.Foundation;

namespace SummerGUI
{
	public class GradientWidgetStyle : WidgetStyle
	{		
		public ButtonStyles ButtonStyle { get; set; }

		public GradientWidgetStyle()
		{			
		}

		/***
		public GradientWidgetStyle (Color back1, Color back2, Color front, Color border, GradientDirections direction = GradientDirections.Vertical)
		{
			BackColorBrush = new LinearGradientBrush (back1, back2, direction);

			ForeColorPen = new  Pen(front);
			ForeColorBrush = new SolidBrush (front);
			BorderColorPen = new Pen (border);

			if (border == Color.Empty)
				BorderColorPen.Width = 0;
			else
				BorderColorPen.Width = 1;					
		}
		***/

		public override void InitStyle ()
		{
			SetBackColor (Theme.Colors.Base0, Theme.Colors.Base00);
			SetForeColor (Theme.Colors.Base02);
			SetBorderColor (Color.Empty);
		}

		public virtual void SetBackColor(Color color1, Color color2, GradientDirections direction = GradientDirections.Vertical)
		{
			BackColorBrush = new LinearGradientBrush (color1, color2, GradientDirections.Vertical);
		}

		public override void PaintBackground (IGUIContext ctx, Widget widget)
		{
			if (widget.CanPaint) {
				switch (ButtonStyle) {
				case ButtonStyles.Gradient:
					base.PaintBackground (ctx, widget);
					break;
				case ButtonStyles.Glossy:
					ctx.DrawButton (widget.Bounds, BackColorBrush.Color, (BackColorBrush as LinearGradientBrush).GradientColor, BorderColorPen.Color);
					break;
				case ButtonStyles.Flat:
					if (BackColorBrush.Color != Color.Empty)						
						ctx.FillRectangle (new SolidBrush(BackColorBrush.Color), widget.Bounds);

					DrawBorder (ctx, widget);
					break;			
				}
			}
		}
	}
}

