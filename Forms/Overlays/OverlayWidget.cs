using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using OpenTK.Input;
using KS.Foundation;

namespace SummerGUI
{
	public enum OverlayModes
	{
		Overlay,
		Window,
		Modal,
		Timer
	}

	public interface IOverlayWidget
	{
		OverlayModes OverlayMode { get; }
		event EventHandler<EventArgs> Closing;
		void OnClose();
	}

	public class OverlayWidget : Widget, IOverlayWidget
	{
		public OverlayModes OverlayMode { get; protected set; }

		public event EventHandler<EventArgs> Closing;
		public virtual void OnClose()
		{
			if (Closing != null)
				Closing (this, EventArgs.Empty);
		}

		public OverlayWidget (string name, IWidgetStyle style)
			: base(name, Docking.None, style)
		{
			ZIndex = 10000;
		}

		public override bool OnMouseWheel (MouseWheelEventArgs e)
		{			
			// allways return true
			//if (Visible && (OverlayMode == OverlayModes.Modal || OverlayMode == OverlayModes.Window || OverlayMode == OverlayModes.Timer))
			if (OverlayMode == OverlayModes.Modal || OverlayMode == OverlayModes.Window)
				return true;

			return base.OnMouseWheel (e);
		}
	}
}

