using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using OpenTK.Input;
using KS.Foundation;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using SummerGUI.Scrolling;

namespace SummerGUI
{
	public enum ScrollOrientation
	{
		HorizontalScroll,
		VerticalScroll
	}

	public class HorizontalScrollBar : ScrollBar
	{
		public HorizontalScrollBar () : base("hscroll", Docking.Bottom)
		{			
			this.ScrollOrientation = ScrollOrientation.HorizontalScroll;

			FirstButton = new ScrollButton("start", Alignment.Near, Docking.Left);
			LastButton = new ScrollButton("end", Alignment.Far, Docking.Right);
			Grip = new HorizontalScrollGrip ();

			Children.Add (FirstButton);
			Children.Add (LastButton);
			Children.Add (Grip);

			FirstButton.Enabled = false;
			LastButton.Enabled = false;
		}

		public override SizeF PreferredSize (IGUIContext ctx, SizeF proposedSize)
		{			
			return new SizeF (proposedSize.Width, ScrollBar.ScrollBarWidth);
		}

		public override RectangleF ScrollBounds {
			get{
				return new RectangleF (ScrollBarWidth, 0, Width  - (ScrollBarWidth * 2), Height);
			}
		}

		public override float ScrollSize
		{
			get{
				return Bounds.Width - FirstButton.Width - LastButton.Width;
			}
		}

		public override float WindowSize
		{ 
			get {				
				if (base.WindowSize > 0)
					return base.WindowSize;
				return this.Width;
			}
			set {
				base.WindowSize = value;
			}
		}

		public override float DocumentSize
		{
			get{
				if (base.DocumentSize > 0 || Parent == null)
					return base.DocumentSize;
				return Parent.Width;
			}
			set {
				base.DocumentSize = value;
			}
		}

		public override void OnMouseDown (MouseButtonEventArgs e)
		{								
			if (LastMouseDownMousePosition.X < Grip.Left)
				Value -= LargeChange;
			else if (LastMouseDownMousePosition.X > Grip.Right)
				Value += LargeChange;
			else
				return;			

			Invalidate ();
			if (Enabled && WidgetState == WidgetStates.Pressed)
				StartTimer ();
		}
	}

	public class VerticalScrollBar : ScrollBar
	{		
		public VerticalScrollBar () : base("vscroll", Docking.Right)
		{			
			this.ScrollOrientation = ScrollOrientation.VerticalScroll;

			FirstButton = new ScrollButton("start", Alignment.Near, Docking.Top);
			LastButton = new ScrollButton("end", Alignment.Far, Docking.Bottom);
			Grip = new VerticalScrollGrip ();

			Children.Add (FirstButton);
			Children.Add (LastButton);
			Children.Add (Grip);

			FirstButton.Enabled = false;
			LastButton.Enabled = false;
		}

		public override SizeF PreferredSize (IGUIContext ctx, SizeF proposedSize)
		{			
			return new SizeF (ScrollBar.ScrollBarWidth, proposedSize.Height);
		}

		public override RectangleF ScrollBounds {
			get{
				return new RectangleF (0, ScrollBarWidth, Width, Height - (ScrollBarWidth * 2f));
			}
		}

		public override float ScrollSize
		{
			get{				
				return Bounds.Height - (2f * ScrollBar.ScrollBarWidth);
			}
		}

		public override float WindowSize
		{ 
			get {				
				if (base.WindowSize > 0)
					return base.WindowSize;
				return ScrollSize;
			}
			set {
				base.WindowSize = value;
			}
		}

		public override float DocumentSize
		{
			get{
				if (base.DocumentSize > 0 || Parent == null)
					return base.DocumentSize;
				return Parent.Height;
			}
			set {
				base.DocumentSize = value;
			}
		}			

		public override void OnMouseDown (MouseButtonEventArgs e)
		{							
			if (LastMouseDownMousePosition.Y < Grip.Top)
				Value -= LargeChange;
			else if (LastMouseDownMousePosition.Y > Grip.Bottom)
				Value += LargeChange;			
			else
				return;

			Invalidate ();
			if (Enabled && WidgetState == WidgetStates.Pressed)
				StartTimer ();			
		}

		/// <summary>
		/// Returns true when handled
		/// </summary>
		/// <param name="e">E.</param>
		public override bool OnMouseWheel (MouseWheelEventArgs e)
		{			
			if (!Visible)
				return false;

			if (!Enabled)
				return true;

			Value -= SmallChange * e.OffsetY;
			return true;
		}

		public void ScrollUp()
		{
			if (IsVisibleEnabled)
				Value -= SmallChange;
		}

		public void ScrollDown()
		{
			if (IsVisibleEnabled)
				Value += SmallChange;
		}

		public void ScrollPageUp()
		{
			if (IsVisibleEnabled)
				Value -= LargeChange;
		}

		public void ScrollPageDown()
		{
			if (IsVisibleEnabled)
				Value += LargeChange;
		}			

		public override bool OnKeyDown (KeyboardKeyEventArgs e)
		{
			if (!IsVisibleEnabled)
				return false;

			bool handled = false;

			switch (e.Key) {
			case Keys.Down:
				Value += SmallChange;
				handled = true;
				break;
			case Keys.Up:
				Value -= SmallChange;
				handled = true;
				break;
			case Keys.PageDown:
				Value += LargeChange;
				handled = true;
				break;
			case Keys.PageUp:
				Value -= LargeChange;
				handled = true;
				break;
			case Keys.Home:
				Value = Minimum;
				handled = true;
				break;
			case Keys.End:
				Value = Maximum;
				handled = true;
				break;
			}

			if (handled) {
				Invalidate ();
				return true;
			}

			return false;
		}
	}		
}


