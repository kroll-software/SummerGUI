using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using KS.Foundation;

namespace SummerGUI
{	
	public class BrownStripesWidgetStyle : WidgetStyle
	{
		public BrownStripesWidgetStyle()
			: base(Theme.Colors.Base01, MetroColors.Silver, System.Drawing.Color.Empty) {
		}

		public override void PaintBackground (IGUIContext ctx, Widget widget)
		{
			base.PaintBackground (ctx, widget);

			float deltaX = 12;
			float deltaY = (float)(widget.Bounds.Height);

			float x = widget.Bounds.Left + 1.0f;
			float y = widget.Bounds.Top + 1.0f;

			ctx.FillRectangle (BackColorBrush, x, y, deltaX - 1, deltaY - 1);

			x += deltaX;
			if (x + deltaX > widget.Bounds.Width)
			{
				x = 1;
				y += deltaY;
			}
		}

		// Auch grossartig:
		// Back = SolarizedColors.Base3;
		// Darüber 12 pix Kästchen = SolarizedColors.Base2
		// 1 pix abstand

	}
}

