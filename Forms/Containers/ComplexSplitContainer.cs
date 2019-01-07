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
	public class ComplexSplitContainer : Container
	{
		private bool m_PanelLeftCollapsed;
		public bool PanelLeftCollapsed {
			get {
				return m_PanelLeftCollapsed;
			}
			set {				
				if (m_PanelLeftCollapsed != value) {
					m_PanelLeftCollapsed = value;
					PanelLeft.Visible = !value;
					Invalidate ();
				}
			}
		}

		private bool m_PanelCenterCollapsed;
		public bool PanelCenterCollapsed {
			get {
				return m_PanelCenterCollapsed;
			}
			set {				
				if (m_PanelCenterCollapsed != value) {
					m_PanelCenterCollapsed = value;
					PanelCenter.Visible = !value;
					Invalidate ();
				}
			}
		}

		private bool m_PanelBottomCollapsed;
		public bool PanelBottomCollapsed {
			get {
				return m_PanelBottomCollapsed;
			}
			set {				
				if (m_PanelBottomCollapsed != value) {
					m_PanelBottomCollapsed = value;
					PanelBottom.Visible = !value;
					Invalidate ();
				}
			}
		}

		private bool m_PanelRightCollapsed;
		public bool PanelRightCollapsed {
			get {
				return m_PanelRightCollapsed;
			}
			set {				
				if (m_PanelRightCollapsed != value) {
					m_PanelRightCollapsed = value;
					PanelRight.Visible = !value;
					Invalidate ();
				}
			}
		}

		public SplitterBase SplitterLeft { get; private set; }
		public SplitterBase SplitterBottom { get; private set; }
		public SplitterBase SplitterRight { get; private set; }

		public ScrollableContainer PanelLeft { get; private set; }
		public ScrollableContainer PanelCenter { get; private set; }
		public ScrollableContainer PanelBottom { get; private set; }
		public ScrollableContainer PanelRight { get; private set; }

		public ComplexSplitContainer (string name, Docking dock, IWidgetStyle style)
			: base(name, dock, style)
		{
			/***
			PanelLeft = new ScrollableContainer ("panelleft", Docking.Fill, new SplitContainerPanelStyle ());
			PanelCenter = new ScrollableContainer ("panelcenter", Docking.Fill, new SplitContainerPanelStyle ());
			PanelBottom = new ScrollableContainer ("panelbottom", Docking.Fill, new SplitContainerPanelStyle ());
			PanelRight = new ScrollableContainer ("panelright", Docking.Fill, new SplitContainerPanelStyle ());
			***/

			PanelLeft = new ScrollableContainer ("panelleft", Docking.Fill, new SplitContainerPanelStyle ());
			PanelCenter = new ScrollableContainer ("panelcenter", Docking.Fill, new EmptyWidgetStyle ());
			PanelBottom = new ScrollableContainer ("panelbottom", Docking.Fill, new EmptyWidgetStyle ());
			PanelRight = new ScrollableContainer ("panelright", Docking.Fill, new EmptyWidgetStyle ());

			SplitterLeft = new VerticalSplitter ("leftsplitter", new SplitContainerTransparentSplitterStyle());
			SplitterBottom = new HorizontalSplitter ("bottomsplitter", new SplitContainerTransparentSplitterStyle());
			SplitterRight = new VerticalSplitter ("rightsplitter", new SplitContainerTransparentSplitterStyle());

			PanelCenter.Dock = Docking.Fill;

			Children.Add (SplitterLeft);
			Children.Add (SplitterBottom);
			Children.Add (SplitterRight);

			Children.Add (PanelLeft);
			Children.Add (PanelCenter);
			Children.Add (PanelBottom);
			Children.Add (PanelRight);

			SplitterLeft.Distance = 0.25f;
			SplitterBottom.Distance = -0.25f;
			SplitterRight.Distance = -0.25f;
		}

		public override void Focus ()
		{
			if (PanelCenter.CanFocus)
				PanelCenter.Focus ();
			else if (PanelLeft.CanFocus)
				PanelLeft.Focus ();			
			else if (!PanelRight.CanFocus)
				PanelRight.Focus ();
			else
				PanelBottom.Focus ();
		}

		public override void OnLayout (IGUIContext ctx, RectangleF bounds)
		{
			base.OnLayout (ctx, bounds);

			SplitterLeft.Visible = PanelLeft.Visible && (PanelCenter.Visible || PanelBottom.Visible);
			SplitterRight.Visible = PanelRight.Visible && PanelCenter.Visible;
			SplitterBottom.Visible = PanelBottom.Visible && (PanelCenter.Visible || PanelRight.Visible);

			if (SplitterLeft.Visible)
				SplitterLeft.OnLayout (ctx, bounds);
			if (SplitterRight.Visible)
				SplitterRight.OnLayout (ctx, bounds);
			if (SplitterBottom.Visible)
				SplitterBottom.OnLayout (ctx, bounds);


			//if (!PanelLeftCollapsed)
			//	SplitterLeft.OnLayout (ctx, this.Bounds);
			/******/
			if (!PanelBottomCollapsed && !PanelCenterCollapsed) {
				float left = bounds.Left;
				if (!PanelLeftCollapsed && SplitterLeft.Visible)
					left = SplitterLeft.Right;
				SplitterBottom.OnLayout (ctx, new RectangleF (left, Top, Width - left, Height - SplitterBottom.Bottom));
			}
			if (!PanelRightCollapsed) {
				float bottom = bounds.Bottom;
				if (!PanelBottomCollapsed && SplitterBottom.Visible)
					bottom = SplitterBottom.Top;
				SplitterRight.OnLayout (ctx, new RectangleF (SplitterRight.Left, Top, SplitterRight.Width, Height - bottom));
			}



			// dump splitters
			/***
			this.LogDebug("SplitterLeft.Left: {0}", SplitterLeft.Left);
			this.LogDebug("SplitterBottom.Top: {0}", SplitterBottom.Top);
			this.LogDebug("SplitterRight.Right: {0}", SplitterRight.Right);
			***/

			// it's nice to handle such things quite optimized..

			if (PanelLeft.Visible)
				PanelLeft.OnLayout (ctx, new RectangleF (Left, Top, SplitterLeft.Left - Left, Height));
			if (PanelCenter.Visible) {
				float left = bounds.Left;
				float right = bounds.Right;
				float bottom = bounds.Bottom;
				if (!PanelLeftCollapsed && SplitterLeft.Visible)
					left = SplitterLeft.Right;
				if (!PanelRightCollapsed && SplitterRight.Visible)
					right = SplitterRight.Left;
				if (!PanelBottomCollapsed && SplitterBottom.Visible)
					bottom = SplitterBottom.Top;				
				PanelCenter.OnLayout (ctx, new RectangleF (left, Top, right - left, bottom - Top));
			}
			if (PanelBottom.Visible) {
				float left = bounds.Left;
				if (!PanelLeftCollapsed && SplitterLeft.Visible)
					left = SplitterLeft.Right;
				PanelBottom.OnLayout (ctx, new RectangleF (left, SplitterBottom.Bottom, Right - left, Bottom - SplitterBottom.Bottom));
			}
			if (PanelRight.Visible) {
				float bottom = bounds.Bottom;
				if (!PanelBottomCollapsed && SplitterBottom.Visible)
					bottom = SplitterBottom.Top;
				PanelRight.OnLayout (ctx, new RectangleF (SplitterRight.Right, Top, Right - SplitterRight.Right, Height - bottom));
			}
		}			

		protected override void LayoutChildren (IGUIContext ctx, RectangleF bounds)
		{			
		}
	}
}

