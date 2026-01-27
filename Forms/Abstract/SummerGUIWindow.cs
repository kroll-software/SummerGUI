using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Input;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Windowing.Desktop;
//using OpenTK.Platform.X11;
using KS.Foundation;
using OpenTK.Graphics.GL;
using System.ComponentModel.DataAnnotations;
using System.Collections;
using System.Net;

//
// On Linux to use GLFW compiled for Wayland set the environment variable OPENTK_4_USE_WAYLAND=1
//

namespace SummerGUI
{	
	public enum Devices
	{
		Unknown,
		Mobile,
		Tablet,
		Desktop
	}

	public struct LoadingError
	{
		public readonly MessageBoxTypes Context;
		public readonly string Message;

		public LoadingError(MessageBoxTypes context, string message)
		{
			Context = context;
			Message = message;
		}			
	}

	public abstract class SummerGUIWindow : NativeWindow, IGUIContext
	{				
		/***
		public event Action<DragEventArgs> DragEnter;
		public event Action<EventArgs> DragLeave;
		public event Action<DragEventArgs> DragDrop;
		public event Action<DragEventArgs> DragOver;

		public void OnDragEnter(DragEventArgs e) => DragEnter?.Invoke(e);
		public void OnDragLeave(EventArgs e) => DragLeave?.Invoke(e);
		public void OnDragDrop(DragEventArgs e) => DragDrop?.Invoke(e);
		public void OnDragOver(DragEventArgs e) => DragOver?.Invoke(e);
		***/		

		public GUIRenderBatcher Batcher 
		{ 
			get
			{
				return GUIRenderBatcher.Batcher;
			}		
		}		

		private int m_FrameRate = 30;
		public int FrameRate
		{
            get
            {
                return m_FrameRate;
            }            
        }


		// Width und Height verwenden die Größe der Basisklasse (NativeWindow.Size)
		public int Width => this.Size.X; // NativeWindow.Size ist ein Vector2i
		public int Height => this.Size.Y; // NativeWindow.Size ist ein Vector2i
		
		// Beispiel: Falls Sie eine eigene Rectangle-Struktur verwenden, muss sie gemappt werden.
		public new Rectangle Bounds
		{
			get
			{
				return ((Rectangle)base.Bounds);				
			}
		}
		
		// Da Sie IGUIContext implementieren, müssen Sie die GlWindow-Eigenschaft selbst liefern:
		public NativeWindow GlWindow => this;		

		/*** ***/
		private Devices m_Device = Devices.Desktop;
		public Devices Device 
		{ 
			get {
				return m_Device;
			}
			set {
				if (m_Device != value) {
					m_Device = value;
					OnDeviceChanged (m_Device);
				}
			}
		}
		public virtual void DetectDevice()
		{
			if (!IsCreated)
				return;
			if (Height > Width && Width < 800)
				Device = Devices.Mobile;
			else if (Width <= 1200)
				Device = Devices.Tablet;
			else
				Device = Devices.Desktop;
		}
			
		public virtual void OnDeviceChanged(Devices currentDevice)
		{
		}		

		public RootContainer Controls { get; private set; }

		public IGuiMenu MainMenu { get; set; } 
		public MenuManager MenuManager { get; private set; }		

		public string Name { get; protected set; }

		public DpiScalingAutomat DpiScaling { get; private set; }

		public int DPI { get; protected set; }
		public virtual void DetectDPI()
		{
			// ToDo: Detect DPI from the Display-Device and calculate the ScaleFactor
			DPI = 72;
			ScaleFactor = 1f;
		}

		public int OriginalWidth { get; private set; }
		public int OriginalHeight { get; private set; }
		public float LayoutFrameRate { get; set; }
		public float PaintFrameRate { get; set; }

		public bool IsCreated { get; private set; }

		public float ScaleFactor { get; protected set; }
		public SummerGUIWindow ParentWindow  { get; protected set; }

		public ClipBoundStackClass ClipBoundStack { get; private set; }

		public static readonly object SyncObject = new object();		
		
		private static int _instanceCount = 0;

		protected SummerGUIWindow(NativeWindowSettings settings, SummerGUIWindow parent = null, int frameRate = 30) : base(settings)
        {
			Interlocked.Increment(ref _instanceCount);

			this.Context.MakeCurrent();

			//NativeWindowSettings test = new NativeWindowSettings();
			//test.TransparentFramebuffer = false;			
						
			m_FrameRate = frameRate;
			this.VSync = VSyncMode.Adaptive;
			this.AutoIconify = false;
			this.CursorState = CursorState.Normal;

			LoadingErrorsQueue = new Queue<LoadingError>();
			ClipBoundStack = new ClipBoundStackClass(this);
			Context.SwapInterval = 1;	
			this.LogInformation ("OpenGL Version: {0}", GL.GetString(StringName.Version));
			DetectDPI ();
			//DetectDevice ();

			ChildWindows = new ClassicLinkedList<ChildFormWindow> ();
			Animator = new AnimationService (frameRate);

			MaxDirtyPaint = 10;
			MaxDirtyLayout = 10;

			ThreadSleepOnEmptyUpdateFrame = 1;
			ThreadSleepOnEmptyRenderFrame = 1;

			OriginalWidth = Width;
			OriginalHeight = Height;
			
			InitFonts ();
			InitCursors ();

			SetRenderingOptions();
			//SetupViewport ();
			BackColor = Theme.Colors.Base02;			
			GL.ClearColor(BackColor);
			
			Controls = new RootContainer (this);
			DpiScaling = new DpiScalingAutomat (this);			
			InvalidateMeter = new FramePerformanceMeter(5);

			this.ParentWindow = parent;
			if (ParentWindow != null)
				this.SetParent(ParentWindow);			
        }

		// Die Basisklasse NativeWindow erwartet die Settings im "base" Aufruf.
    	protected SummerGUIWindow (string caption, int width, int height, SummerGUIWindow parent = null, int frameRate = 30)
			// 1. Übergabe der konfigurierten Settings an den NativeWindow Basis-Konstruktor
			: this(new NativeWindowSettings()
			{
				Title = caption,
				ClientSize = new Vector2i(width, height),
				AutoLoadBindings = true,
				StartVisible = false,				
				Profile = ContextProfile.Core,
				NumberOfSamples = 4,
				TransparentFramebuffer = false,
			}, parent, frameRate) {}

