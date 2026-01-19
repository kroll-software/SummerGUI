using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Input;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using KS.Foundation;

namespace SummerGUI
{	
	public class TabPage : ScrollableContainer, IComparable<TabPage>
	{
		public TabButton TabButton { get; private set; }

		public string Text
		{ 
			get {
				return TabButton.Text;
			}
			set {
				TabButton.Text = value;
			}
		}

		public override bool Selected {
			get {
				return TabButton.Selected;
			}
			set {
				TabButton.Selected = value;
			}
		}

		public TabPage(string name, string text, char icon = (char)0)
			: base(name)
		{
			TabButton = new TabButton (this, name, text, icon);
			TabButton.CanFocus = false;
		}

		public int CompareTo(TabPage other)
		{
			if (other == null)
				return 0;
			return this.TabIndex.CompareTo (other.TabIndex);
		}

		public override void Focus ()
		{
			if (m_CanFocus) {
				base.Focus ();
			} else {
				foreach (Widget child in Children) {
					if (child as SummerGUI.Scrolling.ScrollBar == null && child.CanFocus) {
						child.Focus ();
						break;
					}
				}
			}
		}

		protected override void CleanupManagedResources ()
		{
			if (TabButton != null)
				TabButton.Dispose ();
			base.CleanupManagedResources ();
		}			
	}

	public class TabButton : Button
	{		
		public TabPage TabPage { get; private set; }

		public TabButton(TabPage tabPage, string name, string text, char icon = (char)0)
			: base(name, text, icon, new TabButtonStyle())			
		{	
			TabPage = tabPage;
			Margin = Padding.Empty;
			Padding = new Padding (12, 8, 12, 7);
			MaxSize = SizeMax;

			this.Styles.SetStyle (new TabButtonHoverStyle (), WidgetStates.Hover);
			this.Styles.SetStyle (new TabButtonSelectedStyle (), WidgetStates.Selected);
			this.Styles.SetStyle (new TabButtonPressedStyle (), WidgetStates.Pressed);
			this.Styles.SetStyle (new TabButtonDisabledStyle (), WidgetStates.Disabled);
		}

		public override void OnClick (MouseButtonEventArgs e)
		{
			if (Parent != null && Parent.Parent != null) {
				TabContainer tc = Parent.Parent as TabContainer;
				if (tc != null) {
					tc.SelectedTab = this.TabPage;
				}
			}
			base.OnClick (e);
		}			
			
		protected override void CleanupManagedResources ()
		{
			TabPage = null;
			base.CleanupManagedResources();
		}
	}

	public class TabBar : Container
	{
		public TabBar() : base("tabbar", Docking.Top, new TabBarStyle())
		{
			//Padding = new Padding (0, 1, 0, 1);
			//Padding = Padding.Empty;
		}

		public override SizeF PreferredSize (IGUIContext ctx, SizeF proposedSize)
		{
			if (CachedPreferredSize == SizeF.Empty) {
				if (Children.Count == 0)
					CachedPreferredSize = base.PreferredSize (ctx, proposedSize);
				else
					CachedPreferredSize = new SizeF (proposedSize.Width, Children [0].PreferredSize (ctx, proposedSize).Height + Padding.Height);
			}
			return CachedPreferredSize;
		}
			
		public float ScrollOffsetX { get; private set; }
		public float ContentWidth { get; private set; }

		bool EnsureVisibleFlag;

		public override void OnLayout (IGUIContext ctx, RectangleF bounds)
		{
			//if (!Dirty)
			//	return;

			if (IsLayoutSuspended || !Visible)
				return;

			base.OnLayout (ctx, bounds);
			ContentWidth = Children.OfType<TabButton>().Where(c => c.Visible && !c.IsOverlay)
				.Sum (c => c.PreferredSize(ctx).Width);

			if (EnsureVisibleFlag) {
				EnsureVisibleFlag = false;
				if (Parent as TabContainer != null) {					
					EnsureIndexVisible ((Parent as TabContainer).SelectedTabIndex);
				}
			}
		}

