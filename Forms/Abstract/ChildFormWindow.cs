using System;
using System.Threading;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Runtime.CompilerServices;
using OpenTK;
using OpenTK.Input;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Windowing.Desktop;
using KS.Foundation;
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
		None,
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
				//ClientSize = new Vector2i((width * parent.ScaleFactor).Ceil(), (height * parent.ScaleFactor).Ceil()),
				ClientSize = new Vector2i((width * parent?.ScaleFactor ?? 1f).Ceil(), (height * parent?.ScaleFactor ?? 1f).Ceil()),
				StartVisible = false,
				StartFocused = true,
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
			}

			if (position == WindowPositions.CenterParent)
			{
				CenterWindowOnParent(ParentWindow, this);
			}

			DetectDPI ();

			ParentWindow?.AddChildWindow (this);			
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

        protected override void OnRenderFrame(double elapsedSeconds)
        {
			Batcher.BindContext(this);
            base.OnRenderFrame(elapsedSeconds);
        }

		private WindowBorder _oldParentWindowsBorder;		

		public void ShowDialog(SummerGUIWindow parent)
		{						
			if (ParentWindow != parent)
				throw new ArgumentException("Parameter 'parent' must match ParentWindow.");

			//this.IsVisible = true;

			PlatformExtensions.MakeWindowModal(this, ParentWindow);

			_oldParentWindowsBorder = ParentWindow.WindowBorder;
			ParentWindow.WindowBorder = WindowBorder.Fixed;

			this.Focus();			
			
			Batcher.BindContext(this);		

			this.Run ();	

			if (ParentWindow != null) 
			{
				ParentWindow.WindowBorder = _oldParentWindowsBorder;
				PlatformExtensions.EnableWindow(ParentWindow);				
				ParentWindow.RemoveChildWindow (this);				
				ParentWindow.Focus ();				
			}			
		}

		

		public override void OnProcessEvents ()
		{			
			base.OnProcessEvents ();			
			ParentWindow?.OnProcessEvents ();
		}

		public override void OnDispatchUpdateAndRenderFrame()
		{
			base.OnDispatchUpdateAndRenderFrame ();			
			ParentWindow?.OnDispatchUpdateAndRenderFrame ();
		}

		private readonly object _lifecycleLock = new object();
    	private bool _isClosing = false;
    	private bool _isRendering = false;

        protected override void OnPaint(RectangleF bounds)
		{
			// 1. Non-blocking Check: Wenn wir entladen, sofort raus.
			if (_isClosing) return;

			// 2. Markieren, dass wir im kritischen Bereich sind (kein Lock nötig für bool-Assign)
			_isRendering = true;

			try 
			{
				// Hier passiert dein HarfBuzz / LINQ / unsafe Code
				base.OnPaint(bounds);
			}
			finally 
			{
				_isRendering = false;
			}
		}

		protected override void OnAfterPaint()
		{
			base.OnAfterPaint();

			// 3. Signalisiere dem wartenden Unload-Thread, dass dieser Frame fertig ist.
			// Wir locken nur kurz für das Pulse, das behindert die Loop kaum.
			lock (_lifecycleLock)
			{
				System.Threading.Monitor.PulseAll(_lifecycleLock);
			}
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			// Sofort das Flag setzen, damit OnPaint im nächsten Frame (oder sofort) abbricht
			_isClosing = true; 
			base.OnClosing(e);
		}

		protected override void OnUnload(EventArgs e)
		{
			// 4. Hier warten wir nun geduldig, bis die Renderloop signalisiert: "Ich bin raus!"
			lock (_lifecycleLock)
			{
				// Falls OnPaint gerade noch läuft, warten wir auf das Pulse von OnAfterPaint
				if (_isRendering)
				{
					// Timeout als Safety-Net, falls die Loop abstürzt
					System.Threading.Monitor.Wait(_lifecycleLock, 100); 
				}
			}
			
			base.OnUnload(e);
		}		

        protected override void Dispose(bool manual)
        {
			if (IsDisposed)
				return;
				
			Batcher.UnbindContext(this);
            base.Dispose(manual);
        }
	}
}

