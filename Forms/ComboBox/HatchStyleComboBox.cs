using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Drawing2D;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using KS.Foundation;

namespace SummerGUI
{
	public class HatchStyleComboBox : ComboBoxBase
	{
		
		public HatchStyleComboBox (string name)
			: base (name)
		{
		}

		public override string Text {
			get {
				throw new NotImplementedException ();
			}
			set {
				throw new NotImplementedException ();
			}
		}

		public override void DrawItem(IGUIContext ctx, RectangleF bounds, 
			ComboBoxItem item, IWidgetStyle style)
		{
			// ToDo: DPI Scaling
			bounds.Inflate (-6, 0);
		}
	}
}

