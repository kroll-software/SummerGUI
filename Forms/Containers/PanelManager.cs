using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using KS.Foundation;

namespace SummerGUI
{
	public class PanelManager : RootContainer, IStatusPresenter
	{
		public Panel MenuPanel { get; protected set; }
		public Panel StatusPanel { get; protected set; }

		public GuiMenu MainMenu { get; protected set; } 

		public MenuBar MenuBar { get; protected set; }
		public ToolBar ToolBar { get; protected set; }
		public NotificationPanel Notifications  { get; protected set; }
		public StatusBar StatusBar { get; protected set; }

		public SplitContainer MainSplitter { get; protected set; }
		public ScrollableContainer LeftSideBar { get; protected set; }
		public TabContainer TabMain { get; protected set; }

		public bool LeftSideBarVisible
		{
			get{
				return LeftSideBar.Visible;
			}
			set{
				LeftSideBar.Visible = value;
			}
		}

		public ScrollableContainer RightSideBar
		{
			get{
				return MainSplitter.Panel2;
			}
		}

		public bool RightSideBarVisible
		{
			get{
				return !MainSplitter.Panel2Collapsed;
			}
			set{
				MainSplitter.Panel2Collapsed = !value;
			}
		}			

		public PanelManager (IGUIContext ctx)
			: base(ctx)
		{
			MenuPanel = new Panel ("menupanel", Docking.Top);
			AddChild (MenuPanel);

			StatusPanel = new Panel ("statuspanel", Docking.Bottom);
			AddChild (StatusPanel);

			MainMenu = new GuiMenu ("mainmenu");
			MenuBar = new MenuBar ("mainmenubar", MainMenu);
			ToolBar = new ToolBar ("maintoolbar", MainMenu);

			MenuPanel.AddChild (MenuBar);
			MenuPanel.AddChild (ToolBar);

			StatusBar = new StatusBar ("mainstatusbar");
			StatusPanel.AddChild (StatusBar);

			LeftSideBar = new ScrollableContainer ("leftsidebar", Docking.Left);
			AddChild (LeftSideBar);

			MainSplitter = new SplitContainer ("mainsplitter", SplitOrientation.Vertical, -0.25f);
			AddChild (MainSplitter);

			TabMain = new TabContainer ("maintabs");
			MainSplitter.Panel1.AddChild (TabMain);

			RightSideBarVisible = false;
		}

		// *** IStatusPresenter Proxy Implementation ***

		public void ShowStatus()
		{
			StatusBar.ShowStatus ();
		}

		public void ClearStatus()
		{
			StatusBar.ClearStatus ();
		}

		public void ShowStatus(string status, bool waitCursor, bool useStack = true)
		{
			StatusBar.ShowStatus (status, waitCursor, useStack);
		}

		public void ShowProgress(int percentDone)
		{
			StatusBar.ShowProgress (percentDone);
		}

		public IDisposable DisableGUI ()
		{
			return StatusBar.DisableGUI ();
		}
	}
}

