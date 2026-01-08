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
	// auch das ist alles blödsinn, sobald das layout etwas komplexer wird..
	public enum SplitOrientation
	{
		Vertical,
		Horizontal
	}

	[Flags]
	public enum SplitterFixedPanel
	{
		None,
		All,
		Panel1,
		Panel2,
		Panel3,
		Panel4,
	}

	/*** nächstes mal ..
	public class SplitContainerPanel : ScrollableContainer, ISplitContainerPanel
	{
		public bool IsCollapsed { get; internal set; }
		public bool IsFixed { get; internal set; }
		public bool IsVisibleExpanded
		{
			get{
				return Visible && !IsCollapsed;
			}
		}
		public bool Collapse()
		{
			if (IsCollapsed)
				return false;
			ISplitContainer P = Parent as ISplitContainer;
			if (P == null || P.Collapse (this))
				return true;
			return false;
		}
		public bool Expand()
		{
			if (!IsCollapsed)
				return false;
			ISplitContainer P = Parent as ISplitContainer;
			if (P == null || P.Expand (this))
				return true;
			return false;
		}

		public SplitContainerPanel (string name) : base (name) {}
		public SplitContainerPanel (string name, Docking dock) : base (name, dock, null) {}
		public SplitContainerPanel (string name, Docking dock, IWidgetStyle style) : base (name, dock, style) {}
	}

	public interface ISplitContainerPanel
	{
		bool IsCollapsed { get; }
		bool IsFixed { get; }
		bool IsVisibleExpanded { get; }
		bool Collapse ();
		bool Expand ();
	}

	public interface ISplitContainer
	{
		bool Collapse (ISplitContainerPanel panel);
		bool Expand (ISplitContainerPanel panel);
	}
	***/

	/// <summary>
	/// Split container.
	/// </summary>
	public class SplitContainer : Container	//, ISplitContainer
	{
		public SplitOrientation Orientation 
		{ 
			get {
				return Splitter.Orientation;
			}
		}

		public SplitterFixedPanel FixedPanel { get; set; }

		private bool m_Panel1Collapsed;
		public bool Panel1Collapsed {
			get {
				return m_Panel1Collapsed;
			}
			set {				
				if (m_Panel1Collapsed != value) {
					m_Panel1Collapsed = value;
					Panel1.Visible = !value;
					//Splitter.Visible = !value;	// falsch
					//Invalidate ();
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
					//Splitter.Visible = !value;
					//Invalidate ();
				}
			}
		}

		public ScrollableContainer Panel1 { get; private set; }
		public ScrollableContainer Panel2 { get; private set; }
		public SplitterBase Splitter { get; private set; }

		public SplitContainer (string name, SplitOrientation orientation, float distance)
			: base(name, Docking.Fill, new SplitContainerStyle())
		{	
			/***
			Panel1 = new ScrollableContainer ("panel1", Docking.Fill, new SplitContainerPanelStyle ());
			Panel2 = new ScrollableContainer ("panel2", Docking.Fill, new SplitContainerPanelStyle ());
			***/

			Panel1 = new ScrollableContainer ("panel1", Docking.Fill, new EmptyWidgetStyle ());
			Panel2 = new ScrollableContainer ("panel2", Docking.Fill, new EmptyWidgetStyle ());

			if (orientation == SplitOrientation.Horizontal) {
				Splitter = new HorizontalSplitter ("splitter", new SplitContainerTransparentSplitterStyle ());
			} else {
				Splitter = new VerticalSplitter ("splitter", new SplitContainerTransparentSplitterStyle ());
			}

			//Panel1.MinSize = new SizeF (Splitter.MinDistanceNear, Splitter.MinDistanceNear);
			//Panel2.MinSize = new SizeF (Splitter.MinDistanceFar, Splitter.MinDistanceFar);

			Children.Add (Panel1);
			Children.Add (Splitter);
			Children.Add (Panel2);

			//this.BackColor = Color.Yellow;
			//Panel2.BackColor = Color.Red;
			//Splitter.BackColor = Color.FromArgb(128, Color.LimeGreen);
			//Splitter.SplitterWidth = 12;

			if (distance != 0)
				Splitter.Distance = distance;
		}

		public override void Focus ()
		{
			if (!Panel1Collapsed)
				Panel1.Focus ();
			else
				Panel2.Focus ();
		}			

		public override void OnLayout (IGUIContext ctx, RectangleF bounds)
		{			
			base.OnLayout (ctx, bounds);

			Splitter.Visible = !Panel1Collapsed && !Panel2Collapsed;
			if (Splitter.Visible)
				Splitter.OnLayout (ctx, bounds);

			switch (Orientation) {
			case SplitOrientation.Vertical:
				if (Panel1Collapsed)
					Panel2.OnLayout (ctx, bounds);
				else if (Panel2Collapsed)
					Panel1.OnLayout (ctx, bounds);
				else {
					//Panel1.OnLayout (ctx, new RectangleF (bounds.Left, bounds.Top, Splitter.Left - bounds.Left, bounds.Height));
					//Panel2.OnLayout (ctx, new RectangleF (Splitter.Right, bounds.Top, bounds.Right - Splitter.Right, bounds.Height));

					Panel1.OnLayout (ctx, new RectangleF (bounds.Left, bounds.Top, Splitter.Left - bounds.Left, bounds.Height));
					Panel2.OnLayout (ctx, new RectangleF (Splitter.Right, bounds.Top, bounds.Right - Splitter.Right - Splitter.Width, bounds.Height));
				}
				break;

			default:
				if (Panel1Collapsed)
					Panel2.OnLayout (ctx, bounds);
				else if (Panel2Collapsed)
					Panel1.OnLayout (ctx, bounds);
				else {
					Panel1.OnLayout (ctx, new RectangleF (bounds.Left, bounds.Top, bounds.Width, Splitter.Top - bounds.Top));
					Panel2.OnLayout (ctx, new RectangleF (bounds.Left, Splitter.Bottom, bounds.Width, bounds.Bottom - Splitter.Bottom));
				}
				break;
			}				
		}

		protected override void LayoutChildren (IGUIContext ctx, RectangleF bounds)
		{
			// we've already layouted the children
		}
	}		
}

