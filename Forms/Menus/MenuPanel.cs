using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using KS.Foundation;

namespace SummerGUI
{
	public class MenuPanel : Panel
	{		
		ApplicationWindow AppWin;

		public string WebsiteTitle { get; set; }
		public string WebsiteURL { get; set; }

		public IGuiMenu MainMenu { get; protected set; } 
		public MenuBar MenuBar { get; protected set; }
		public ApplicationToolBar ToolBar { get; protected set; }
		public NotificationPanel Notifications  { get; protected set; }

		public MenuPanel(IGuiMenu menu) : this ("menupanel", menu) {}
		public MenuPanel (string name, IGuiMenu menu)
			: base(name, Docking.Top)
		{			
			//MainMenu = new GuiMenu ("mainmenu");
			IsMenu = true;
			MainMenu = menu;

			MenuBar = AddChild (new MenuBar ("mainmenubar", MainMenu));
			ToolBar = AddChild (new ApplicationToolBar ("maintoolbar", MainMenu));
			Notifications = AddChild (new NotificationPanel ("notifications"));
			Notifications.MaxNotifications = 3;

			ToolBar.Margin = Padding.Empty;
			ToolBar.Style.BorderColorPen.Color = System.Drawing.Color.Empty;

			ToolBar.HamburgerMenu.Visible = true;

			ToolBar.HamburgerMenu.Click += delegate {				
				ToggleMobileMenu();
			};
		}

		protected override void OnParentChanged ()
		{
			base.OnParentChanged ();
			if (Parent != null)
				AppWin = ParentWindow as ApplicationWindow;
			else
				AppWin = null;
		}	
			
		public bool MenuBarVisible 
		{ 
			get {
				return MenuBar.Visible;
			}
			set {
				MenuBar.Visible = value;
			}
		}

		public override void UpdateMenus()
		{
			if (AppWin != null) {
				mnuShowMenuBar.Checked = MenuBarVisible;
				mnuShowLeftSideBar.Checked = AppWin.LeftSideBarVisible;
				mnuShowToolBar.Checked = ToolBar.Visible;
			}
		}

		public override void OnLayout (IGUIContext ctx, System.Drawing.RectangleF bounds)
		{
			if (IsLayoutSuspended)
				return;
			base.OnLayout (ctx, bounds);
			if (!MenuBarVisible && (ctx.Device == Devices.Desktop || ctx.Device == Devices.Tablet))
				MenuBar.Visible = false;
			//else if (MenuBarVisible)
			//	ToolBar.HamburgerMenu.Visible = ctx.Device == Devices.Mobile;
			EnsureAnyMenu ();
		}

		/***
		public override bool OnKeyDown (KeyboardKeyEventArgs e)
		{
			if (base.OnKeyDown (e))
				return true;
			switch (e.Key) {
			case Key.F10:
				if (AppWin.Device == Devices.Mobile) {
					AppWin.LeftSideBarVisible = true;
				} else {
					MenuBar.Visible = true;
					MenuBar.Select ();
				}
				Invalidate ();
				return true;
			}
			return false;
		}
		***/
			
		public void ToggleMobileMenu()
		{
			(AppWin).Do (app => {
				app.ToggleLeftSideBarVisible ();
				mnuShowLeftSideBar.Checked = app.LeftSideBarVisible;
			});
			EnsureAnyMenu ();
		}

		// **************** Some Default Menu Items *****************

		IGuiMenuItem mnuShowMenuBar;
		IGuiMenuItem mnuShowToolBar;
		IGuiMenuItem mnuShowStatusBar;
		IGuiMenuItem mnuShowLeftSideBar;
		IGuiMenuItem mnuFullScreen;
		IGuiMenuItem mnuScaling;

		IGuiMenuItem mnuDeviceMobile;
		IGuiMenuItem mnuDeviceTablet;
		IGuiMenuItem mnuDeviceDesktop;

