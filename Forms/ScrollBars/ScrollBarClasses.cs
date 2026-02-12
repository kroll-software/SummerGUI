using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using OpenTK.Input;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using KS.Foundation;

namespace SummerGUI.Scrolling
{		
	// ****************************** SCroll-Grip *******************************

	/// <summary>
	/// Scroll Grip, provides a gripper to the ScrollBar
	/// </summary>
	public abstract class ScrollGripBase : Widget
	{
		protected ScrollGripBase () : base("grip", Docking.None, new ScrollGripStyle()) 
		{				
			ZIndex = 100;				
			Styles.SetStyle(new ScrollGripHoverStyle(), WidgetStates.Hover);
			Styles.SetStyle(new ScrollGripMovingStyle(), WidgetStates.Pressed);
		}

		protected ScrollBar ParentScroll
		{
			get{
				return Parent as ScrollBar;
			}
		}

        public override void Focus()
        {
            if (Parent != null)
				Parent.Focus();
        }

		public override void OnPaintBackground (IGUIContext ctx, RectangleF bounds)
		{
			if (ParentScroll.Enabled)
				base.OnPaintBackground (ctx, bounds);
		}
	}

	public class VerticalScrollGrip : ScrollGripBase
	{
		public VerticalScrollGrip() : base() {}

		public override void OnLayout (IGUIContext ctx, RectangleF bounds)
		{					
			ScrollBar sb = ParentScroll;
			if (sb == null || sb.Maximum - sb.Minimum <= 0)
				return;

			float gripSize = sb.GripSize;
			float scrollSize = sb.ScrollSize;

			if (gripSize < ScrollBar.MinGripSize) {
				scrollSize -= (ScrollBar.MinGripSize - gripSize);
				gripSize = ScrollBar.MinGripSize;
			}

			if (scrollSize <= 0)
				return;

			float factor = ((sb.Maximum - sb.Minimum) / scrollSize);

			float top = (sb.Value / factor) + ScrollBar.ScrollBarWidth + sb.Top;
			this.SetBounds(new RectangleF (bounds.Right - ScrollBar.ScrollBarWidth + 2, top, 
				ScrollBar.ScrollBarWidth - 4, gripSize));
		}			

		public override Widget HitTest (float x, float y)
		{
			RectangleF r = new RectangleF (x, y, 1, 1);
			RectangleF rBounds = new RectangleF (Bounds.Left - 2, Bounds.Top, Bounds.Width + 4, Bounds.Height);

			if (!rBounds.IntersectsWith (r))
				return null;

			return this;
		}

		public override void OnMouseMove (MouseMoveEventArgs e)
		{						
			if (WidgetState != WidgetStates.Pressed)
				return;

			ScrollBar sb = ParentScroll;
			if (sb == null)
				return;

			float scrollSize = sb.ScrollSize;
			if (scrollSize <= 0 || sb.Maximum - sb.Minimum <= 0)
				return;

			//Debug.WriteLine (e.Y, "e.Y");

			float factor = ((sb.Maximum - sb.Minimum) / scrollSize);
			float y = e.Y - sb.Top - (LastMouseDownMousePosition.Y - LastMouseDownUpperLeft.Y) - ScrollBar.ScrollBarWidth;
			sb.Value =  y * factor;
		}
	}

	public class HorizontalScrollGrip : ScrollGripBase
	{
		public HorizontalScrollGrip() : base() {}

		public override void OnLayout (IGUIContext ctx, RectangleF bounds)
		{					
			ScrollBar sb = ParentScroll;
			if (sb == null || sb.Maximum - sb.Minimum <= 0)
				return;

			float gripSize = sb.GripSize;
			float scrollSize = sb.ScrollSize;		

			if (gripSize < ScrollBar.MinGripSize) {
				scrollSize -= (ScrollBar.MinGripSize - gripSize);
				gripSize = ScrollBar.MinGripSize;
			}

			if (scrollSize <= 0)
				return;

			float factor = ((sb.Maximum - sb.Minimum) / scrollSize);

			float left = (sb.Value / factor) + ScrollBar.ScrollBarWidth + sb.Left;
			this.SetBounds(new RectangleF (left, bounds.Bottom - ScrollBar.ScrollBarWidth + 2,
				gripSize, ScrollBar.ScrollBarWidth - 4));
		}

		public override Widget HitTest (float x, float y)
		{
			RectangleF r = new RectangleF (x, y, 1, 1);
			RectangleF rBounds = new RectangleF (Bounds.Left, Bounds.Top - 2, Bounds.Width, Bounds.Height + 4);

			if (!rBounds.IntersectsWith (r))
				return null;

			return this;
		}

