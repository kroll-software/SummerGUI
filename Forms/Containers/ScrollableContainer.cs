using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
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

		/***
		public override void ResetCachedLayout ()
		{
			base.ResetCachedLayout ();

			if (Children != null) {
				foreach (Widget child in Children) {
					if (child != null)
						child.ResetCachedLayout ();
				}				
			}
		}
		***/

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
			if (Children.Count > 0) {
				RectangleF r = bounds;
				SizeF docSize = new SizeF();

				for (int i = 0; i < Children.Count; i++)
				//for (int i = Children.Count - 1; i >= 0; i--)
				{
					Widget child = Children [i];
					if (child != null && child.Visible) {
						LayoutChild(ctx, child, r);

						if (child.Dock == Docking.Top || child.Dock == Docking.Fill) {
							if (VScrollBar != null && VScrollBar.ZIndex > child.ZIndex) {
								docSize.Height += child.Height + child.Margin.Top;
								if (i == 0)
									docSize.Height += child.Margin.Bottom;
							}
							else
								r.Height -= child.Height;

							r.Y += child.Height;
							if (child.Margin.Bottom > 0)
								r.Offset (0, child.Margin.Bottom);
						}

						if (child.Dock == Docking.Left || child.Dock == Docking.Fill) {
							if (HScrollBar != null && HScrollBar.ZIndex > child.ZIndex) {
								docSize.Width += child.Width + child.Margin.Left;
								if (i == 0)
									docSize.Width += child.Margin.Right;
							}
							else
								r.Width -= child.Width;

							r.X += child.Width;
							if (child.Margin.Right > 0)
								r.Offset (child.Margin.Right, 0);
						}
							
						switch (child.Dock) {
						case Docking.Right:
							r.Width -= child.Width;
							break;
						case Docking.Bottom:
							r.Height -= child.Height;
							break;						
						}							
					}
				}
					
				DocumentSize = docSize;
			}
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
							//LayoutChildren (ctx, Bounds);
						}						
						if (HScrollBar.Visible)
							HScrollBar.SetUp (Width - VScrollBarWidth, DocumentSize.Width);
					}

					if (VScrollBar != null) {						
						bool visible = DocumentSize.Height > Height - HScrollBarHeight;
						if (VScrollBar.Visible != visible) {
							VScrollBar.Visible = visible;
							LayoutChildren (ctx, PaddingBounds);
							//LayoutChildren (ctx, Bounds);
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
			
		public override bool OnMouseWheel (OpenTK.Input.MouseWheelEventArgs e)
		{			
			if (HitTest (e.X, e.Y) == null)
				return false;
			return base.OnMouseWheel (e);
		}
	}
}

