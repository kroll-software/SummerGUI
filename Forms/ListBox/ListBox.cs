using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using KS.Foundation;
using System.Drawing;

namespace SummerGUI
{
	public class ListBox : ListBoxBase
	{
		public ListBox (string name)
			: base (name)
		{
		}

        public override void DrawItem(IGUIContext ctx, RectangleF bounds, ListBoxItem item, IWidgetStyle style)
        {
            if (item == null)
				return;
			// ToDo: DPI Scaling
			bounds.Inflate (-TextMargin.Width, 0);
			ctx.DrawString (item.Text, Font, style.ForeColorBrush, bounds, FontFormat.DefaultSingleLine);
        }
	}
}

