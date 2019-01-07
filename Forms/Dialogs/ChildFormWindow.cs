using System;
using OpenTK;
using OpenTK.Input;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using KS.Foundation;

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
		
	public class ChildFormWindow : SummerGUIWindow, IChildFormHost, IComparable<ChildFormWindow>
	{			
		// IChildFormHost Interface
		public bool IsModal { get; private set; }
		public bool ShowInTaskBar { get; set; }
		public bool AllowMinimize { get; set; }
		public bool AllowMaximize { get; set; }

		public DialogResults Result { get; protected set; }

		public ChildFormWindow (string name, string caption, int width, int height, SummerGUIWindow parent, bool modal = false, GameWindowFlags flags = GameWindowFlags.Default, WindowPositions position = WindowPositions.CenterParent)
			: base(caption, width, height, parent, flags)
		{		
			if (String.IsNullOrEmpty (name))
				name = "ChildWindow";
			
			Name = name;
			IsModal = modal;

			if (!IsModal) {
				ShowInTaskBar = true;
				AllowMinimize = flags == GameWindowFlags.Default;
				AllowMaximize = flags == GameWindowFlags.Default;
			} else {	
				// ToDo: FixMe:
				this.HideFromTaskbar ();
			}

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

		public void Show(SummerGUIWindow parent)
		{
			if (ParentWindow != parent)
				this.LogWarning ("Parent should equal ParentWindow");

			if (ParentWindow != null)
				ParentWindow.AddChildWindow (this);			
			this.Run ();
		}			

		public override void OnProcessEvents ()
		{
			base.OnProcessEvents ();
			if (ParentWindow != null) {				
				ParentWindow.OnProcessEvents ();
			}
		}			

		protected override void OnUnload (EventArgs e)
		{				
			if (ParentWindow != null) {
				ParentWindow.RemoveChildWindow (this);
				ParentWindow.MakeCurrent ();
				ParentWindow.Focus ();
			}

			base.OnUnload (e);
		}
	}
}

