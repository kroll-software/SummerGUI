using System;
using System.IO;
using System.Drawing;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Input;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using KS.Foundation;

namespace SummerGUI
{
	public class ApplicationWindow : SummerGUIWindow, IApplicationWindow, IStatusPresenter
	{
		public MenuPanel MenuPanel { get; protected set; }
		public Panel StatusPanel { get; protected set; }

		public StatusBar StatusBar { get; protected set; }
		public SideMenuContainer LeftSideMenu { get; protected set; }

		public ComplexSplitContainer MainSplitter { get; protected set; }
		public TabContainer TabMain { get; protected set; }

		public ApplicationWindowDiagnostics Diagnostics { get; protected set; }

		public ApplicationWindow (string caption, int width, int height)
			: base(caption, width, height)
		{		
			Name = "ApplicationWindow";

			MainMenu = new GuiMenu ("mainmenu");
			MenuPanel = AddChild (new MenuPanel (MainMenu));
			MenuPanel.WebsiteTitle = "Visit the Kroll-Software Website..";
			MenuPanel.WebsiteURL = "http://www.kroll-software.ch";

			StatusPanel = AddChild (new Panel ("statuspanel", Docking.Bottom));

			StatusBar = StatusPanel.AddChild (new StatusBar ("mainstatusbar"));

			MainSplitter = AddChild (new ComplexSplitContainer ("mainsplitter", Docking.Fill, new DarkGradientPanelWidgetStyle()));
			MainSplitter.PanelBottomCollapsed = true;
			MainSplitter.PanelRightCollapsed = true;
			MainSplitter.SplitterLeft.Distance = 200;

			TabMain = MainSplitter.PanelCenter.AddChild (new TabContainer ("maintabs"));
			TabMain.Dock = Docking.Fill;

			/******/
			LeftSideMenu = MainSplitter.PanelLeft.AddChild (new SideMenuContainer ("leftmenu", MenuPanel.MainMenu));
			LeftSideMenu.Dock = Docking.Fill;
			LeftSideMenu.MenuBar.Close += (object sender, EventArgs e) => this.LeftSideBarVisible = false;
			MainSplitter.SplitterLeft.MinDistanceNear = LeftSideMenu.MinSize.Width;

			MenuPanel.InitMenu ();
			//initialized = true;
		}

		// *********** Menu Updates **********

		public override void OnScalingChanged ()
		{
			base.OnScalingChanged ();
			if (MenuPanel != null)
				MenuPanel.UpdateScalingMenu (ScaleFactor);
		}

		// *** IStatusPresenter Proxy Implementation ***
		public override void OnDeviceChanged (Devices currentDevice)
		{
			base.OnDeviceChanged (currentDevice);
			if (currentDevice == Devices.Mobile) {
				LeftSideBarVisible = false;
			}
			if (MenuPanel != null) {
				MenuPanel.UpdateDeviceMenu (currentDevice);
			}
		}

		protected override void OnLoad (EventArgs e)
		{
			base.OnLoad (e);
			MenuPanel.UpdateMenuBarsVisible (LeftSideBarVisible, MenuPanel.ToolBar.Visible, StatusBar.Visible);

			if (Controller != null && StatusBar != null)
				Controller.Subscribe (StatusBar.RootControllerObserver);
		}

		protected override void OnWindowStateChanged (EventArgs e)
		{
			base.OnWindowStateChanged (e);
			if (MenuPanel != null)
				MenuPanel.UpdateWindowStateMenu (this.WindowState);
		}