		public void UpdateDeviceMenu(Devices value)
		{
			switch (value) {
			case Devices.Mobile:
				mnuDeviceMobile.OnClick (false);
				mnuShowToolBar.Enabled = false;
				break;
			case Devices.Tablet:
				mnuDeviceTablet.OnClick (false);
				mnuShowToolBar.Enabled = true;
				break;
			case Devices.Desktop:
				mnuDeviceDesktop.OnClick (false);
				mnuShowToolBar.Enabled = true;
				MenuBar.Visible = true;
				mnuShowMenuBar.Checked = true;
				break;
			}
		}

		public void UpdateScalingMenu(float value)
		{			
			foreach (IGuiMenuItem mnu in mnuScaling.Children) {				
				if (Math.Abs(mnu.Tag.SafeFloat () - value) < 0.1) {
					mnu.OnClick (false);
					break;
				}
			}
		}

		public void UpdateMenuBarsVisible(bool leftSidebarVisible, bool toolbarVisible, bool StatusbarVisible)
		{
			mnuShowLeftSideBar.Checked = leftSidebarVisible;
			mnuShowToolBar.Checked = toolbarVisible;
			mnuShowStatusBar.Checked = StatusbarVisible;

			if (ToolBar.HamburgerMenuVisible)
				BlinkHamburgerMenu ();
		}

		public void UpdateWindowStateMenu (WindowState value)
		{
			mnuFullScreen.Checked = (value == WindowState.Fullscreen);
		}

		public void EnsureAnyMenu()
		{
			if (!(MenuBar.Visible || (ToolBar.Visible && ToolBar.HamburgerMenuVisible) || AppWin.LeftSideBarVisible)) {
				//ToolBar.Visible = true;
				if (!mnuShowToolBar.Checked)
					mnuShowToolBar.OnClick();
				ToolBar.HamburgerMenu.Visible = true;
				BlinkHamburgerMenu ();
			}
		}

		void BlinkHamburgerMenu()
		{
			// Animate / Blink Hamburger Menu !
			if (AppWin != null && AppWin.Device != Devices.Mobile && !MenuBar.Visible)
				AppWin.Animator.AddAnimation(this, "HamburgerMenuBlinker", 0, 10, 5);
		}

		private int m_HamburgerMenuBlinker;
		public int HamburgerMenuBlinker { 
			get {
				return m_HamburgerMenuBlinker;
			}
			set {
				m_HamburgerMenuBlinker = value;
				if (ToolBar.HamburgerMenuVisible) {
					if (value % 2 != 0)
						ToolBar.HamburgerMenu.BackColor = Theme.Colors.Red;
					else if (value == 10)
						ToolBar.HamburgerMenu.BackColor = Theme.Colors.Base01;
					else
						ToolBar.HamburgerMenu.BackColor = Theme.Colors.Base02;
				}
			}
		}

		#pragma warning disable 219		// disable warnings about unused menu items

