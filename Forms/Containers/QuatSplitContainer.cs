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
	// not in use, not ready, not tested

	public class QuatSplitter : Container
	{
		// to be tested if it makes any sense

		private bool m_Panel1Collapsed;
		public bool Panel1Collapsed {
			get {
				return m_Panel1Collapsed;
			}
			set {				
				if (m_Panel1Collapsed != value) {
					m_Panel1Collapsed = value;
					Panel1.Visible = !value;
					Splitter1.Visible = !value;
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
					Splitter1.Visible = !value;
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
					Splitter2.Visible = !value;
					Invalidate ();
				}
			}
		}

		private bool m_Panel4Collapsed;
		public bool Panel4Collapsed {
			get {
				return m_Panel4Collapsed;
			}
			set {				
				if (m_Panel4Collapsed != value) {
					m_Panel4Collapsed = value;
					Panel4.Visible = !value;
					Splitter2.Visible = !value;
					Invalidate ();
				}
			}
		}

		public ScrollableContainer Panel1 { get; private set; }
		public ScrollableContainer Panel2 { get; private set; }
		public SplitterBase Splitter1 { get; private set; }
		public SplitterBase Splitter2 { get; private set; }
		public ScrollableContainer Panel3 { get; private set; }
		public ScrollableContainer Panel4 { get; private set; }

		public QuatSplitter (string name, Docking dock, IWidgetStyle style)
			: base(name, dock, style)
		{
			Panel1 = new ScrollableContainer ("panel1", Docking.Fill, new SplitContainerPanelStyle ());
			Panel2 = new ScrollableContainer ("panel2", Docking.Fill, new SplitContainerPanelStyle ());
			Panel3 = new ScrollableContainer ("panel3", Docking.Fill, new SplitContainerPanelStyle ());
			Panel4 = new ScrollableContainer ("panel4", Docking.Fill, new SplitContainerPanelStyle ());

			Splitter1 = new HorizontalSplitter ();
			Splitter2 = new VerticalSplitter ();

			Children.Add (Splitter1);
			Children.Add (Splitter2);
			Children.Add (Panel1);
			Children.Add (Panel2);
			Children.Add (Panel3);
			Children.Add (Panel4);
		}

		public override void Focus ()
		{
			if (!Panel1Collapsed)
				Panel1.Focus ();
			else if (!Panel2Collapsed)
				Panel2.Focus ();
			else if (!Panel3Collapsed)
				Panel3.Focus ();
			else
				Panel4.Focus ();
		}

		public override void OnLayout (IGUIContext ctx, RectangleF bounds)
		{
			base.OnLayout (ctx, bounds);

			if (!Panel1Collapsed && !Panel2Collapsed)
				Splitter1.OnLayout (ctx, this.Bounds);
			if (!Panel3Collapsed && !Panel2Collapsed)
				Splitter2.OnLayout (ctx, this.Bounds);


			/***
			if (Panel1Collapsed && Panel2Collapsed && !Panel3Collapsed)
				Panel3.OnLayout (ctx, bounds);

			if (Panel1Collapsed && !Panel2Collapsed && Panel3Collapsed)
				Panel2.OnLayout (ctx, bounds);

			if (!Panel1Collapsed && Panel2Collapsed && Panel3Collapsed)
				Panel1.OnLayout (ctx, bounds);

			if (!Panel1Collapsed && !Panel3Collapsed)
				Panel2.OnLayout (ctx, bounds);
			***/


			if (Panel1.IsVisibleEnabled)
				Panel1.OnLayout (ctx, new RectangleF (Left, Top, Splitter1.Left - Left, Height));
			if (Panel2.IsVisibleEnabled)
				Panel2.OnLayout (ctx, new RectangleF (Splitter1.Right, Top, Splitter2.Left - Splitter1.Right, Height));
			if (Panel3.IsVisibleEnabled)
				Panel3.OnLayout (ctx, new RectangleF (Splitter2.Right, Top, Right - Splitter2.Right, Height));
		}

		protected override void LayoutChildren (IGUIContext ctx, RectangleF bounds)
		{
			// we've already layouted the children
		}
	}
}