		public override void OnLoadSettings ()
		{			
			base.OnLoadSettings ();
			if (WindowState != WindowState.Minimized) {
				ConfigurationService.Instance.ConfigFile.Do (cfg => {					
					MainSplitter.PanelLeftCollapsed = cfg.GetSetting(Name, "MainSplitter.PanelLeftCollapsed", MainSplitter.PanelLeftCollapsed).SafeBool();
					MainSplitter.SplitterLeft.Distance = cfg.GetSetting(Name, "MainSplitter.SplitterLeft", MainSplitter.SplitterLeft.Distance).SafeFloat();
					MainSplitter.PanelRightCollapsed = cfg.GetSetting(Name, "MainSplitter.PanelRightCollapsed", MainSplitter.PanelRightCollapsed).SafeBool();
					MainSplitter.SplitterRight.Distance = cfg.GetSetting(Name, "MainSplitter.SplitterRight", MainSplitter.SplitterRight.Distance).SafeFloat();
					MainSplitter.PanelBottomCollapsed = cfg.GetSetting(Name, "MainSplitter.PanelBottomCollapsed", MainSplitter.PanelBottomCollapsed).SafeBool();
					MainSplitter.SplitterBottom.Distance = cfg.GetSetting(Name, "MainSplitter.SplitterBottom", MainSplitter.SplitterBottom.Distance).SafeFloat();
					MenuPanel.MenuBar.Visible = cfg.GetSetting(Name, "MenuBar.Visible", MenuPanel.MenuBar.Visible).SafeBool();
					MenuPanel.ToolBar.Visible = cfg.GetSetting(Name, "ToolBar.Visible", MenuPanel.ToolBar.Visible).SafeBool();
					StatusBar.Visible = cfg.GetSetting(Name, "StatusBar.Visible", StatusBar.Visible).SafeBool();
				});

				MenuPanel.UpdateMenus ();
				LeftSideMenu.Init ();
			}
		}

		public override void OnSaveSettings ()
		{
			base.OnSaveSettings ();
			if (WindowState != WindowState.Minimized) {
				ConfigurationService.Instance.ConfigFile.Do (cfg => {					
					cfg.SetSetting (Name, "MainSplitter.PanelLeftCollapsed", MainSplitter.PanelLeftCollapsed.ToLowerString());
					cfg.SetSetting (Name, "MainSplitter.SplitterLeft", MainSplitter.SplitterLeft.Distance / ScaleFactor);
					cfg.SetSetting (Name, "MainSplitter.PanelRightCollapsed", MainSplitter.PanelRightCollapsed.ToLowerString());
					cfg.SetSetting (Name, "MainSplitter.SplitterRight", MainSplitter.SplitterRight.Distance / ScaleFactor);
					cfg.SetSetting (Name, "MainSplitter.PanelBottomCollapsed", MainSplitter.PanelBottomCollapsed.ToLowerString());
					cfg.SetSetting (Name, "MainSplitter.SplitterBottom", MainSplitter.SplitterBottom.Distance / ScaleFactor);
					cfg.SetSetting (Name, "MenuBar.Visible", MenuPanel.MenuBar.Visible.ToLowerString());
					cfg.SetSetting (Name, "ToolBar.Visible", MenuPanel.ToolBar.Visible.ToLowerString());
					cfg.SetSetting (Name, "StatusBar.Visible", StatusBar.Visible.ToLowerString());
					//cfg.SetSetting ("Application", "ScaleFactor", ScaleFactor.ToString("n3"));
				});
			}
		}

		public void ToggleLeftSideBarVisible()
		{
			LeftSideBarVisible = !LeftSideBarVisible;
		}

		public bool LeftSideBarVisible
		{
			get{
				return !MainSplitter.PanelLeftCollapsed;
			}
			set{
				if (!MainSplitter.PanelLeftCollapsed != value) {
					MainSplitter.PanelLeftCollapsed = !value;
					if (IsCreated)
						OnLeftSideBarVisibleChanged ();
				}
			}
		}

		protected virtual void OnLeftSideBarVisibleChanged()
		{
			MenuPanel.UpdateMenuBarsVisible (LeftSideBarVisible, MenuPanel.ToolBar.Visible, StatusBar.Visible);
		}			