		public unsafe void AddChildWindow(ChildFormWindow wnd)
		{
			if (!ChildWindows.Contains (wnd)) {
				ChildWindows.AddLast (wnd);
				this.Controls.Enabled = false;				

				if (this.WindowState == WindowState.Fullscreen)
					//this.BringToFront();
					GLFW.FocusWindow(this.WindowPtr);
					//this.IsVisible = true;					
            }
		}

		public unsafe void RemoveChildWindow(ChildFormWindow wnd)
		{
			Batcher.BindContext(this);
			
			try {				
				ChildWindows.Remove (wnd);
			} catch (Exception ex) {
				ex.LogError ();
			}
			if (ChildWindows.Count == 0)
            {
                this.Controls.Enabled = true;
			}
			if (this.WindowState == WindowState.Fullscreen)
				//this.BringToFront();
				GLFW.FocusWindow(this.WindowPtr);
        }

		public bool HasChildWindow
		{
			get{
				return ChildWindows != null && ChildWindows.Count > 0;
			}
		}

		protected ClassicLinkedList<ChildFormWindow> ChildWindows { get; private set; }
		public ChildFormWindow ActiveChildWindow 
		{ 
			get {				
				if (!HasChildWindow)
					return null;
				return ChildWindows.First;
			}
		}

		public unsafe bool ActivateChildWindow()
		{						
			var cw = ActiveChildWindow;
			if (cw == null)
				return false;
			
			//cw.BringToFront ();
			GLFW.FocusWindow(cw.WindowPtr);
			cw.Focus();
			return true;
		}		

		public int TitleBarHeight => Math.Max(0, Size.Y - ClientSize.Y);
		
		protected override void OnFramebufferResize(FramebufferResizeEventArgs e)
		{
			base.OnFramebufferResize(e);
			MakeCurrent();

			// Viewport IMMER zuerst			
			GL.Viewport(0, 0, e.Width, e.Height);			

			// Projection exakt gleich
			//Batcher.UpdateSize(e.Width, e.Height);			
			GUIRenderBatcher.Batcher.BindContext(this);

			Invalidate();
		}

		private void SetupViewport()
		{
		}		

		private Rectangle m_LastResizeBounds = Rectangle.Empty;

		protected override void OnResize(ResizeEventArgs e)
		{			
			base.OnResize(e);			
			this.MakeCurrent();			

			if (Bounds != m_LastResizeBounds) {				
				m_LastResizeBounds = Bounds;
				//SetupViewport ();
				iDirtyLayout = MaxDirtyLayout * 2;	// be safe and allow 10 layouts
				Invalidate ();

				// *** Test
				// später aber erst, sobald das richtig funktioniert..
				// DetectDPI ();
				// DetectDevice ();

				if (WindowState == WindowState.Normal)
					DefaultSize = Size;				
			}			
		}

        protected override void OnMove(WindowPositionEventArgs e)
        {
            base.OnMove(e);
			if (WindowState == WindowState.Normal)
				DefaultLocation = Location;
        }

		public void InitializeWidgets()
		{
			// ToDo:
			// implement some more interfaces, e.g. Device aware widgets,
			// connect observers, wire menus, hide unused menus, ..

			Controls.EnumerateWidgets ().ForEach (InitializeWidget);
		}

		public virtual void InitializeWidget(Widget widget)
		{
			if (widget == null) {
				this.LogWarning ("InitializeWidget: Widget is null. There shouldn't be any null references in Container's children.");
				return;
			}

			try {
				widget.Initialize();
			} catch (Exception ex) {
				ex.LogError ("Initialization failed for widget: {0}, Type: {1}, Error: {2}", widget.Name, widget.GetType().Name, ex.Message);
			}
		}		

		public event EventHandler<EventArgs> Load;
		protected virtual void OnLoad(EventArgs e)
		{			
			if (_instanceCount == 1)
			{
				this.Batcher.Init(this.Size.X, this.Size.Y);
				this.Batcher.SetGamma(1.2f);
			}

			IsCreated = true;
			IsVisible = true;

			PerformanceTimer.Time (() => {
				InitializeWidgets ();
			}, 1, "Initialization of all Widgets");

			if (MainMenu != null) {
				MenuManager = new MenuManager (Controls);
				MenuManager.InitMenu (MainMenu);
			}

			if (this.WindowBorder == WindowBorder.Resizable)
				this.MinimumSize = new Vector2i(320, 240);

			try {
				OnLoadSettings ();
			} catch (Exception ex) {
				ex.LogError ();
			}

			try {
				if (Load != null)
					Load (this, e);	
			} catch (Exception ex) {
				ex.LogError ();
			}
				
			if (ScaleFactor > 1)
				ScaleGUI(ScaleFactor);			

			// Wichtig für Core-Profile
			GL.Enable(EnableCap.Blend);
			GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
			GL.Disable(EnableCap.DepthTest);

			// activate immediate swapping for flicker-free painting.
			Context.SwapInterval = 0;
		}

		protected Vector2i DefaultLocation { get; set; }
		protected Vector2i DefaultSize { get; set; }

		public virtual void OnLoadSettings()
		{			
			ConfigurationService.Instance.ConfigFile.Do (cfg => {
				if (!String.IsNullOrEmpty (Name) && this.WindowBorder == WindowBorder.Resizable) {				
					WindowState winState = (WindowState)cfg.GetSetting (this.Name, "WindowState", this.WindowState).SafeString ().ToEnum (this.WindowState);
					if (winState != WindowState.Minimized) {						
						int left = Math.Max(0, cfg.GetSetting (this.Name, "Left", this.Location.X).SafeInt ());
						int top = Math.Max(0, cfg.GetSetting (this.Name, "Top", this.Location.Y).SafeInt ());
						DefaultLocation = new Vector2i (left, top);
						int width = Math.Max(10, cfg.GetSetting (this.Name, "Width", this.Width).SafeInt ());
						int height = Math.Max(10, cfg.GetSetting (this.Name, "Height", this.Height).SafeInt ());
						DefaultSize = new Vector2i (width, height);

						if (winState != WindowState.Normal) {
							this.WindowState = winState;
							//this.Context.Update (this.WindowInfo);
						} else {
							this.Location = DefaultLocation;
							this.Size = DefaultSize;
						}
					}
				}

				ScaleFactor = Math.Max (1, cfg.GetSetting (this.Name, "ScaleFactor", ScaleFactor).SafeFloat ());
				Device = cfg.GetSetting (this.Name, "Device", Device.ToString ()).SafeString ().ToEnum (Devices.Desktop);

				// Load menu settings
				if (MainMenu != null) {
					Dictionary<string, string> dict = new Dictionary<string, string> ();
					try {
						cfg.ReadSection ("MenuItems").ForEach (kv => dict.Add (kv.Key, kv.Value.SafeString ()));
					} catch (Exception ex) {
						ex.LogError ();
					}
					MainMenu.Items ().ForEach (item => {
						string val;
						if (dict.TryGetValue (item.Name, out val)) {
							item.ClickCount = val.FindBlock ("Clicks:", ";").SafeInt ();
						}
					});
				}
			});
		}
			