		public bool IsOverflow
		{
			get{
				return Parent != null && ContentWidth > Parent.Width;
			}
		}			

		public override void OnResize ()
		{			
			base.OnResize ();
			if (!IsOverflow || Children.Count == 0)
				ScrollOffsetX = 0;
			else {
				Widget lastchild = Children.OfType<TabButton> ().LastOrDefault (c => c.Visible && !c.IsOverlay);
				if (lastchild != null) {
					if (lastchild.Right < Bounds.Right) {
						ScrollOffsetX += (Bounds.Right - lastchild.Right);				
					}
				} 

				if (ScrollOffsetX > 0) {
					ScrollOffsetX = 0;		
				}
					
				EnsureVisibleFlag = true;
			}				
		}

		private float WidthAtIndex(int index)
		{
			try {
				return Children.Take (index).OfType<TabButton> ().Where (c => c.Visible && !c.IsOverlay)
					.Sum (c => c.Width);	
			} catch (Exception ex) {
				ex.LogError ();
				return 0;
			}
		}

		public void EnsureIndexVisible(int index)
		{
			if (Parent != null && IsOverflow && index >= 0 && index < Children.Count) {
				Widget child = Children [index];
				if (child != null) {					
					if (child.Left < Left) {
						ScrollOffsetX = -WidthAtIndex (index);
						//Invalidate ();
					}
					else if (child.Width < Width && child.Right > Right) {
						ScrollOffsetX = -((WidthAtIndex(index) + child.Width) - Width);
						//Invalidate ();
					}
				}
			}
		}

		public bool CanScrollLeft
		{
			get{				
				return ScrollOffsetX > Width - ContentWidth;
			}
		}

		public bool CanScrollRight
		{
			get{				
				return ScrollOffsetX < 0;
			}
		}			
			
		public void ScrollLeft ()
		{
			Scroll (+8 * ScaleFactor);
		}

		public void ScrollRight ()
		{
			Scroll (-8 * ScaleFactor);
		}

		public void ScrollPrevious ()
		{
			// TabPage am linen Rand
			int idx = -1;
			for (int i = 0; i < Children.Count; i++) {
				Widget child = Children [i];
				if (child != null && child.Visible) {					
					if (child.Left < Left) {
						idx = i;
					} else {						
						if (idx >= 0) {
							ScrollOffsetX = -WidthAtIndex (idx);
							Invalidate ();
						}
						return;
					}
				}
			}
			ScrollOffsetX = 0;
			Invalidate ();
		}

		public void ScrollNext ()
		{
			// TabPage am linen Rand
			for (int i = 0; i < Children.Count; i++) {
				Widget child = Children [i];
				if (child != null && child.Visible) {					
					if (child.Left > Left) {
						ScrollOffsetX = Math.Max(-WidthAtIndex (i), Width - ContentWidth);
						Invalidate ();
						return;
					}
				}
			}
		}
			
		public void Scroll(float deltaX)
		{
			ScrollOffsetX += deltaX;
			ScrollOffsetX = Math.Min (0, ScrollOffsetX);
			Invalidate ();
		}

		public void ResetScroll ()
		{
			ScrollOffsetX = 0;
		}

		protected override void LayoutChildren (IGUIContext ctx, RectangleF bounds)
		{
			//base.LayoutChildren (ctx, bounds);
			float x = Left;
			Children.OfType<TabButton>().Where(c => c.Visible && !c.IsOverlay)
				.ForEach (c => {
					c.SetBounds (new RectangleF(x + ScrollOffsetX, bounds.Top, c.PreferredSize(ctx).Width, bounds.Height));
					x += c.Width;
				});
		}

