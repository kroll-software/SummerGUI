using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using KS.Foundation;
using SummerGUI.Splitting;

namespace SummerGUI
{
	public class TripleSplitContainer : Container
	{
		private bool m_Panel1Collapsed;
		public bool Panel1Collapsed {
			get {
				return m_Panel1Collapsed;
			}
			set {				
				if (m_Panel1Collapsed != value) {
					m_Panel1Collapsed = value;
					Panel1.Visible = !value;
					//Splitter1.Visible = !value;
					Invalidate ();
				}
			}
		}
			
		private bool m_Panel2Collapsed;
		public bool Panel2Collapsed {
			get {
				return m_Panel2Collapsed;
			}
			set {				
				if (m_Panel2Collapsed != value) {
					m_Panel2Collapsed = value;
					Panel2.Visible = !value;
					//Splitter1.Visible = !value;
					Invalidate ();
				}
			}
		}

		private bool m_Panel3Collapsed;
		public bool Panel3Collapsed {
			get {
				return m_Panel3Collapsed;
			}
			set {				
				if (m_Panel3Collapsed != value) {
					m_Panel3Collapsed = value;
					Panel3.Visible = !value;
					//Splitter2.Visible = !value;
					Invalidate ();
				}
			}
		}


		public ScrollableContainer Panel1 { get; private set; }
		public SplitterBase Splitter1 { get; private set; }
		public ScrollableContainer Panel2 { get; private set; }
		public SplitterBase Splitter2 { get; private set; }
		public ScrollableContainer Panel3 { get; private set; }

		public SplitOrientation Orientation { get; private set; }

		public TripleSplitContainer (string name, SplitOrientation orientation, Docking dock, IWidgetStyle style)
			: base(name, dock, style)
		{
			Orientation = orientation;

			/***
			Panel1 = new ScrollableContainer ("panel1", Docking.Fill, new SplitContainerPanelStyle ());
			Panel2 = new ScrollableContainer ("panel2", Docking.Fill, new SplitContainerPanelStyle ());
			Panel3 = new ScrollableContainer ("panel3", Docking.Fill, new SplitContainerPanelStyle ());
			***/

			Panel1 = new ScrollableContainer ("panel1", Docking.Fill, new EmptyWidgetStyle ());
			Panel2 = new ScrollableContainer ("panel2", Docking.Fill, new EmptyWidgetStyle ());
			Panel3 = new ScrollableContainer ("panel3", Docking.Fill, new EmptyWidgetStyle ());


			if (orientation == SplitOrientation.Horizontal) {
				Splitter1 = new HorizontalSplitter ("splitter1", new SplitContainerTransparentSplitterStyle());
				Splitter2 = new HorizontalSplitter ("splitter2", new SplitContainerTransparentSplitterStyle());
			} else {
				Splitter1 = new VerticalSplitter ("splitter1", new SplitContainerTransparentSplitterStyle());
				Splitter2 = new VerticalSplitter ("splitter2", new SplitContainerTransparentSplitterStyle());
			}

			Children.Add (Panel1);
			Children.Add (Splitter1);
			Children.Add (Panel2);
			Children.Add (Splitter2);
			Children.Add (Panel3);

			//if (distance != 0)
			Splitter1.Distance = 0.334f;
			Splitter2.Distance = 0.334f;
		}

		public override void Focus ()
		{
			if (!Panel1Collapsed)
				Panel1.Focus ();
			else if (!Panel2Collapsed)
				Panel2.Focus ();
			else if (!Panel3Collapsed)
				Panel3.Focus ();
		}

		public override void OnLayout (IGUIContext ctx, RectangleF bounds)
		{			
			base.OnLayout (ctx, bounds);

			bool visible1 = Panel1.Visible && Panel2.Visible && !Panel1Collapsed && !Panel2Collapsed;
			bool visible2 = Panel2.Visible && Panel3.Visible && !Panel2Collapsed && !Panel3Collapsed;
			if (!visible1 && !visible2)
				visible1 = true;

			Splitter1.Visible = visible1;
			Splitter2.Visible = visible2;

			if (Splitter1.Visible)
				Splitter1.OnLayout (ctx, bounds);
			if (Splitter2.Visible)
				Splitter2.OnLayout (ctx, bounds);

			if (Orientation == SplitOrientation.Vertical) {
				if (Panel1.Visible) {
					if (Panel2.Visible)
						Panel1.OnLayout (ctx, new RectangleF (Left, Top, Splitter1.Left - Left, Height));
					else if (Panel3.Visible) {
						Panel1.OnLayout (ctx, new RectangleF (Left, Top, Splitter1.Left - Left, Height));
					} else {
						Panel1.OnLayout (ctx, new RectangleF (Left, Top, Width, Height));
					}
				}
				if (Panel2.Visible) {
					if (Panel1.Visible) {
						if (Panel3.Visible)
							Panel2.OnLayout (ctx, new RectangleF (Splitter1.Right, Top, Splitter2.Left - Splitter1.Right, Height));
						else
							Panel2.OnLayout (ctx, new RectangleF (Splitter1.Right, Top, Right - Splitter1.Right, Height));
					}
					else if (Panel3.Visible)
						Panel2.OnLayout (ctx, new RectangleF (Splitter1.Right, Top, Splitter2.Left - Left, Height));
					else
						Panel2.OnLayout (ctx, new RectangleF (Left, Top, Width, Height));				
				}
				if (Panel3.Visible) {
					if (Panel1.Visible)
						Panel3.OnLayout (ctx, new RectangleF (Left, Top, Right - Splitter1.Right, Height));
					else if (Panel2.Visible)
						Panel3.OnLayout (ctx, new RectangleF (Left, Top, Right - Splitter2.Right, Height));
					else
						Panel3.OnLayout (ctx, new RectangleF (Left, Top, Width, Height));
				}
			} else {	// *** Orientation = Horizontal
				if (Panel1.Visible) {
					if (Panel2.Visible)
						Panel1.OnLayout (ctx, new RectangleF (Left, Top, Width, Splitter1.Top - Top));
					else if (Panel3.Visible) {
						Panel1.OnLayout (ctx, new RectangleF (Left, Top, Width, Splitter1.Top - Top));
					} else {
						Panel1.OnLayout (ctx, new RectangleF (Left, Top, Width, Height));
					}
				}
				if (Panel2.Visible) {
					if (Panel1.Visible) {
						if (Panel3.Visible)
							Panel2.OnLayout (ctx, new RectangleF (Splitter1.Right, Top, Width, Splitter2.Top - Splitter1.Bottom));
						else
							Panel2.OnLayout (ctx, new RectangleF (Splitter1.Right, Top, Width, Bottom - Splitter1.Bottom));
					}
					else if (Panel3.Visible)
						Panel2.OnLayout (ctx, new RectangleF (Left, Top, Width, Splitter2.Top - Top));				
					else
						Panel2.OnLayout (ctx, new RectangleF (Left, Top, Width, Height));				
				}
				if (Panel3.Visible) {
					if (Panel2.Visible)
						Panel3.OnLayout (ctx, new RectangleF (Left, Splitter2.Bottom, Width, Bottom - Splitter2.Bottom));
					else if (Panel1.Visible)
						Panel3.OnLayout (ctx, new RectangleF (Left, Splitter1.Bottom, Width, Bottom - Splitter1.Bottom));
					else
						Panel3.OnLayout (ctx, new RectangleF (Left, Top, Width, Height));
				}
			}
			
		}		

		protected override void LayoutChildren (IGUIContext ctx, RectangleF bounds)
		{
			// we've already layouted the children
		}
	}
}