		public virtual void OnSaveSettings()
		{
			if (!String.IsNullOrEmpty (Name) && (this.WindowBorder == WindowBorder.Resizable || this.WindowState == WindowState.Fullscreen)) {				
				ConfigurationService.Instance.ConfigFile.Do (cfg => {
					if (this.WindowState != WindowState.Minimized) {
						cfg.SetSetting (this.Name, "WindowState", this.WindowState.ToString ());
						if (this.WindowState == WindowState.Normal) {							
							cfg.SetSetting (this.Name, "Width", DefaultSize.X);
							cfg.SetSetting (this.Name, "Height", DefaultSize.Y);
							cfg.SetSetting (this.Name, "Left", DefaultLocation.X);
							cfg.SetSetting (this.Name, "Top", DefaultLocation.Y);
						}
					}						
					cfg.SetSetting (this.Name, "ScaleFactor", ScaleFactor.ToString ("n3"));
					cfg.SetSetting (this.Name, "Device", Device.ToString ());

					cfg.ClearSection ("MenuItems");
					if (MainMenu != null) {
						MainMenu.Items ().ForEach (item => 
						cfg.SetSetting ("MenuItems", item.Name, String.Format ("Visible:{0};Enabled:{1};Checked:{2};Clicks:{3};Collapsed:{4}", 
							item.Visible.ToLowerString (), item.Enabled.ToLowerString (), item.Checked.ToLowerString (),
							item.ClickCount, item.Collapsed.ToLowerString ())));
					}
				});
			}				
		}

		void InitFonts()
		{									
			OnInitFonts ();			
		}

		// Preload your fonts here
		protected virtual void OnInitFonts()
		{	
			//FontManager.InitAllFonts ();
			FontManager.Manager.InitDefaultFont();
		}

		void InitCursors()
		{			
			OnInitCursors ();
			OnLoadWindowIcon ();
		}

		protected virtual void OnLoadWindowIcon()
		{			
			string path = "Assets/images/SummerGUI64.png".FixedExpandedPath();			
			WindowResourceManager.Manager.LoadWindowIcon(this, path);
		}

		// Preload your cursors here
		protected virtual void OnInitCursors()
		{				
			WindowResourceManager.Manager.LoadCursorFromFile ("Assets/Cursors/VSplit.png", Cursors.VSplit);
			WindowResourceManager.Manager.LoadCursorFromFile ("Assets/Cursors/HSplit.png", Cursors.HSplit);
			WindowResourceManager.Manager.LoadCursorFromFile ("Assets/Cursors/Text.png", Cursors.Text);
			WindowResourceManager.Manager.LoadCursorFromFile ("Assets/Cursors/Wait.png", Cursors.Wait);
		}
			
		private bool m_SettingCursor;
		private string m_GlobalCursor;
		private string m_CurrentCursorName = Cursors.Default.ToString();        
		public void SetCursor(Cursors cursor)
		{
            SetCustomCursor(cursor.ToString());
		}

		public void SetCustomCursor(string name)
		{
			if (m_GlobalCursor != null) {
				m_CurrentCursorName = name;
			} else if (!m_SettingCursor && m_CurrentCursorName != name) {
				m_SettingCursor = true;
				WindowResourceManager.Manager.SetCursor (this, name);
				m_CurrentCursorName = name;
				m_SettingCursor = false;
				Invalidate ();
			}
		}

		public void SetGlobalCursor(string name)
		{
            if (!m_SettingCursor && m_GlobalCursor != name)
            {
				m_SettingCursor = true;
				WindowResourceManager.Manager.SetCursor (this, name);
				m_SettingCursor = false;
                m_GlobalCursor = name;
				Invalidate ();
			}            
		}

		public void ShowWaitCursor()
		{
			SetGlobalCursor (Cursors.Wait.ToString());
		}

		public void RestoreCursor()
		{
            if (!m_SettingCursor && m_GlobalCursor != null)
            {                
				m_SettingCursor = true;                
                WindowResourceManager.Manager.SetCursor(this, m_CurrentCursorName);                
                m_GlobalCursor = null;
				m_SettingCursor = false;                
                Invalidate();
			}            
		}

		// Slow-down rendering when the user is inactive for a while
		private void RaiseSleepTime()
		{
			try {
				if (RaiseSleepTimeCounter > 0) {
					RaiseSleepTimeCounter--;
				} else if (ThreadSleepOnEmptyUpdateFrame < 10) {				
					ThreadSleepOnEmptyUpdateFrame++;
					ThreadSleepOnEmptyRenderFrame++;
					RaiseSleepTimeCounter = 60;
				}	
			} catch {}
		}

		// Raise it up immediately when there is any user action
		private int RaiseSleepTimeCounter = 60;
		private void LowerSleepTime()
		{
			try {				
				ThreadSleepOnEmptyUpdateFrame = 1;
				ThreadSleepOnEmptyRenderFrame = 1;
				RaiseSleepTimeCounter = 60;
			} catch {}
		}						