		public void InitMenu()
		{						
			IGuiMenuItem mnuFile = MainMenu.Add ("File", "&File");

            IGuiMenuItem mnuNew = mnuFile.AddChild("New", "&New..", (char)FontAwesomeIcons.fa_file_o)
                .SetHotKey(KeyModifiers.Control, Key.N);
            IGuiMenuItem mnuOpen = mnuFile.AddChild ("Open", "&Open..", (char)FontAwesomeIcons.fa_folder_open_o)
				.SetHotKey(KeyModifiers.Alt, Key.O);
            IGuiMenuItem mnuSave = mnuFile.AddChild("Save", "&Save..", (char)FontAwesomeIcons.fa_save)
                .SetHotKey(KeyModifiers.Control, Key.S);

            mnuFile.AddSeparator ();

			IGuiMenuItem mnuExit = mnuFile.AddChild ("Exit", "E&xit").SetHotKey(KeyModifiers.Alt, Key.F4);
			mnuExit.Click += delegate {
				AppWin.Close ();
			};

			// *************************

			IGuiMenuItem mnuEdit = MainMenu.Add ("Edit", "&Edit");
			IGuiMenuItem mnuCopy = mnuEdit.AddChild ("Copy", "Copy", (char)FontAwesomeIcons.fa_copy)
				.SetHotKey(KeyModifiers.Control, Key.C);
			IGuiMenuItem mnuPaste = mnuEdit.AddChild ("Paste", "Paste", (char)FontAwesomeIcons.fa_paste)
				.SetHotKey(KeyModifiers.Control, Key.V);
			IGuiMenuItem mnuCut = mnuEdit.AddChild ("Cut", "Cut", (char)FontAwesomeIcons.fa_cut)
				.SetHotKey(KeyModifiers.Control, Key.X);
			mnuEdit.AddSeparator ();
			IGuiMenuItem mnuUndo = mnuEdit.AddChild ("Undo", "Undo", (char)FontAwesomeIcons.fa_undo)
				.SetHotKey(KeyModifiers.Control, Key.Z);
			IGuiMenuItem mnuRedo = mnuEdit.AddChild ("Redo", "Redo", (char)FontAwesomeIcons.fa_repeat)
				.SetHotKey(KeyModifiers.Control, Key.Y);
			mnuEdit.AddSeparator ();
			IGuiMenuItem mnuSelectAll = mnuEdit.AddChild ("SelectAll", "Select All", (char)0)
				.SetHotKey(KeyModifiers.Control, Key.A);
			IGuiMenuItem mnuInvertSelection = mnuEdit.AddChild ("InvertSelection", "Invert Selection", (char)0)
				.SetHotKey(KeyModifiers.Control, Key.I);

			// Test: Disable something
			mnuCopy.Enabled = false;
			mnuRedo.Enabled = false;
			mnuCut.Enabled = false;

			// ***************************

			IGuiMenuItem mnuView = MainMenu.Add ("View", "&View");
			mnuView.AddChild ("ZoomIn", "Zoom In", (char)FontAwesomeIcons.fa_search_plus).SetFireButton();
			mnuView.AddChild ("ZoomOut", "Zoom Out", (char)FontAwesomeIcons.fa_search_minus).SetFireButton();
			mnuView.AddChild ("Find", "Find..", (char)FontAwesomeIcons.fa_binoculars).SetHotKey(KeyModifiers.Control, Key.F).HideFromToolbar();

			IGuiMenuItem mnuShowMenus = mnuView.AddChild ("ShowMenus", "Menus");

			mnuShowMenuBar = mnuShowMenus.AddChild ("ShowMenuBar", "Menubar", (char)FontAwesomeIcons.fa_list)
				.SetChecked(MenuBar.Visible);
			mnuShowMenuBar.CheckedChanged += delegate {				
				MenuBar.Visible = mnuShowMenuBar.Checked;
				EnsureAnyMenu ();
			};

			mnuShowToolBar = mnuShowMenus.AddChild ("ShowToolBar", "Toolbar", (char)FontAwesomeIcons.fa_ellipsis_h)
				.SetChecked(ToolBar.Visible).SetHotKey(KeyModifiers.Control, Key.F9);
			mnuShowToolBar.CheckedChanged += delegate {
				ToolBar.Visible = mnuShowToolBar.Checked;
				EnsureAnyMenu ();
			};												

			mnuShowLeftSideBar = mnuShowMenus.AddChild ("ShowLeftSideBar", "Sidebar", (char)FontAwesomeIcons.fa_hand_o_right)
				.SetChecked (AppWin.LeftSideBarVisible);
			mnuShowLeftSideBar.Click += delegate {				
				AppWin.ToggleLeftSideBarVisible();
				EnsureAnyMenu ();
			};

			mnuShowStatusBar = mnuShowMenus.AddChild ("ShowStatusBar", "Statusbar", (char)FontAwesomeIcons.fa_hand_o_right)
				.SetChecked (AppWin.StatusBar.Visible).SetHotKey(KeyModifiers.Shift, Key.F9);
			mnuShowStatusBar.CheckedChanged += delegate {
				AppWin.StatusBar.Visible = mnuShowStatusBar.Checked;
			};

			/***
			IGuiMenuItem mnuSpecialCharacters = mnuView.AddChild ("NonPrintingCharacters", "Non-Printing Characters");
			mnuSpecialCharacters.AddChild ("WhiteSpaceVisible", "White Space").SetChecked (true);
			mnuSpecialCharacters.AddChild ("LineBreaksVisible", "Line Breaks").SetChecked (true);
			mnuSpecialCharacters.AddChild ("EndOfTextVisible", "End Of Text").SetChecked (true);
			***/
				
			mnuScaling = mnuView.AddChild ("Scaling", "Scaling / Zoom");
			mnuScaling.Tag = 0f;

			IGuiMenuItem mnuScaleNormal = mnuScaling.AddChild ("ScaleNormal", "Normal");
			mnuScaleNormal.IsToggleButton = true;
			mnuScaleNormal.IsOptionGroup = true;
			mnuScaleNormal.Tag = 1f;
			mnuScaleNormal.Click += delegate {
				AppWin.ScaleGUI(1f);
			};

			IGuiMenuItem mnuScale112 = mnuScaling.AddChild ("Scale112", String.Format("Scale {0}%", (112.5).ToString()));
			mnuScale112.IsToggleButton = true;
			mnuScale112.IsOptionGroup = true;
			mnuScale112.Tag = 1.125f;
			mnuScale112.Click += delegate {
				AppWin.ScaleGUI(1.125f);
			};

			IGuiMenuItem mnuScale125 = mnuScaling.AddChild ("Scale125", "Scale 125%");
			mnuScale125.IsToggleButton = true;
			mnuScale125.IsOptionGroup = true;
			mnuScale125.Tag = 1.25f;
			mnuScale125.Click += delegate {
				AppWin.ScaleGUI(1.25f);
			};

			IGuiMenuItem mnuScale150 = mnuScaling.AddChild ("Scale150", "Scale 150%");
			mnuScale150.IsToggleButton = true;
			mnuScale150.IsOptionGroup = true;
			mnuScale150.Tag = 1.5f;
			mnuScale150.Click += delegate {
				AppWin.ScaleGUI(1.5f);
			};

			IGuiMenuItem mnuScale175 = mnuScaling.AddChild ("Scale175", "Scale 175%");
			mnuScale175.IsToggleButton = true;
			mnuScale175.IsOptionGroup = true;
			mnuScale175.Tag = 1.75f;
			mnuScale175.Click += delegate {
				AppWin.ScaleGUI(1.75f);
			};

			IGuiMenuItem mnuScale200 = mnuScaling.AddChild ("Scale200", "Scale 200%");
			mnuScale200.IsToggleButton = true;
			mnuScale200.IsOptionGroup = true;
			mnuScale200.Tag = 2f;
			mnuScale200.Click += delegate {
				AppWin.ScaleGUI(2f);
			};

			IGuiMenuItem mnuScale225 = mnuScaling.AddChild ("Scale225", "Scale 225%");
			mnuScale225.IsToggleButton = true;
			mnuScale225.IsOptionGroup = true;
			mnuScale225.Tag = 2.25f;
			mnuScale225.Click += delegate {
				AppWin.ScaleGUI(2.25f);
			};

			IGuiMenuItem mnuScale250 = mnuScaling.AddChild ("Scale250", "Scale 250%");
			mnuScale250.IsToggleButton = true;
			mnuScale250.IsOptionGroup = true;
			mnuScale250.Tag = 2.5f;
			mnuScale250.Click += delegate {
				AppWin.ScaleGUI(2.5f);
			};

			IGuiMenuItem mnuScaleAuto = mnuScaling.AddChild ("ScaleAuto", "Auto-Detect by Display DPI");
			mnuScaleAuto.IsToggleButton = true;
			mnuScaleAuto.IsOptionGroup = true;
			mnuScaleAuto.Tag = 0f;
			mnuScaleAuto.Click += delegate {
				AppWin.ScaleGUI(0);
			};

			mnuFullScreen = mnuView.AddChild ("FullScreen", "Full Screen")
				.SetHotKey (Key.F11).SetChecked (false);
			mnuFullScreen.Click += delegate {
				AppWin.ToggleFullScreen();
			};

			mnuScaleAuto.SetChecked (true);

			// ********************************

			IGuiMenuItem mnuExtras = MainMenu.Add ("Extras", "Ex&tras");
			IGuiMenuItem mnuOptions = mnuExtras.AddChild ("Options", "Options..", (char)FontAwesomeIcons.fa_gear);

			Devices device = ParentWindow.Device;
			if (device == Devices.Unknown)
				device = Devices.Desktop;

			// Device Selection Checkbutton Group
			mnuDeviceMobile = mnuExtras.AddChild ("DeviceMobile", "Running on Mobile", (char)FontAwesomeIcons.fa_gear);
			mnuDeviceMobile.IsToggleButton = true;
			mnuDeviceMobile.IsOptionGroup = true;
			mnuDeviceMobile.Checked = device == Devices.Mobile;
			mnuDeviceMobile.Click += delegate {
				AppWin.Device = Devices.Mobile;
				if (!ToolBar.Visible)
					mnuShowToolBar.OnClick();
				if (MenuBar.Visible)
					mnuShowMenuBar.OnClick();
				this.UpdateDeviceMenu(Devices.Mobile);
			};				
			mnuDeviceTablet = mnuExtras.AddChild ("DeviceTablet", "Running on Tablet", (char)FontAwesomeIcons.fa_gear);
			mnuDeviceTablet.Checked = device == Devices.Tablet;
			mnuDeviceTablet.IsToggleButton = true;
			mnuDeviceTablet.IsOptionGroup = true;
			mnuDeviceTablet.Click += delegate {
				AppWin.Device = Devices.Tablet;
				this.UpdateDeviceMenu(Devices.Tablet);
			};
			mnuDeviceDesktop = mnuExtras.AddChild ("DeviceDesktop", "Running on Desktop", (char)FontAwesomeIcons.fa_gear);
			mnuDeviceDesktop.Checked = device == Devices.Desktop;
			mnuDeviceDesktop.IsToggleButton = true;
			mnuDeviceDesktop.IsOptionGroup = true;
			mnuDeviceDesktop.Click += delegate {
				AppWin.Device = Devices.Desktop;
				this.UpdateDeviceMenu(Devices.Desktop);
			};

			// ************************

			IGuiMenuItem mnuHelp = MainMenu.Add ("Help", "?");
			IGuiMenuItem mnuHelpContents = mnuHelp.AddChild ("HelpContents", "&Help Contents..", (char)FontAwesomeIcons.fa_question_circle_o)
				.SetHotKey(0, Key.F1);

			mnuHelp.AddSeparator ();

			IGuiMenuItem mnuAbout = mnuHelp.AddChild ("About", "&About..", (char)FontAwesomeIcons.fa_info_circle);
			mnuAbout.Click += delegate {
				AppWin.AboutScreen();
			};

			IGuiMenuItem mnuVisitWebsite = mnuHelp.AddChild ("VisitWebsite", WebsiteTitle, (char)FontAwesomeIcons.fa_home);
			mnuVisitWebsite.Click += delegate {
				Helpers.OpenURL (WebsiteURL, AppWin);
			};


			// ************* TOOLBAR REFRESH **************
			ToolBar.RefreshMenu ();

			// ********************************************
		}


		protected override void CleanupManagedResources ()
		{
			Parent = null;
			base.CleanupManagedResources ();
		}

		protected override void CleanupUnmanagedResources ()
		{						
			MainMenu = null;
			Parent = null;
			base.CleanupUnmanagedResources ();
		}
	}
}

