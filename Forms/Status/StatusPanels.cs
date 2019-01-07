using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using KS.Foundation;

namespace SummerGUI
{
	public class StatusTextPanel : TextWidget
	{		
		public StatusTextPanel(string name, Docking dock, string text = null)
			: base (name, dock, new StatusPanelStyle(), text, null)
		{			
			Format = FontFormat.DefaultSingleLine;
			Margin = Padding.Empty;
			InvalidateOnHeartBeat = true;
		}			

		public override void OnResize ()
		{
			base.OnResize ();
			Invalidate ();
		}
	}

	public class StatusProgressPanel : ProgressBar
	{		
		public StatusProgressPanel(string name)
			: base (name)
		{			
			ProgressPadding = 3;
			Padding = Padding.Empty;
			Margin = Padding.Empty;

			// ToDo: DPI Scaling
			MaxSize = new Size (140, Int32.MaxValue);
			Dock = Docking.Right;
		}

		public override SizeF PreferredSize (IGUIContext ctx, SizeF proposedSize)
		{			
			// ToDo: DPI Scaling

			if (Font == null)
				return new SizeF(140, 16);

			return new SizeF(140, Font.Height);
		}
	}
}

