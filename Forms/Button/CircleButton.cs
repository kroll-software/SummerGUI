using System;
using System.Drawing;
using KS.Foundation;

namespace SummerGUI
{
	public class CircleButton : ImageButton
	{
		public CircleButton(string name, Docking dock, string text, ImageList imageList, string imageKey, WidgetStyle style)
			: base (name, dock, text, imageList, imageKey, style)
		{
			if ((style as CircleWidgetStyle) == null) {
				this.LogWarning ("Style should be of type CircleWidgetStyle");;
			}

			Image.VAlign = Alignment.Near;
			Padding = Padding.Empty;
		}

		public override Widget HitTest (float x, float y)
		{
			// TODO: exact hit-test on a circle
			return base.HitTest (x, y);
		}
	}
}