		protected override void OnKeyDown (KeyboardKeyEventArgs e)
		{
			//this.LogDebug ("OnKeyDown {0}", e.Key);
			//base.OnKeyDown (e);
			if (HasChildWindow)
				return;			

			switch (e.Key)
			{
				case Keys.LeftControl:
					ModifierKeys.OnLeftControlPressed();
					break;

				case Keys.RightControl:
					ModifierKeys.OnRightControlPressed();
					break;

				case Keys.LeftShift:
					ModifierKeys.OnLeftShiftPressed();
					break;

				case Keys.RightShift:
					ModifierKeys.OnRightShiftPressed();
					break;

				case Keys.CapsLock:
					ModifierKeys.OnCapsLockPressed();
					break;

				case Keys.LeftAlt:
					ModifierKeys.OnLeftAltPressed();
					LowerSleepTime ();
					Invalidate ();
					return;

				case Keys.RightAlt:
					ModifierKeys.OnRightAltPressed();
					LowerSleepTime ();
					Invalidate ();
					return;
			}
						
			LowerSleepTime ();
			if (!Controls.OnKeyDown (e))
			{
				OnUnhandledKeyDown(e);
			}
			//this.LogDebug ("AltPressed: {0}", ModifierKeys.AltPressed);
		}

		protected virtual void OnUnhandledKeyDown(KeyboardKeyEventArgs e)
		{			
		}

		protected override void OnKeyUp (KeyboardKeyEventArgs e)
		{			
			//this.LogDebug ("OnKeyUp {0}", e.Key);
			//base.OnKeyUp (e);
			if (HasChildWindow)
				return;			

			switch (e.Key)
			{
				case Keys.LeftControl:
					ModifierKeys.OnLeftControlReleased();
					break;

				case Keys.RightControl:
					ModifierKeys.OnRightControlReleased();
					break;

				case Keys.LeftShift:
					ModifierKeys.OnLeftShiftReleased();
					break;

				case Keys.RightShift:
					ModifierKeys.OnRightShiftReleased();
					break;

				case Keys.CapsLock:
					//ModifierKeys.OnCapsLockReleased();
					break;

				case Keys.LeftAlt:
					ModifierKeys.OnLeftAltReleased();
					LowerSleepTime ();
					Invalidate ();
					return;

				case Keys.RightAlt:
					ModifierKeys.OnRightAltReleased();
					LowerSleepTime ();
					Invalidate ();
					return;
			}			
			
			//this.LogDebug ("AltPressed: {0}", ModifierKeys.AltPressed);
			Controls.OnKeyUp(e);
		}

        protected override void OnTextInput(TextInputEventArgs e)
        {
            //base.OnTextInput(e);
			if (HasChildWindow)
				return;
			
			if (e.AsString == null || e.AsString.Length == 0)
				return;

			char c = e.AsString[0];
			KeyPressEventArgs args = new KeyPressEventArgs(c);
			Controls.OnKeyPress (args);
        }

		// finding out that this is a BUG took me some hours..
		// After each mouse down, a OnMouseWheel is fired, where the wheel isn't touched at all
		private bool m_MouseDownFlag = false;		

        protected override void OnMouseDown(OpenTK.Windowing.Common.MouseButtonEventArgs e)
        {
			if (HasChildWindow)
				return;
			//this.LogDebug ("OnMouseDown: {0}, Focused={1}", Name, Focused);
			m_MouseDownFlag = true;
			base.OnMouseDown (e);
			
			Vector2 pos = this.MouseState.Position;			
			SummerGUI.MouseButtonEventArgs args = new MouseButtonEventArgs((int)pos.X, (int)pos.Y, e.Button);
			Controls.OnMouseDown (args);
        }

        protected override void OnMouseUp(OpenTK.Windowing.Common.MouseButtonEventArgs e)
        {
            if (HasChildWindow)
				return;			
			base.OnMouseUp (e);
			
			Vector2 pos = this.MouseState.Position;
			SummerGUI.MouseButtonEventArgs args = new MouseButtonEventArgs((int)pos.X, (int)pos.Y, e.Button);

			Controls.OnMouseUp (args);
			m_MouseDownFlag = false;
        }		

		protected override void OnMouseMove (MouseMoveEventArgs e)
		{
			if (HasChildWindow)
				return;
			LowerSleepTime ();
			base.OnMouseMove (e);
						
			Controls.OnMouseMove (e);
		}

		protected override void OnMouseWheel (OpenTK.Windowing.Common.MouseWheelEventArgs e)
		{						
			if (HasChildWindow || m_MouseDownFlag || (e.OffsetX == 0 && e.OffsetY == 0))
				return;
			//this.LogDebug ("OnMouseWheel: {0}, Focused={1}", Name, Focused);									

			LowerSleepTime ();
			base.OnMouseWheel (e);

			Vector2 pos = this.MouseState.Position;			
			var args = new SummerGUI.MouseWheelEventArgs((int)pos.X, (int)pos.Y, e.Offset, e.OffsetX, e.OffsetY);			
			Controls.OnMouseWheel (args);
		}

		public T AddChild<T>(T c) where T : Widget
		{
			return Controls.AddChild (c);
		}			

		public Color BackColor { get; set; }	

		public int MaxDirtyPaint { get; set; }
		public int MaxDirtyLayout { get; set; }

		private int iDirtyPaint = 10;
		private int iDirtyLayout = 0;
			
		public void Invalidate()
		{
			if (IsCreated)				
				Invalidate (MaxDirtyLayout);
		}

		private bool _isCurrentlyAnimating = false;

		readonly FramePerformanceMeter InvalidateMeter;

		/// <summary>
		/// this can be called from multiple threads
		/// </summary>
		public void Invalidate(int frames)
		{				
			if (IsExiting)
				return;

			long mean_milliseconds = InvalidateMeter.Pulse();
			_isCurrentlyAnimating = mean_milliseconds < 200;			

			if (frames <= 0)
				frames = MaxDirtyPaint;

			if (iDirtyLayout < MaxDirtyLayout)
				iDirtyLayout += 2;
			iDirtyPaint = Math.Max(1, Math.Min(iDirtyPaint + frames, MaxDirtyPaint));

			// WECKE den Render-Thread auf, falls er schläft (z.B. in ProcessEvents(0.01))
			GLFW.PostEmptyEvent(); 
			
			// Setze den Active-State, um den Timeout-Wert auf 0.0 zu setzen (Polling)
			//this.SetActivationState(true);
		}

