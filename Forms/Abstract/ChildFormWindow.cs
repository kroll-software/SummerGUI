using System;
using OpenTK;
using OpenTK.Input;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Windowing.Desktop;
using KS.Foundation;
using System.Runtime.CompilerServices;
using System.Drawing;

namespace SummerGUI
{	
	public enum WindowPositions
	{
		Default,
		CenterParent,
		CenterScreen,
		Manual
	}

	/***
	public enum WindowSizeModes {
	}
	***/

	public enum DialogResults
	{				
		Cancel,
		OK,
		Yes,
		No,
		Continue,
		Repeat
	}
		
	public abstract class ChildFormWindow : SummerGUIWindow, IChildFormHost, IComparable<ChildFormWindow>
	{			
		// IChildFormHost Interface
		public bool IsModal { get; private set; }
		public bool ShowInTaskBar { get; set; }
		public bool AllowMinimize { get; set; }
		public bool AllowMaximize { get; set; }

		public DialogResults Result { get; protected set; }

		//public ChildFormWindow (string name, string caption, int width, int height, SummerGUIWindow parent, bool modal = false, WindowPositions position = WindowPositions.CenterParent)
		//	: base(caption, width, height, parent)

		public ChildFormWindow (string name, string caption, int width, int height, SummerGUIWindow parent, bool modal = false, bool sizable = false, WindowPositions position = WindowPositions.CenterParent, int frameRate = 30)
			: base(new NativeWindowSettings()
			{
				SharedContext = parent.Context,
				Title = caption,
				ClientSize = new Vector2i(width, height),
				StartVisible = false,
				StartFocused = false,
				//IsEventDriven = false,
				//Location = new Vector2i(parent.Location.X, parent.Location.Y),
				//MinimumClientSize = new Vector2i(100, 100),
				//MaximumClientSize = new Vector2i(100, 100),
				
				AutoLoadBindings = true,
				WindowState = WindowState.Normal,				
				//API = ContextAPI.OpenGL,
				//APIVersion = new Version(3, 3), 				
				Profile = ContextProfile.Core,
				NumberOfSamples = 4,
				WindowBorder = sizable ? WindowBorder.Resizable : WindowBorder.Fixed,				
			}, parent, frameRate)
		{
			if (String.IsNullOrEmpty (name))
				name = "ChildWindow";
			
			//NativeWindowSettings test = new NativeWindowSettings();
			//test.StartVisible = false
			
			Name = name;
			IsModal = modal;
			ParentWindow = parent;
			BackColor = Color.White;

			if (!IsModal) {
				ShowInTaskBar = true;
				AllowMinimize = true;
				AllowMaximize = true;
			} else {	
				ShowInTaskBar = false;
				AllowMinimize = false;
				AllowMaximize = false;
				// ToDo: FixMe:
				this.HideFromTaskbar ();
			}

			if (position == WindowPositions.CenterParent)
			{
				CenterWindowOnParent(ParentWindow, this);
			}
			
			if (parent != null){
				//this.SetModalState(parent, modal);
				this.SetParent(parent);
			}

			ParentWindow?.AddChildWindow (this);

			// ToDo: Set Window Modal State on Platform
			/**	ToDo:
			m_pDisplay->Create( NULL,  //CWnd default
				NULL,   //has no name
				WS_CHILD|WS_CLIPSIBLINGS|WS_CLIPCHILDREN|WS_VISIBLE,
				rect,
				this,   //this is the parent
				0);     //this should really be a different 
			//  number... check resource.h

			return TRUE;  // return TRUE  unless you set 
			//    the focus to a control
			**/			
		}

		private static void CenterWindowOnParent(SummerGUIWindow parent, SummerGUIWindow child)
		{
			Vector2i parentLocation = parent.Location;
			Vector2i parentSize = parent.Size;
			Vector2i childSize = child.Size;

			int newX = parentLocation.X + (parentSize.X - childSize.X) / 2;
			int newY = parentLocation.Y + (parentSize.Y - childSize.Y) / 2;

			child.Location = new Vector2i(newX, newY);
		}

		public override void DetectDPI ()
		{
			base.DetectDPI ();
			if (ParentWindow != null)
				ScaleFactor = ParentWindow.ScaleFactor;
		}
			
		/***
		protected override void OnInitFonts ()
		{
			// do nothing!
		}

		protected override void OnInitCursors ()
		{
			// do nothing again!
		}
		***/

		/***
		public override FontManager FontManager {
			get {
				return ParentWindow.FontManager;
			}
		}

		protected override void InitFonts ()
		{
			// do nothing again!
		}
		***/

		/*** brauchen wir das wirklich ???
		private Button m_DefaultButton;
		public Button DefaultButton
		{
			get{
				return m_DefaultButton;
			}
			set{
				if (m_DefaultButton != value) {
					m_DefaultButton = value;
				}
			}
		}

		private Button m_CancelButton;
		public Button CancelButton
		{
			get{
				return m_CancelButton;
			}
			set{
				if (m_CancelButton != value) {
					m_CancelButton = value;
				}
			}
		}
		***/

		public int CompareTo(ChildFormWindow other)
		{
			if (other == null || other.Name == null || Name == null)
				return 0;
			return this.Name.CompareTo (other.Name);
		}

		public IGUIContext GUIContext
		{
			get{
				return this;
			}
		}

		public virtual void OnOK()
		{
			Result = DialogResults.OK;
			this.Close ();
		}

		public virtual void OnCancel()
		{
			Result = DialogResults.Cancel;
			this.Close ();
		}

        /***
		protected override void OnKeyDown (KeyboardKeyEventArgs e)
		{
			switch (e.Key) {
			case Key.Escape:
				OnCancel ();
				break;
			case Key.Enter:				
				OnOK ();
				break;
			default:
				base.OnKeyDown (e);
				break;
			}				
		}
		****/

        protected override void OnRenderFrame(double elapsedSeconds)
        {
			Batcher.BindContext(this);
            base.OnRenderFrame(elapsedSeconds);
        }

		public void Show(SummerGUIWindow parent)
		{
			if (ParentWindow != parent)
				this.LogWarning ("Parent should equal ParentWindow");
									
			this.IsVisible = true;			
			this.Focus();			
			this.BringToFront();
			
			Batcher.BindContext(this);
			this.Run ();
		}        

		public override void OnProcessEvents ()
		{
			base.OnProcessEvents ();			
			//ParentWindow?.OnProcessEvents ();
		}

		public override void OnDispatchUpdateAndRenderFrame()
		{
			base.OnDispatchUpdateAndRenderFrame ();			
			//ParentWindow?.OnDispatchUpdateAndRenderFrame ();
		}

		protected override void OnUnload (EventArgs e)
		{				
			if (ParentWindow != null) {
				ParentWindow.RemoveChildWindow (this);			
				ParentWindow.Focus ();				
			}			

			base.OnUnload (e);
		}

        protected override void Dispose(bool manual)
        {
			Batcher.UnbindContext(this);
            base.Dispose(manual);
        }
	}
}

