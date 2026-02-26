using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using KS.Foundation;
using SummerGUI.Scrolling;

namespace SummerGUI
{	
	[Flags]
	public enum ScrollBars
	{
		None = 0,
		Horizontal = 1,
		Vertical = 2,
		Both = Horizontal | Vertical
	}

	public class ScrollableContainer : Container
	{
		public bool AutoScroll { get; set; }

		public HorizontalScrollBar HScrollBar { get; private set; }
		public VerticalScrollBar VScrollBar { get; private set; }

		public ScrollableContainer (string name) : this(name, Docking.Fill, null) {}
		public ScrollableContainer (string name, Docking dock) : this(name, dock, null) {}
		public ScrollableContainer (string name, Docking dock, IWidgetStyle style)
			: base(name, dock, style)
		{		
		}		

		public ScrollBars ScrollBars
		{
			get{
				ScrollBars ret = ScrollBars.None;
				if (HScrollBar != null)
					ret |= ScrollBars.Horizontal;
				if (VScrollBar != null)
					ret |= ScrollBars.Vertical;
				return ret;
			}
			set{
				if (value.HasFlag (ScrollBars.Horizontal)) {
					if (HScrollBar == null) {						
						HScrollBar = AddChild (new HorizontalScrollBar ());
					}
				}
				else {
					ReleaseScrollBar (HScrollBar);				
				}

				if (value.HasFlag (ScrollBars.Vertical)) {
					if (VScrollBar == null) {						
						VScrollBar = AddChild (new VerticalScrollBar ());
					}
				} else {
					ReleaseScrollBar (VScrollBar);				
				}

				// Set margins, respect and reserve some space, when both scrollbars are visible

				float marginFar = 0;
				if (value == ScrollBars.Both) {
					marginFar = ScrollBar.ScrollBarWidth;
				}

				if (HScrollBar != null)
					HScrollBar.Margin = new Padding (HScrollBar.Margin.Left, HScrollBar.Margin.Top, marginFar, HScrollBar.Margin.Bottom);

				if (VScrollBar != null)
					VScrollBar.Margin = new Padding (VScrollBar.Margin.Left, VScrollBar.Margin.Top, VScrollBar.Margin.Right, 0.5f);
			}
		}

		private void ReleaseScrollBar(ScrollBar sb)
		{
			if (sb == null)
				return;

			this.Children.Remove (sb);
			sb.Dispose ();
			sb = null;
		}
			
		public override void OnLayout (IGUIContext ctx, RectangleF bounds)
		{
			if (IsLayoutSuspended || !Visible)
				return;			
			
			base.OnLayout (ctx, bounds);
			SetUpScrollbars (ctx);
		}

		public SizeF DocumentSize { get; protected set; }

		protected override void LayoutChildren(IGUIContext ctx, RectangleF bounds)
		{			
			SizeF docSize = new SizeF();

			if (this.Children.Count > 0) {				
				RectangleF r = bounds;				
				// iterate forward by ZIndex
				for (int i = 0; i < Children.Count; i++)
				{
					Widget child = Children [i];

					if (child.Visible) {						
						LayoutChild(ctx, child, r);

						if (child.Bounds.IsEmpty)
							continue;

						RectangleF cmb = child.MarginBounds;	// Child-Margin-Bounds

						switch (child.Dock) {
						case Docking.Top:							
							r.Height -= cmb.Height;
							r.Y += cmb.Height;
							docSize.Height += cmb.Height;
							break;
						case Docking.Left:
							r.Width -= cmb.Width;
							r.X += cmb.Width;
							docSize.Width += cmb.Width;
							break;
						case Docking.Right:
							r.Width -= cmb.Width;
							if (child != VScrollBar)
								docSize.Width += cmb.Width;
							break;
						case Docking.Bottom:
							r.Height -= cmb.Height;
							if (child != HScrollBar)
								docSize.Height += cmb.Height;
							break;
						case Docking.Fill:
							docSize.Width += cmb.Width;
							docSize.Height += cmb.Height;
							break;
						}							
					}
				}
			}

			DocumentSize = docSize;
		}		

		protected float HScrollBarHeight
		{
			get{
				if (HScrollBar != null && HScrollBar.Visible)
					return HScrollBar.Height;
				return 0;
			}
		}