		protected virtual void ClearBackground()
		{
			RectangleF rect = new RectangleF(0, 0, this.Width, this.Height);
			Brush br = new SolidBrush(BackColor);
			this.FillRectangle(br, rect);
		}

		protected virtual void SetRenderingOptions()
		{
			GL.MatrixMode (MatrixMode.Modelview);
			GL.LoadIdentity ();			

			// WICHTIG: Depth Test aus für 2D GUI
			GL.Disable(EnableCap.DepthTest);
			GL.DepthMask(false);    // NICHT in den Depth Buffer schreiben

			// WICHTIG: Blending an, damit Text (Alpha) funktioniert
			GL.Enable(EnableCap.Blend);
			GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
			GL.Clear (ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);			

			GL.Enable(EnableCap.ScissorTest);			
		}
					
		private void DoPaint(RectangleF bounds)
		{				
			int clipCount = ClipBoundStack.Count;
			if (clipCount > 0) {
				ClipBoundStack.Clear ();
				this.LogWarning ("ClipBoundStack was not empty, Count: {0}", clipCount);
			}

			ClearBackground();

			try {
				Batcher.ResetCounters();
				OnPaint (bounds);				
				Batcher.Flush();
				//Debug.WriteLine($"DrawCount: {Batcher.DrawCount}, FlushCount: {Batcher.FlushCount}, ClipCount: {Batcher.ClipCount}");

			} catch (Exception ex) {
				ex.LogError ("DoPaint");
			} finally {
				OnAfterPaint ();
			}
		}

		private void Test_Fonts()
		{
			ClipBoundStack.Clear();
			this.FillRectangle(new SolidBrush(Theme.Colors.Base02), new RectangleF(0, 0, this.Width, this.Height));			

			float xStart = 100;
			float yStart = 100;
			float width = 200;
			float height = 200;
			float distance = 25;
			string text = "Hallo Welt";
			Pen pen = new Pen(Color.Yellow);
			Brush brush = new SolidBrush(Color.White);
			FontFormat format = new FontFormat(Alignment.Center, Alignment.Center, FontFormatFlags.None);
			format = FontFormat.DefaultIconFontFormatCenter;

			string iconText = ((char)FontAwesomeIcons.fa_amazon).ToString() + ((char)FontAwesomeIcons.fa_align_justify).ToString() + ((char)FontAwesomeIcons.fa_android).ToString();			

			foreach (var font in FontManager.Manager.Fonts.Values)
			{
				if (font == null)
					continue;

				if (font.Name == "FontAwesome")
					text = iconText;
				else
					text = font.Name + " ff ffi s&z";

				SizeF sz = font.Measure(text);
				width = sz.Width + 50;		
				height = font.TextBoxHeight + 150;

				RectangleF rect = new RectangleF(xStart, yStart, width, height);
				this.DrawRectangle(pen, rect);

				this.DrawString(text, font, brush, rect, format);
				
				yStart += height + distance;
				if (yStart + height > this.Height)
				{
					yStart = 100;			
					xStart += width + distance;
				}
			}
		}

		protected virtual void OnPaint(RectangleF bounds)
		{				
			this.Controls.Update (this);
			return;			
		}

		protected virtual void OnAfterPaint()
		{			
			try {				
				this.SwapBuffers();	
			} catch (Exception ex) {
				ex.LogError ();
			}            
		}
			
		public virtual void Focus(bool setFocus = true)
		{
			if (IsExiting)
				return;

			Controls.ResetLastMouseDownWidget ();
			LowerSleepTime ();
			m_MouseDownFlag = false;
			OnProcessEvents ();
		}

        protected override void OnFocusedChanged(FocusedChangedEventArgs e)
        {
			if (IsExiting)
				return;			

            base.OnFocusedChanged(e);

			if (IsFocused && ActivateChildWindow ())
				return;

			Invalidate ();
        }        
				
		public int ThreadSleepOnEmptyUpdateFrame { get; set; }
		public int ThreadSleepOnEmptyRenderFrame { get; set; }

		/// <summary>
		/// this is called every frame, put game logic here
		/// </summary>
		/// <param name="e">Contains information necessary for frame updating.</param>
		protected virtual void OnUpdateFrame(double elapsedSeconds)
		{				
			RaiseSleepTime ();			
			if (iDirtyPaint <= 0) {
				Thread.Sleep (ThreadSleepOnEmptyUpdateFrame);
				return;
			}
				
			if (iDirtyLayout > 0) {				
				Rectangle rec = new Rectangle(0, 0, ClientRectangle.Size.X, ClientRectangle.Size.Y);
				this.Controls.OnLayout (this, rec);
				iDirtyLayout--;
			}			
		}
			
		protected virtual void OnRenderFrame(double elapsedSeconds)
		{							
			// Vor dem Zeichnen: Batcher auf dieses Fenster "eichen"
    		GUIRenderBatcher.Batcher.BindContext(this);

			if (Animator.IsStarted) {
				Animator.Animate ();
				Invalidate (1);
			}			
			else if (iDirtyPaint <= 0) {
				Thread.Sleep (ThreadSleepOnEmptyRenderFrame);
				return;
			}
						
			DoPaint ((Rectangle)ClientRectangle);			
			iDirtyPaint--;
		}

		// *** Overlay Widgets

		public void RegisterOverlay(Widget widget)
		{
			Controls.RegisterOverlay (widget);
		}

		public void UnregisterOverlay(Widget widget)
		{
			Controls.UnregisterOverlay (widget);
		}

		public void ClearOverlays()
		{
			Controls.ClearOverlays ();
		}


		// *** Lifecycle

		protected virtual void OnUnload(EventArgs e)
		{									
			if (ChildWindows != null && ChildWindows.Count > 0) {				
				foreach (ChildFormWindow child in ChildWindows) {
					if (child != null && !child.IsExiting) {
						try {
							child.Close ();
							child.Dispose ();							
						} catch (Exception ex) {
							ex.LogError ();
						}
					}
				}
			}
		}

		protected virtual void ShutdownFramework()
		{
			FontManager.Manager.Dispose();
        	GUIRenderBatcher.Batcher.Dispose();
		}
			
		public AnimationService Animator { get; private set; }

		// *** different kinds of Message-Boxes ***

