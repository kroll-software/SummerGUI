using System;
using System.Linq;
using KS.Foundation;
using System.Drawing;

namespace SummerGUI
{
	public class SidemenuToolbar : ComponentToolBar
	{
		public ToolBarButton CmdCollapseAll { get; private set; }
		public ToolBarButton CmdExpandAll { get; private set; }

		//public ToolBarSeparator Separator1 { get; private set; }
		//public ToolBarSeparator Separator2 { get; private set; }

		public ToolBarButton CmdScrollUp { get; private set; }
		public ToolBarButton CmdScrollDown { get; private set; }

		public ToolBarButton CmdPin { get; private set; }
		public ToolBarButton CmdHamburgerClose { get; private set; }

		SideMenuBar SideMenu;

		public SidemenuToolbar(string name, SideMenuBar menu)
			: base(name, null)
		{
			SideMenu = menu;
			Menu = menu.Menu;

			CmdCollapseAll = AddChild(new ToolBarButton("collapseall", "Collapse All", (char)FontAwesomeIcons.fa_minus_square));
			CmdExpandAll = AddChild(new ToolBarButton("expandall", "Expand All", (char)FontAwesomeIcons.fa_plus_square_o));

			CmdScrollUp = AddChild(new ToolBarButton("scrollup", "Scroll Up", (char)FontAwesomeIcons.fa_chevron_up));
			CmdScrollDown = AddChild(new ToolBarButton("scrolldown", "Scroll Down", (char)FontAwesomeIcons.fa_chevron_down));

			CmdScrollUp.IsAutofire = true;
			CmdScrollDown.IsAutofire = true;


			CmdPin = AddChild(new ToolBarButton ("pinned", "Pin the sidebar", (char)FontAwesomeIcons.fa_thumb_tack));
			CmdPin.Dock = Docking.Right;
			CmdPin.IsToggleButton = true;
			CmdPin.Click += (sender, e) => SideMenu.AutoClose = !CmdPin.Checked;

			CmdHamburgerClose = AddChild(new ToolBarButton("close", "Minimize to Mobile-Menu", (char)FontAwesomeIcons.fa_bars));
			CmdHamburgerClose.Dock = Docking.Right;
			CmdHamburgerClose.Margin = new Padding (2, 0, 1, 0);
			this.Padding = new Padding (5, 1, 1, 1);

			Children.OfType<ToolBarButton> ().ForEach (btn => btn.DisplayStyle = ButtonDisplayStyles.Image);
			CmdHamburgerClose.Click += (sender, e) => SideMenu.OnClose ();
		}

		protected override void OnParentChanged ()
		{
			base.OnParentChanged ();
			if (Parent != null){
				var context = SummerGUIWindow.CurrentContext;
				//var context = Parent.ParentWindow as IGUIContext;
				float ContentWidth = Children.Sum (b => b.PreferredSize(context).Width + (2 * ScaleFactor));
				if (ContentWidth > 100)
					Parent.MinSize = new SizeF (ContentWidth, 0);
			}
		}

		protected override void CleanupManagedResources ()
		{
			Menu = null;
			SideMenu = null;
			base.CleanupManagedResources ();
		}
	}

	public class SideMenuContainer : Container
	{
		public SidemenuToolbar ToolBar { get; private set; }
		public SideMenuBar MenuBar { get; private set; } 
		public IGuiMenu Menu { get; private set; }

		public SideMenuContainer (string name, IGuiMenu menu, IWidgetStyle style = null)
			: base (name, Docking.Fill, style ?? new EmptyGradientWidgetStyle())
		{			
			IsMenu = true;
			Menu = menu;
			Style.BackColorBrush = new LinearGradientBrush (Theme.Colors.Base00, Theme.Colors.Base0, GradientDirections.BackwardDiagonal);
			//Style.BackColorBrush = new LinearGradientBrush (Theme.Colors.Base0, Theme.Colors.Base00);
		}