		/***
		public override bool OnKeyDown (KeyboardKeyEventArgs e)
		{
			if (base.OnKeyDown (e))
				return true;

			switch (e.Key) {
			case Key.Left:
				if (CanScrollLeft) {
					ScrollLeft ();
					return true;
				}
				break;
			case Key.Right:
				if (CanScrollRight) {
					ScrollRight ();
					return true;
				}
				break;
			}

			return false;
		}
		***/
	}		

	public class TabPageCollection : BinarySortedList<TabPage>
	{		
		public TabPage this [string name] 
		{
			get {
				// we have robust threadsafe iterators with the collection !
				// still catch errors to be sure
				try {
					return this.FirstOrDefault (tp => tp.Name == name);	
				} catch (Exception ex) {
					ex.LogError ();
					return null;
				}
			}
		}
	}

	public class TabContainer : Container
	{
		public TabBar TabBar { get; private set; }
		public TabPageCollection TabPages  { get; private set; }	
		public int SelectedTabIndex { get; set; }

		public Button ScrollLeftButton { get; private set; }
		public Button ScrollRightButton { get; private set; }

		public TabContainer (string name)
			: base(name, Docking.Fill, new TabContainerStyle())
		{			
			ScrollLeftButton = new Button ("scrollleft", null, ((char)FontAwesomeIcons.fa_long_arrow_left), ColorContexts.Default);
			ScrollLeftButton.Dock = Docking.Left;
			ScrollLeftButton.CanFocus = false;
			ScrollLeftButton.CanSelect = false;
			ScrollLeftButton.IsAutofire = true;
			ScrollRightButton = new Button ("scrollright", null, ((char)FontAwesomeIcons.fa_long_arrow_right), ColorContexts.Default);
			ScrollRightButton.Dock = Docking.Right;
			ScrollRightButton.IsAutofire = true;
			ScrollRightButton.CanFocus = false;
			ScrollRightButton.CanSelect = false;

			Children.Add (ScrollLeftButton);
			Children.Add (ScrollRightButton);

			TabBar = new TabBar ();
			Children.Add (TabBar);
			TabPages = new TabPageCollection ();

			ScrollLeftButton.Fire += ScrollLeftButton_Fire;
			ScrollRightButton.Fire += ScrollRightButton_Fire;
		}			

		void ScrollLeftButton_Fire (object sender, EventArgs e)
		{	
			//TabBar.ScrollLeft ();
			TabBar.ScrollPrevious ();
		}

		void ScrollRightButton_Fire (object sender, EventArgs e)
		{	
			TabBar.ScrollNext ();
			//TabBar.ScrollRight ();
		}			
			
		public TabPage SelectedTab
		{
			get{
				if (SelectedTabIndex < 0 || SelectedTabIndex >= TabPages.Count)
					return null;

				return TabPages[SelectedTabIndex];
			}
			set{
				if (value != null && value != SelectedTab && value.IsVisibleEnabled) {
					TabPages.ForEach (t => t.TabButton.Selected = false);
					value.TabButton.Selected = true;
					SelectedTabIndex = TabPages.IndexOf (value);
					value.Focus ();
					TabBar.EnsureIndexVisible (SelectedTabIndex);
					ResetCachedLayout ();
					value.Update();
				}
			}
		}

		public void SelectNextTabPage()
		{
			int newIndex = SelectedTabIndex + 1;
			while (newIndex < TabPages.Count && (!TabPages [newIndex].Visible || TabPages [newIndex].IsOverlay))
				newIndex++;
			if (newIndex < TabPages.Count) {
				SelectedTab = TabPages [newIndex];			
			}
		}

		public void SelectPrevTabPage()
		{
			int newIndex = SelectedTabIndex - 1;
			while (newIndex >= 0 && (!TabPages [newIndex].Visible || TabPages [newIndex].IsOverlay))
				newIndex--;
			if (newIndex >= 0) {
				SelectedTab = TabPages [newIndex];
			}
		}