		// ToDo: The overlay calls don't block but return immediately
		// without having the DialogResult set.

		public void ShowError(Exception ex)
		{
			if (ex == null)
				return;
			while (ex.InnerException != null)
				ex = ex.InnerException;

			if (!IsInitialized)
				LoadingErrorsQueue.Enqueue (new LoadingError(MessageBoxTypes.Error, ex.Message));
			else
				ShowError (ex.Message);
		}

		public void ShowError(string message)
		{
			if (!IsInitialized)
				LoadingErrorsQueue.Enqueue (new LoadingError(MessageBoxTypes.Error, message));
			else if (Device == Devices.Desktop)
				MessageBoxWindow.ShowError (message, this);
			else
				MessageBoxOverlay.ShowError (message, this);				
		}

		public void ShowWarning(string message)
		{
			if (!IsInitialized)
				LoadingErrorsQueue.Enqueue (new LoadingError(MessageBoxTypes.Warning, message));
			else if (Device == Devices.Desktop)
				MessageBoxWindow.ShowWarning (message, this);
			else
				MessageBoxOverlay.ShowWarning (message, this);
		}

		public void ShowInfo(string message)
		{
			if (!IsInitialized)
				LoadingErrorsQueue.Enqueue (new LoadingError(MessageBoxTypes.Info, message));
			else if (Device == Devices.Desktop)
				MessageBoxWindow.ShowInfo (message, this);
			else
				MessageBoxOverlay.ShowInfo (message, this);
		}

		public void ShowSuccess(string message)
		{
			if (!IsInitialized)
				LoadingErrorsQueue.Enqueue (new LoadingError(MessageBoxTypes.Success, message));
			else if (Device == Devices.Desktop)
				MessageBoxWindow.ShowSuccess (message, this);
			else
				MessageBoxOverlay.ShowSuccess (message, this);
		}

		public DialogResults ShowQuestion(string message)
		{
			if (!IsInitialized) {
				LoadingErrorsQueue.Enqueue (new LoadingError (MessageBoxTypes.Question, message));
				return DialogResults.Cancel;
			}
			else if (Device == Devices.Desktop)
				return MessageBoxWindow.ShowQuestion (message, this);
			else
				return MessageBoxOverlay.ShowQuestion (message, this);
		}			

		/***
		public void Invoke(Action action)
		{
			ReflectionUtils.InvokeMethod(SummerGUIWindow, 
		}
		***/

		public void ScaleGUI(float scaleFactor)
		{
			if (scaleFactor < float.Epsilon)
				DetectDPI ();
			else
				ScaleFactor = scaleFactor;
			FontManager.Manager.ReScaleFonts (this.ScaleFactor);
			ScaleGUI ();
		}
			
		protected virtual void ScaleGUI()
		{
			try {
				DpiScaling.ScaleGUI ();
			} catch (Exception ex) {
				ex.LogError ();
			} finally {
				Invalidate ();
				OnScalingChanged ();
			}
		}

		public event EventHandler<EventArgs> ScalingChanged;
		public virtual void OnScalingChanged()
		{
			if (ScalingChanged != null)
				ScalingChanged (this, EventArgs.Empty);
		}

		private WindowState BeforeFullscreenWindowState;
		public unsafe void ToggleFullScreen()
		{
			if (IsFullScreen) {
				if (BeforeFullscreenWindowState == WindowState.Minimized || BeforeFullscreenWindowState == WindowState.Fullscreen)
					BeforeFullscreenWindowState = WindowState.Normal;				

				WindowState = BeforeFullscreenWindowState;			
				
				if (BeforeFullscreenWindowState == WindowState.Normal)
				{											
					Vector2i newSize = new Vector2i(DefaultSize.X, DefaultSize.Y);
					this.Size = newSize;
					this.Location = DefaultLocation;
				}

				//this.BringToFront ();	// Window does not always have focus on Linux after it
				GLFW.FocusWindow(this.WindowPtr);
			} else {
				BeforeFullscreenWindowState = WindowState;
				WindowState = WindowState.Fullscreen;
			}
		}

		public bool IsFullScreen
		{
			get{
				return WindowState == WindowState.Fullscreen;
			}
		}

		public override void Dispose()
		{
			try
			{
			    Dispose(true);
			}
			finally
			{
				GC.SuppressFinalize(this);
				base.Dispose();
			}			
		}

		public bool IsDisposed {get; private set;}

		protected override void Dispose(bool manual)
		{			
			if (IsDisposed)
				return;			

			if (manual) {
				try {
					// first of all, stop all running animations					
					Animator?.Dispose ();
					Animator = null;
					Controls?.Dispose ();
					Controls = null;
					ClipBoundStack?.Clear();
					ClipBoundStack = null;
					MainMenu = null;					
				} catch (Exception ex) {
					ex.LogError ();
				}					
			}

			IsDisposed = true;

			base.Dispose (manual);
		}


		// IGameWindow Implementation

		const double MaxFrequency = 500.0; // Frequency cap for Update/RenderFrame events

		readonly Stopwatch watch = new Stopwatch();

		//#pragma warning disable 612,618
		//readonly IJoystickDriver LegacyJoystick =
		//	Factory.Default.CreateLegacyJoystickDriver();
		//#pragma warning restore 612,618
		

		public IRootController Controller { get; protected set; }		

		double update_period, render_period;
		double target_update_period, target_render_period;

		double update_time; // length of last UpdateFrame event
		double render_time; // length of last RenderFrame event

		double update_timestamp; // timestamp of last UpdateFrame event
		double render_timestamp; // timestamp of last RenderFrame event

		double update_epsilon; // quantization error for UpdateFrame events

		bool is_running_slowly; // true, when UpdatePeriod cannot reach TargetUpdatePeriod

		public virtual void Exit()
		{
			Close();
		}
		
		public void SwapBuffers()
		{
			try {				
				this.Context.SwapBuffers();	
			} catch (Exception ex) {
				ex.LogWarning ();
			}
		}

		private bool _isAlreadyClosing = false; // Instanz-Variable		

		protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
		{
			if (_isAlreadyClosing)
			{				
				e.Cancel = true;
				return;
			}

			_isAlreadyClosing = true;

			base.OnClosing(e);
			if (!e.Cancel)
			{
				try {
					OnSaveSettings ();
					ConfigFile cfg = ConfigurationService.Instance.ConfigFile;
					if (cfg != null) {
						cfg.Save ();
					}	
				} catch (Exception ex) {
					ex.LogError ();
				}

				OnUnload (EventArgs.Empty);				
				
				Interlocked.Decrement(ref _instanceCount);
				if (_instanceCount <= 0)
				{					
					this.IsVisible = false;
					if (ParentWindow == null && !HasChildWindow)
					{
						Debug.WriteLine("Shutting Down Framework ...");
						this.ShutdownFramework();
					}
				}			
			}
		}

		/***
		public VSyncMode VSync
		{
			get
			{				
				GraphicsContext.Assert();
				if (Context.SwapInterval < 0)
				{
					return VSyncMode.Adaptive;
				}
				else if (Context.SwapInterval == 0)
				{
					return VSyncMode.Off;
				}
				else
				{
					return VSyncMode.On;
				}
			}
			set
			{				
				GraphicsContext.Assert();
				switch (value)
				{
				case VSyncMode.On:
					Context.SwapInterval = 1;
					break;

				case VSyncMode.Off:
					Context.SwapInterval = 0;
					break;

				case VSyncMode.Adaptive:
					Context.SwapInterval = -1;
					break;
				}
			}
		}
		***/


		/// <summary>
		/// Gets a double representing the actual frequency of RenderFrame events, in hertz (i.e. fps or frames per second).
		/// </summary>
		public double RenderFrequency
		{
			get
			{				
				if (render_period == 0.0)
					return 1.0;
				return 1.0 / render_period;
			}
		}

		/// <summary>
		/// Gets a double representing the period of RenderFrame events, in seconds.
		/// </summary>
		public double RenderPeriod
		{
			get
			{				
				return render_period;
			}
		}

		/// <summary>
		/// Gets a double representing the time spent in the RenderFrame function, in seconds.
		/// </summary>
		public double RenderTime
		{
			get
			{				
				return render_time;
			}
			protected set
			{			
				render_time = value;
			}
		}


		/// <summary>
		/// Gets or sets a double representing the target render frequency, in hertz.
		/// </summary>
		/// <remarks>
		/// <para>A value of 0.0 indicates that RenderFrame events are generated at the maximum possible frequency (i.e. only limited by the hardware's capabilities).</para>
		/// <para>Values lower than 1.0Hz are clamped to 0.0. Values higher than 500.0Hz are clamped to 200.0Hz.</para>
		/// </remarks>
		public double TargetRenderFrequency
		{
			get
			{				
				if (TargetRenderPeriod < 0.001)
					return 0.0;
				return 1.0 / TargetRenderPeriod;
			}
			set
			{			
				if (value < 1.0) {
					TargetRenderPeriod = 0.0;
				} else if (value <= MaxFrequency) {
					TargetRenderPeriod = 1.0 / value;
				} else {
					this.LogWarning ("Target render frequency clamped to {0}Hz.", MaxFrequency);
				}
			}
		}


		/// <summary>
		/// Gets or sets a double representing the target render period, in seconds.
		/// </summary>
		/// <remarks>
		/// <para>A value of 0.0 indicates that RenderFrame events are generated at the maximum possible frequency (i.e. only limited by the hardware's capabilities).</para>
		/// <para>Values lower than 0.002 seconds (500Hz) are clamped to 0.0. Values higher than 1.0 seconds (1Hz) are clamped to 1.0.</para>
		/// </remarks>
		public double TargetRenderPeriod
		{
			get
			{				
				return target_render_period;
			}
			set
			{			
				if (value <= 1 / MaxFrequency) {
					target_render_period = 0.0;
				} else if (value <= 1.0) {
					target_render_period = value;
				} else {
					this.LogWarning ("Target render period clamped to 1.0 seconds.");
				}
			}
		}

		/// <summary>
		/// Gets or sets a double representing the target update frequency, in hertz.
		/// </summary>
		/// <remarks>
		/// <para>A value of 0.0 indicates that UpdateFrame events are generated at the maximum possible frequency (i.e. only limited by the hardware's capabilities).</para>
		/// <para>Values lower than 1.0Hz are clamped to 0.0. Values higher than 500.0Hz are clamped to 500.0Hz.</para>
		/// </remarks>
		public double TargetUpdateFrequency
		{
			get
			{				
				if (TargetUpdatePeriod == 0.0)
					return 0.0;
				return 1.0 / TargetUpdatePeriod;
			}
			set
			{			
				if (value < 1.0)
				{
					TargetUpdatePeriod = 0.0;
				}
				else if (value <= MaxFrequency)
				{
					TargetUpdatePeriod = 1.0 / value;
				}
				else 
					this.LogDebug ("Target render frequency clamped to {0}Hz.", MaxFrequency);
			}
		}


		/// <summary>
		/// Gets or sets a double representing the target update period, in seconds.
		/// </summary>
		/// <remarks>
		/// <para>A value of 0.0 indicates that UpdateFrame events are generated at the maximum possible frequency (i.e. only limited by the hardware's capabilities).</para>
		/// <para>Values lower than 0.002 seconds (500Hz) are clamped to 0.0. Values higher than 1.0 seconds (1Hz) are clamped to 1.0.</para>
		/// </remarks>
		public double TargetUpdatePeriod
		{
			get
			{				
				return target_update_period;
			}
			set
			{				
				if (value <= 1 / MaxFrequency)
				{
					target_update_period = 0.0;
				}
				else if (value <= 1.0)
				{
					target_update_period = value;
				}
				else 
					this.LogDebug ("Target update period clamped to 1.0 seconds.");
			}
		}

		/// <summary>
		/// Gets a double representing the frequency of UpdateFrame events, in hertz.
		/// </summary>
		public double UpdateFrequency
		{
			get
			{				
				//if (update_period == 0.0)
				if (update_period < 0.001)
					return 1.0;
				return 1.0 / update_period;
			}
		}

		/// <summary>
		/// Gets a double representing the period of UpdateFrame events, in seconds.
		/// </summary>
		public double UpdatePeriod
		{
			get
			{				
				return update_period;
			}
		}