		protected float VScrollBarWidth
		{
			get{
				if (VScrollBar != null && VScrollBar.Visible)
					return VScrollBar.Width;
				return 0;
			}
		}

		protected override void LayoutChild (IGUIContext ctx, Widget child, RectangleF bounds)
		{
			if (child == VScrollBar) {
				bounds.Offset (Padding.Right, 0);
				bounds.Inflate (0, Padding.Height / 2);
			} else if (child == HScrollBar) {
				bounds.Offset (0, Padding.Bottom);
				bounds.Inflate (Padding.Width / 2, 0);
			} else {				
				bounds.Offset (HScrollBar != null && HScrollBar.IsVisibleEnabled ? -HScrollBar.Value : 0, VScrollBar != null && VScrollBar.IsVisibleEnabled ? -VScrollBar.Value : 0);
			}		

			base.LayoutChild (ctx, child, bounds);
		}

        public EventHandler<EventArgs> VScrollbarVisibleChanged;
        protected virtual void OnVScrollbarVisibleChanged()
        {
            VScrollbarVisibleChanged?.Invoke(this, EventArgs.Empty);
        }

        public EventHandler<EventArgs> HScrollbarVisibleChanged;
        protected virtual void OnHScrollbarVisibleChanged()
        {
            HScrollbarVisibleChanged?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void SetUpScrollbars(IGUIContext ctx)
		{			
			try {
				if (AutoScroll && Visible && Parent != null)
				{
					if (HScrollBar != null) {
						bool visible = DocumentSize.Width > Width - VScrollBarWidth;
						if (HScrollBar.Visible != visible) {
							HScrollBar.Visible = visible;
							LayoutChildren (ctx, PaddingBounds);
                            OnHScrollbarVisibleChanged();
                        }						
						if (HScrollBar.Visible)
							HScrollBar.SetUp (Width - VScrollBarWidth, DocumentSize.Width);
					}

					if (VScrollBar != null) {						
						bool visible = DocumentSize.Height > Height - HScrollBarHeight;
						if (VScrollBar.Visible != visible) {
							VScrollBar.Visible = visible;
							LayoutChildren (ctx, PaddingBounds);
                            OnVScrollbarVisibleChanged();
                        }
						if (VScrollBar.Visible)
							VScrollBar.SetUp (Height - HScrollBarHeight, DocumentSize.Height);
					}


					/*** ToDo; still not perfect ***/
					//CanFocus = (VScrollBar != null && VScrollBar.Enabled) || (HScrollBar != null && HScrollBar.Enabled);

					// m_LastSize = Size;
					// m_LastDocumentSize = DocumentSize;
				}	
			} catch (Exception ex) {
				ex.LogError ();
			}				
		}			
			
		public override void OnPaintBackground (IGUIContext ctx, RectangleF bounds)
		{
			base.OnPaintBackground (ctx, bounds);

			if (HScrollBar != null && VScrollBar != null
				&& HScrollBar.Right < Right 
				&& VScrollBar.Bottom < Bottom) {
				float sbWidth = ScrollBar.ScrollBarWidth;
				ctx.FillRectangle (VScrollBar.Style.BackColorBrush, new RectangleF (bounds.Right - sbWidth, bounds.Bottom - sbWidth, sbWidth, sbWidth));
			}	
		}

		public override void Update (IGUIContext ctx)
		{
			try {		
				if (AutoScroll && ScrollBars != ScrollBars.None) {
					float offsetX = 0;
					float offsetY = 0;

					if (HScrollBar != null && HScrollBar.IsVisibleEnabled)
						offsetX = HScrollBar.Value;					
					if (VScrollBar != null && VScrollBar.IsVisibleEnabled)
						offsetY = VScrollBar.Value;

					Bounds.Offset (-offsetX, -offsetY);
				}

				base.Update (ctx);
			} catch (Exception ex) {
				ex.LogError ();
			}				
		}
			
		public override bool OnMouseWheel (MouseWheelEventArgs e)
		{						
			if (HitTest (e.X, e.Y) == null)
				return false;
			return base.OnMouseWheel (e);
		}		
	}
}