		public override void OnMouseMove (MouseMoveEventArgs e)
		{						
			if (WidgetState != WidgetStates.Pressed)
				return;

			ScrollBar sb = ParentScroll;
			if (sb == null)
				return;

			float scrollSize = sb.ScrollSize;
			if (scrollSize <= 0 || sb.Maximum - sb.Minimum <= 0)
				return;

			float factor = ((sb.Maximum - sb.Minimum) / scrollSize);
			float x = e.X - sb.Left - (LastMouseDownMousePosition.X - LastMouseDownUpperLeft.X) - ScrollBar.ScrollBarWidth;
			sb.Value =  x * factor;
		}
	}


	// ****************************** SCroll-Button *******************************

	/// <summary>
	/// Scroll Button, provides a button with a little triangle for the ScrollBar
	/// to handle SmallChange on Click
	/// </summary>
	public class ScrollButton : Widget
	{
		public ScrollOrientation ScrollOrientation 
		{ 
			get{ 
				if ((Parent as ScrollBar) == null)
					return ScrollOrientation.VerticalScroll;
				return (Parent as ScrollBar).ScrollOrientation;
			}
		}
		public Alignment Align { get; private set; }

		public ScrollButton (string name, Alignment align, Docking dock) 
			: base(name, dock, new ScrollChildStyle()) 
		{
			Align = align;
			Styles.SetStyle(new ScrollButtonStyleDisabled(), WidgetStates.Disabled);
			Styles.SetStyle(new ScrollButtonStyleHover(), WidgetStates.Hover);
			Styles.SetStyle(new ScrollButtonStylePressed(), WidgetStates.Pressed);
			Padding = new Padding (7);		
		}			

		public override SizeF PreferredSize (IGUIContext ctx, SizeF proposedSize)
		{
			return new SizeF(ScrollBar.ScrollBarWidth, ScrollBar.ScrollBarWidth);
		}

		// Continuous Button Press with a timer >>>>>>>>>>	

		TaskTimer Timer;
		void InitTimer()
		{
			if (Timer == null)
				Timer = new TaskTimer (50, Fire, 500);
		}

		private void Fire()
		{
			OnMouseDown (null);
		}
			
		public override void OnMouseDown (MouseButtonEventArgs e)
		{	
			base.OnMouseDown (e);

			ScrollBar sb = Parent as ScrollBar;
			if (sb != null && sb.Visible && sb.Enabled) {
				switch (this.Align) {
				case Alignment.Near:
					sb.Value -= sb.SmallChange;
					break;
				case Alignment.Far:
					sb.Value += sb.SmallChange;
					break;
				}

				Invalidate ();
				if (e != null && Enabled && WidgetState == WidgetStates.Pressed) {
					InitTimer ();
					Timer.Start ();
				}
			}				
		}

		public override void OnMouseUp (MouseButtonEventArgs e)
		{						
			if (Timer != null)
				Timer.Stop ();
			base.OnMouseUp (e);
		}

		public override void Focus()
        {
            if (Parent != null)
				Parent.Focus();
        }

		// *************** Painting ******************

		public override void OnPaint (IGUIContext ctx, RectangleF bounds)
		{
			base.OnPaint (ctx, bounds);		
			
			float iMax = 0, x = 0, y = 0;

			switch (ScrollOrientation) {
			case ScrollOrientation.VerticalScroll:
				x = Left + Width / 2f;
				iMax = Width / 2f - Padding.Width / 4f;
				if (Align == Alignment.Near) {
					y = Top + Padding.Top;
					for (int i = 0; i < iMax; i++) {
						ctx.DrawLine(Style.ForeColorPen, x - i, y, i + x + 1, y);
						y++;
					}
				} else {
					y = Bottom - Padding.Bottom;
					for (int i = 0; i < iMax; i++) {					
						ctx.DrawLine(Style.ForeColorPen, x - i, y, i + x + 1, y);
						y--;
					}
				}
				break;

			case ScrollOrientation.HorizontalScroll:
				y = Top + Height / 2f;
				iMax = Height / 2f - Padding.Width / 4f;
				if (Align == Alignment.Near) {
					x = Left + Padding.Left;
					for (int i = 0; i < iMax; i++) {					
						ctx.DrawLine(Style.ForeColorPen, x, y - i, x, i + y + 1);
						x++;
					}
				} else {
					x = Right - Padding.Right;
					for (int i = 0; i < iMax; i++) {					
						ctx.DrawLine(Style.ForeColorPen, x, y - i, x, i + y + 1);
						x--;
					}
				}
				break;
			}		
		}

		protected override void CleanupManagedResources ()
		{
			if (Timer != null)
				Timer.Dispose();
			base.CleanupManagedResources ();
		}			
	}


	// ****************************** SCROLLBAR WIDGET *******************************