		protected override void OnParentChanged ()
		{
			base.OnParentChanged ();
			if (Parent != null) {
				MenuBar = AddChild (new SideMenuBar ("sidemenubar", Menu));
				ToolBar = AddChild(new SidemenuToolbar ("toolbar", MenuBar));

				ScrollStep = MenuBar.LineHeight;
				ScrollOffsetY = 0;

				ToolBar.CmdCollapseAll.Click += (sender, e) => {
					Menu.CollapseAll();
					MenuBar.LastExpandedItems.Clear();
					MenuBar.LastExpandedItem = null;
					ScrollTop();
					OnCollapseExpand ();
					MenuBar.Invalidate();
				};
				ToolBar.CmdExpandAll.Click += (sender, e) => {
					Menu.ExpandAll ();
					MenuBar.LastExpandedItems.Clear();
					Menu.Items().Where(c => c.HasChildren).ForEach(c => 
						MenuBar.LastExpandedItems.Add(c));

					OnCollapseExpand ();
					MenuBar.Invalidate();
				};

				ToolBar.CmdScrollUp.Fire += (sender, e) => {
					ScrollUp();
					MenuBar.Invalidate();
				};

				ToolBar.CmdScrollDown.Fire += (sender, e) => {
					ScrollDown();
					MenuBar.Invalidate();
				};					
			}
		}

		public float ScrollOffsetY { get; set; }
		public float ScrollStep { get; set; }

		public void ScrollTop()
		{
			ScrollOffsetY = 0;
		}

		public void ScrollUp()
		{			
			ScrollOffsetY = Math.Min(0, ScrollOffsetY + ScrollStep);
		}

		public void ScrollDown()
		{
			ScrollOffsetY = Math.Max(Height - MenuBar.Height - ToolBar.Height, ScrollOffsetY - ScrollStep);
		}

		public void UpdateScroll()
		{			
			ScrollOffsetY = Math.Min(0, Math.Max(Height - MenuBar.Height - ToolBar.Height, ScrollOffsetY));
		}

		protected override void LayoutChildren (IGUIContext ctx, RectangleF bounds)
		{
			base.LayoutChildren (ctx, bounds);
			ToolBar.CmdScrollUp.Enabled = ScrollOffsetY < 0;
			ToolBar.CmdScrollDown.Enabled = MenuBar.Height + ScrollOffsetY > Height - ToolBar.Height;
		}
			
		protected override void LayoutChild (IGUIContext ctx, Widget child, RectangleF bounds)
		{			
			UpdateScroll ();
			if (child is SideMenuBar)
				bounds.Offset (0, ScrollOffsetY);
			base.LayoutChild (ctx, child, bounds);
		}

		public void Init()
		{
			ToolBar.CmdPin.Checked = !MenuBar.AutoClose;

			// Expand first level
			if (Menu.Count > 0) {
				Menu.CollapseAll ();
				Menu.Children.Where (c => c.CanExpand).ForEach (child => {					
					child.Expand ();
					MenuBar.LastExpandedItems.Add (child);
				});
			}

			System.Threading.Tasks.Task.Delay(250).ContinueWith((t) => MenuBar.TrimToScreen ());
		}

		public void OnCollapseExpand()
		{
			Menu.Items ().Where(c => c.Parent == null || !c.Parent.Collapsed).ToList ().Do (lst => {
				ToolBar.CmdCollapseAll.Enabled = lst.Any (m => m.CanCollapse);
				ToolBar.CmdExpandAll.Enabled = lst.Any (m => m.CanExpand);
			});

			MenuBar.MenuDirtyFlag = true;
		}

		public override bool OnMouseWheel (OpenTK.Input.MouseWheelEventArgs e)
		{			
			if (e.Delta != 0 && MenuBar.Height > Height - ToolBar.Height) {
				if (e.Delta < 0)
					ScrollDown ();
				else if (e.Delta > 0)
					ScrollUp ();
				Invalidate ();
			}
			return true;
		}

		protected override void CleanupManagedResources ()
		{
			Menu = null;
			base.CleanupManagedResources ();
		}
	}
}