		public ScrollableContainer RightSideBar
		{
			get{
				return MainSplitter.PanelRight;
			}
		}

		public bool RightSideBarVisible
		{
			get{
				return !MainSplitter.PanelRightCollapsed;
			}
			set{
				if (MainSplitter.PanelRightCollapsed == value) {
					MainSplitter.PanelRightCollapsed = !value;
					if (IsCreated)
						OnRightSideBarVisibleChanged ();
				}
			}
		}

		protected virtual void OnRightSideBarVisibleChanged()
		{			
		}			

		public virtual void AboutScreen()
		{
			AboutScreen DlgAbout = new AboutScreen("About Summer GUI", this);
			DlgAbout.ProgramTitle = "Summer GUI";
			DlgAbout.ProgramSubTitle = "by John Summer. A lightweight X-Platform GUI Framework written in C#";
			//DlgAbout.UrlCaption = "Visit website:";
			DlgAbout.LicenseInfo = "License: MIT";
			DlgAbout.Url = "http://www.kroll-software.ch";
			DlgAbout.UrlText = "www.kroll-software.ch";
			DlgAbout.Copyright = "Produced 2016-2019 by Kroll-Software, free to copy and use.";
			// ToDo: DPI Scaling
			DlgAbout.ImagePath = "Assets/Logo/SummerGUI_96px.png".FixedExpandedPath();	

			// load license texts
			DlgAbout.AddCreditsFromTextFile(Strings.ApplicationPath(true) + "SummerGUI-Credits.txt");
			DlgAbout.AddCreditsParagraph("\n");
			DlgAbout.AddCreditsImage(Strings.ApplicationPath(true) + "Assets/Logo/kroll-software-logo.png", this, 0.85f);

			DlgAbout.Show (this);
			DlgAbout.Dispose();
		}


		public void ShowStatus(string message, bool waitCursor, bool useStack = true)
		{
			StatusBar.ShowStatus (message, waitCursor, useStack);
		}

		public void ShowStatus()
		{
			StatusBar.ShowStatus ();
		}

		public void ClearStatus()
		{
			StatusBar.ClearStatus ();
		}
			
		public void ShowProgress(int percentDone)
		{
			StatusBar.ShowProgress (percentDone);
		}
			
		public IDisposable DisableGUI ()
		{
			try {
				return StatusBar.DisableGUI ();
			} catch (Exception ex) {
				ex.LogError ();	
				return null;
			}
		}

		// *** Notifications
		public void ShowNotification(string message, ColorContexts context, char icon = (char)0, bool canClose = true, double autoCloseSeconds = 10)
		{
			try {
				MenuPanel.Notifications.AddNotification (message, context, icon, canClose, autoCloseSeconds);	
			} catch (Exception ex) {
				ex.LogError ();
			}
		}
			
		public bool IsDiagnosticsRunning { get; private set; }
		public void StartDiagnostics()
		{
			lock(SyncObject) {
				if (!IsDiagnosticsRunning && Diagnostics == null) {
					Diagnostics = new ApplicationWindowDiagnostics (this);
					IsDiagnosticsRunning = true;
					//Invalidate ();
				}
			}
		}

		public void StopDiagnostics()
		{
			lock(SyncObject) {
				if (IsDiagnosticsRunning && Diagnostics != null) {
					IsDiagnosticsRunning = false;
					Diagnostics.Dispose ();
					Diagnostics = null;
				}
			}
		}

		protected override void OnUpdateFrame (double elapsedSeconds)
		{			
			if (IsDiagnosticsRunning)
				Diagnostics.PulseLayout ();
			base.OnUpdateFrame (elapsedSeconds);
		}

		protected override void OnRenderFrame (double elapsedSeconds)
		{						
			base.OnRenderFrame (elapsedSeconds);
			if (IsDiagnosticsRunning)
				Diagnostics.PulsePaint ();
		}			
	}
}