		/// <summary>
		/// Gets a double representing the time spent in the UpdateFrame function, in seconds.
		/// </summary>
		public double UpdateTime
		{
			get
			{				
				return update_time;
			}
		}		

		/// <summary>
		/// This is the best place to initialize other components
		/// To open databases and so on
		/// It will happen after OnLoad
		/// </summary>
		public virtual void OnApplicationRunning()
		{			
		}

		public bool IsInitialized { get; private set; }
		Queue<LoadingError> LoadingErrorsQueue;
		void ShowLoadingErros()
		{	
			// The default DotNet Queue is a blocking queue ??
			if (LoadingErrorsQueue.Count > 0) {				
				foreach (var msg in LoadingErrorsQueue.ToList()) {
					Task.Delay (500).ContinueWith ((t) => {
						MessageBoxOverlay.Show (msg.Message, msg.Context, MessageBoxButtons.OK, this);					
					});
				}
			}
		}

		public void Run() 
		{
			//float rate = Math.Min(60, Math.Max(30, DisplayDevice.Default.RefreshRate));			
			this.Run (m_FrameRate);			
		}

		public void Run(double frameRate)
		{			
			this.Run (frameRate, frameRate);
			// Run(updateRate, 0.0);
		}

		protected double OriginalUpdateFrequency  { get; private set; }
		protected double OriginalRenderFrequency { get; private set; }

		public void Run(double updates_per_second, double renderframes_per_second)
		{
			LayoutFrameRate = (float)updates_per_second;
			PaintFrameRate = (float)renderframes_per_second;
			Animator.FrameRate = renderframes_per_second;
			//base.Run (updateRate, renderRate);
			// ToDo:
			

			try
			{
				if (updates_per_second < 0.0 || updates_per_second > 200.0)
					throw new ArgumentOutOfRangeException("updates_per_second", updates_per_second,
						"Parameter should be inside the range [0.0, 200.0]");
				if (renderframes_per_second < 0.0 || renderframes_per_second > 200.0)
					throw new ArgumentOutOfRangeException("frames_per_second", renderframes_per_second,
						"Parameter should be inside the range [0.0, 200.0]");

				TargetUpdateFrequency = updates_per_second;
				TargetRenderFrequency = renderframes_per_second;

				OriginalUpdateFrequency = updates_per_second;
				OriginalRenderFrequency = renderframes_per_second;

				//Visible = true;   // Make sure the GameWindow is visible.
				//OnResize(EventArgs.Empty);

				OnLoad(EventArgs.Empty);
				Vector2i currentSize = new Vector2i(this.Width, this.Height);
				ResizeEventArgs args = new ResizeEventArgs(currentSize);
				OnResize(args);		// Wichtig für 1. Laden ohne Settings

				OnApplicationRunning ();
				IsInitialized = true;
				ShowLoadingErros();

				//this.LogVerbose("Entering main loop.");
				watch.Start();
				while (true)
				{
					OnProcessEvents();
					if (Exists && !IsExiting)
						OnDispatchUpdateAndRenderFrame();
					else
						return;					
				}
			}
			finally
			{				
				if (Exists)
				{
					// TODO: Should similar behaviour be retained, possibly on native window level?
					//while (this.Exists)
					//    ProcessEvents(false);
				}
			}
		}

		public virtual void OnProcessEvents()
		{			
			double timeout;
			
			if (_isCurrentlyAnimating)
				timeout = 0.0; // Polling-Modus für maximale Geschwindigkeit
			else
				timeout = 0.1;
			
			// ProcessEvents aufrufen
			ProcessEvents(timeout);
		}

		double ClampElapsed(double elapsed)
		{
			return MathHelper.Clamp(elapsed, 0.0, 1.0);
		}

		public virtual void OnDispatchUpdateAndRenderFrame()
        {
            DispatchUpdateAndRenderFrame();
        }

		protected virtual void DispatchUpdateAndRenderFrame()
		{
			this.MakeCurrent();

			int is_running_slowly_retries = 4;
			double timestamp = watch.Elapsed.TotalSeconds;
			double elapsed = 0;

			elapsed = ClampElapsed(timestamp - update_timestamp);
			while (elapsed > 0 && elapsed + update_epsilon >= TargetUpdatePeriod)
			{
				RaiseUpdateFrame(elapsed, ref timestamp);

				// Calculate difference (positive or negative) between
				// actual elapsed time and target elapsed time. We must
				// compensate for this difference.
				update_epsilon += elapsed - TargetUpdatePeriod;

				// Prepare for next loop
				elapsed = ClampElapsed(timestamp - update_timestamp);

				if (TargetUpdatePeriod <= Double.Epsilon)
				{
					// According to the TargetUpdatePeriod documentation,
					// a TargetUpdatePeriod of zero means we will raise
					// UpdateFrame events as fast as possible (one event
					// per ProcessEvents() call)
					break;
				}

				is_running_slowly = update_epsilon >= TargetUpdatePeriod;
				if (is_running_slowly && --is_running_slowly_retries == 0)
				{
					// If UpdateFrame consistently takes longer than TargetUpdateFrame
					// stop raising events to avoid hanging inside the UpdateFrame loop.
					break;
				}
			}

			elapsed = ClampElapsed(timestamp - render_timestamp);
			if (elapsed > 0 && elapsed >= TargetRenderPeriod)
			{
				RaiseRenderFrame(elapsed, ref timestamp);
			}
		}

		void RaiseUpdateFrame(double elapsed, ref double timestamp)
		{
			// Raise UpdateFrame event
			if (Exists && !IsExiting) OnUpdateFrame(elapsed);

			// Update UpdatePeriod/UpdateFrequency properties
			update_period = elapsed;

			// Update UpdateTime property
			update_timestamp = timestamp;
			timestamp = watch.Elapsed.TotalSeconds;
			update_time = timestamp - update_timestamp;
		}


		void RaiseRenderFrame(double elapsed, ref double timestamp)
		{
			// Raise RenderFrame event
			if (Exists && !IsExiting) OnRenderFrame(elapsed);

			// Update RenderPeriod/UpdateFrequency properties
			render_period = elapsed;

			// Update RenderTime property
			render_timestamp = timestamp;
			timestamp = watch.Elapsed.TotalSeconds;
			render_time = timestamp - render_timestamp;
		}


	}
}

