using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using OpenTK.Input;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using KS.Foundation;

namespace SummerGUI
{	
	public class OverlayContainer : ScrollableContainer, IOverlayWidget
	{	
		public OverlayModes OverlayMode { get; protected set; }

		public event EventHandler<EventArgs> Closing;
		public virtual void OnClose()
		{
			if (Closing != null && !IsDisposed)
				Closing (this, EventArgs.Empty);
		}

		public OverlayContainer (string name, Docking dock, IWidgetStyle style)
			: base (name, dock, style)
		{
			ZIndex = 10000;
			AutoScroll = true;
			CanFocus = true;
		}			
			
		public override bool OnMouseWheel (MouseWheelEventArgs e)
		{			
			// allways return true
			if (OverlayMode == OverlayModes.Modal || OverlayMode == OverlayModes.Window)
				return true;

			return base.OnMouseWheel (e);
		}

		public override bool OnKeyDown (KeyboardKeyEventArgs e)
		{			
			// eat Tab-Keys..
			if (IsFocused && e.Key == Keys.Tab)
				return true;
			return false;
		}

		public override void Update (IGUIContext ctx)
		{
			if (!HasSize || Parent == null)
				return;

			base.Update (ctx);			
		}
	}
}