		public void AdTabPage(string name, string caption, Color backColr = default(Color) , char icon = (char)0, int index = -1)
		{			
			TabPage tp = new TabPage (name, caption, icon);
			tp.Parent = this;
			tp.BackColor = backColr;
			if (index >= 0 && index < TabPages.Count) {
				tp.TabIndex = TabPages [index].TabIndex;
				for (int i = index + 1; i < TabPages.Count; i++)
					TabPages [i].TabIndex++;
				TabPages.Insert (index, tp);
			} else {
				if (TabPages.Count == 0)
					tp.TabIndex = 0;
				else
					tp.TabIndex = TabPages.Last.TabIndex + 1;
				TabPages.AddLast (tp);
			}
			TabBar.AddChild (tp.TabButton);
			tp.Selected |= TabPages.Count == 1;
			ResetCachedLayout ();
		}

		protected override void LayoutChildren (IGUIContext ctx, RectangleF bounds)
		{		
			bool overFlow = TabBar.IsOverflow;
			ScrollLeftButton.Visible = overFlow;
			ScrollRightButton.Visible = overFlow;

			if (overFlow) {
				ScrollLeftButton.Enabled = TabBar.CanScrollRight;
				ScrollRightButton.Enabled = TabBar.CanScrollLeft;
				SizeF sz = TabBar.PreferredSize (ctx, bounds.Size);
				RectangleF rb = new RectangleF (bounds.Left - 1, bounds.Top, bounds.Width + 2, sz.Height);
				LayoutChild (ctx, ScrollLeftButton, rb);
				LayoutChild (ctx, ScrollRightButton, rb);
				RectangleF centerBounds = rb;
				centerBounds.Inflate (-ScrollLeftButton.Width, 0);
				LayoutChild (ctx, TabBar, centerBounds);
			} else {				
				LayoutChild (ctx, TabBar, bounds);
			}				

			TabPage tp = SelectedTab;
			if (tp != null) {
				LayoutChild(ctx, tp, new RectangleF (bounds.Left, bounds.Top + TabBar.Height + TabBar.Margin.Bottom, 
					bounds.Width, bounds.Height - TabBar.Height - TabBar.Margin.Bottom));
			}
		}			
			
		public override void Update (IGUIContext ctx)
		{			
			base.Update (ctx);
            SelectedTab?.Update(ctx);
		}

		public override void OnLayout (IGUIContext ctx, RectangleF bounds)
		{
			base.OnLayout (ctx, bounds);
		}

		public override Widget HitTest (float x, float y)
		{
			Widget wt = base.HitTest (x, y);
			if (wt == null || wt == this) {
				TabPage tp = SelectedTab;
				if (tp != null) {
					wt = tp.HitTest (x, y);
				}
			}
			return wt;
		}

		public override bool OnKeyDown (KeyboardKeyEventArgs e)
		{
			if (base.OnKeyDown (e))
				return true;

			if (IsVisibleEnabled && Children.Any(c => c.IsFocused)) {
				switch (e.Key) {
				case Keys.Right:
					//this.SelectNextChild (SelectedTab.TabButton);
					Root.SelectNextWidget ();
					return true;
				case Keys.Left:
					//this.SelectPrevChild (SelectedTab.TabButton);
					Root.SelectPrevWidget ();
					return true;
				}
			}

			return (SelectedTab != null && SelectedTab.OnKeyDown (e));
		}			

		public override void OnPaint (IGUIContext ctx, RectangleF bounds)
		{
			base.OnPaint (ctx, bounds);
		}

		public override bool OnMouseWheel (MouseWheelEventArgs e)
		{
			if (base.OnMouseWheel (e))
				return true;
			return (SelectedTab != null && SelectedTab.OnMouseWheel (e));
		}

		protected override void CleanupManagedResources ()
		{
			TabPages.DisposeListObjects ();
			TabPages.Clear ();
			TabBar = null;

			base.CleanupManagedResources ();
		}
	}
}