	/// <summary>
	/// Scroll bar.
	/// Hosts two ScrollButtons and a ScrollGripper
	/// </summary>
	public abstract class ScrollBar : Container
	{
		// TODO: this has to be scaled
		[DpiScalable]
		public static float ScrollBarWidth = 17;

		public static float MinGripSize
		{
			get{
				//return ScrollBarWidth * 2f / 3f + 1f;				
				//return ScrollBarWidth - 2f;
				return ScrollBarWidth;
			}
		}

		public ScrollOrientation ScrollOrientation { get; protected set; }

		public ScrollButton FirstButton { get; protected set; }
		public ScrollButton LastButton { get; protected set; }
		public ScrollGripBase Grip { get; protected set; }

		public event EventHandler<EventArgs> Scroll;
		public void OnScroll()
		{			
			if (Scroll != null)
				Scroll (this, EventArgs.Empty);
		}

		public abstract RectangleF ScrollBounds { get; }
		public abstract float ScrollSize { get; }

		private float m_WindowSize = -1;
		public virtual float WindowSize
		{ 
			get {				
				return m_WindowSize;
			}
			set {
				m_WindowSize = value;
				ResetCachedLayout ();
			}
		}

		private float m_DocumentSize = -1;
		public virtual float DocumentSize
		{ 
			get {				
				return m_DocumentSize;
			}
			set {
				m_DocumentSize = value;
				ResetCachedLayout ();
			}
		}			

		public float GripSize{
			get{
				if (DocumentSize <= 0)
					return 0;

				return WindowSize * ScrollSize / DocumentSize;
			}
		}

		public bool NeedsScrollBar
		{
			get{
				return DocumentSize > WindowSize;
			}
		}

		private void AdjustValue()
		{
			m_Value = Math.Max(Minimum, Math.Min(Maximum - LargeChange + 1f, m_Value));
		}

		private float m_Value = 0;
		public float Value  
		{ 
			get {
				return m_Value;
			}
			set {
				if (m_Value != value) {
					ResetCachedLayout ();
					m_Value = Math.Max(Minimum, Math.Min(Maximum - LargeChange + 1, value));

					FirstButton.Enabled = m_Value > Minimum;
					LastButton.Enabled = m_Value + LargeChange < Maximum;

					OnScroll ();
					Invalidate ();
				}
			}
		}

		public float SmallChange  { get; set; }
		public float LargeChange  { get; set; }
		public float Minimum  { get; set; }
		public float Maximum  { get; set; }

		protected ScrollBar (string name, Docking dock)
			: base(name, dock, new ScrollBarStyle())
		{
			// without this, the pressed state can't be activated
			this.Styles.SetStyle (new ScrollBarStyle (), WidgetStates.Pressed);
			ZIndex = 1000;
		}

		public void SetUp(float windowSize, float documentSize, float smallChange = 0)
		{			
			if (windowSize <= 0 || documentSize <= 0) {
				Enabled = false;
				LastButton.Enabled = false;
				return;
			}

			m_WindowSize = windowSize;
			m_DocumentSize = documentSize;

			Minimum = 0;
			Maximum = Math.Max(0, documentSize);

			SmallChange = 0;
			LargeChange = windowSize;

			if (smallChange > 0)
				SmallChange = smallChange;
			else
				SmallChange = Maximum / 12f;

			bool needed = NeedsScrollBar;

			Enabled = needed;
			AdjustValue();
			LastButton.Enabled = needed && (Value + LargeChange - 1) < Maximum;
		}
			
		private TaskTimer m_Timer = null;
		protected void StartTimer()
		{
			try {
				if (m_Timer == null)
					m_Timer = new TaskTimer (50, () => {
						if (Enabled && WidgetState == WidgetStates.Pressed)
							OnMouseDown(null);	
					}, 500);
				m_Timer.Start ();	
			} catch (Exception ex) {
				ex.LogError ();
			}
		}

		protected void StopTimer()
		{
			try {
				if (m_Timer != null) {
					m_Timer.Stop ();
					m_Timer.Dispose ();
					m_Timer = null;
				}	
			} catch (Exception ex) {
				ex.LogError ();
			}
		}

		public override void OnMouseUp (MouseButtonEventArgs e)
		{			
			StopTimer ();
			base.OnMouseUp (e);
		}

		public void ScrollFirst()
		{
			if (IsVisibleEnabled)
				Value = Minimum;
		}

		public void ScrollLast()
		{
			if (IsVisibleEnabled)
				Value = Maximum;
		}

		public override void Focus()
        {
            if (Parent != null && Parent.CanFocus)
				Parent.Focus();
        }

		protected override void CleanupManagedResources ()
		{
			StopTimer ();
			base.CleanupManagedResources ();
		}
	}
}

