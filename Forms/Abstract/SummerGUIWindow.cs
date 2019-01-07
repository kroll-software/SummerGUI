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
using OpenTK.Input;
using OpenTK.Platform.X11;
using KS.Foundation;

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

	public static class ModifierKeys
	{
		public static bool ControlPressed { get; set; }
		public static bool ShiftPressed { get; set; }

		private static int m_AltPressedCount;
		public static void OnAltPressed()
		{
			if (m_AltPressedCount < 2)
				m_AltPressedCount++;
		}
		public static void OnAltReleased()
		{
			if (m_AltPressedCount > 0)
				m_AltPressedCount--;
		}

		public static bool AltPressed 
		{ 
			get {
				return m_AltPressedCount > 0;
			}
		}
	}

	public class GUIContextResolver
	{
		private Dictionary<int, IGUIContext> dictContexts;
		public GUIContextResolver()
		{
			dictContexts = new Dictionary<int, IGUIContext> ();
		}

		public void AddContext(int ID, IGUIContext ctx)
		{
			if (!dictContexts.ContainsKey (ID))
				dictContexts.Add (ID, ctx);
			else
				dictContexts.LogWarning ("Duplicate Context registered: {0}", ID);
		}

		public void RemoveContext(int ID)
		{
			if (!dictContexts.Remove (ID))
				this.LogWarning ("RemoveContext: Context not found: {0}", ID);
		}

		public IGUIContext CurrentContext
		{
			get{
				if (GraphicsContext.CurrentContext == null) {
					if (dictContexts != null && dictContexts.Count == 1)
						return dictContexts.Values.FirstOrDefault();
					return null;
				}

				int ID = GraphicsContext.CurrentContext.GetHashCode ();
				IGUIContext ctx;
				if (dictContexts.TryGetValue (ID, out ctx))
					return ctx;
				return null;
			}
		}
	}

	public abstract class SummerGUIWindow : OpenTK.NativeWindow, IGUIContext
	{		
		/**
		private VertexBuffer<ColouredVertex> vertexBuffer;
		private ShaderProgram shaderProgram;
		private VertexArray<ColouredVertex> vertexArray;
		private Matrix4Uniform projectionMatrix;
		**/

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

		public static GUIContextResolver ContextResolver
		{
			get{
				return Singleton<GUIContextResolver>.Instance;
			}
		}
			
		public static IGUIContext CurrentContext
		{
			get{
				return ContextResolver.CurrentContext;
			}
		}


		public RootContainer Controls { get; private set; }

		public IGuiMenu MainMenu { get; set; } 
		public MenuManager MenuManager { get; private set; }

		static object GetStaticFieldValue(Type type, string fieldName)
		{
			return type.GetField(fieldName,
				System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).GetValue(null);
		}

		static void SetStaticFieldValue(Type type, string fieldName, object value)
		{
			type.GetField(fieldName,
				System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).SetValue(null, value);
		}

		public void AddChildWindow(ChildFormWindow wnd)
		{
			if (!ChildWindows.Contains (wnd)) {
				ChildWindows.AddLast (wnd);

				//var carbonWindow = ( CarbonWindowInfo)Context;
				//OpenTK.Platform.X11  x11;

				//X11GLContext hat die

				//var Display = ((X11WindowInfo)window).Display;

				//((X11WindowInfo)window).Display
				//using 	(var Display = System.IntPtr
				//((OpenTK.Platform.IWindowInfo)wnd).Handle;


				/***
				Display = ((X11WindowInfo)window).Display;
				currentWindow = (X11WindowInfo)window;
				currentWindow.VisualInfo = SelectVisual(mode, currentWindow);
				ContextHandle shareHandle = shared != null ?
					(shared as IGraphicsContextInternal).Context : (ContextHandle)IntPtr.Zero;
					***/


				//OpenTK.Platform.Utilities.CreateX11WindowInfo (((OpenTK.Platform.IWindowInfo)wnd).Handle);
				//OpenTK.Platform.X11

				//SystemSpecific.Linux.Common.MakeParentWindow ((((OpenTK.Platform.IWindowInfo)wnd) as OpenTK.Platform.X11.X11WindowInfo).Display, this, wnd);
			}
		}

		public void RemoveChildWindow(ChildFormWindow wnd)
		{
			try {				
				ChildWindows.Remove (wnd);
			} catch (Exception ex) {
				ex.LogError ();
			}
		}

		protected ClassicLinkedList<ChildFormWindow> ChildWindows { get; private set; }
		public ChildFormWindow ActiveChildWindow 
		{ 
			get {				
				if (ChildWindows.Count == 0)
					return null;
				return ChildWindows.First;
			}
		}
		public bool HasChildWindow
		{
			get{
				return ChildWindows.Count > 0;
			}
		}

		public bool ActivateChildWindow()
		{			
			if (ChildWindows.Count > 0) {		
				var cw = ChildWindows.First;
				if (cw != null) {	
					// ToDo: BringToFront on Linux !
					cw.BringToFront ();
				}
				return true;
			}
			return false;
		}

		public string Name { get; protected set; }

		public DpiScalingAutomat DpiScaling { get; private set; }

		public int DPI { get; protected set; }
		public virtual void DetectDPI()
		{
			// ToDo: Detect DPI from the Display-Device and calculate the ScaleFactor
			DPI = 72;
			ScaleFactor = 1f;
		}

		public bool IsCreated { get; private set; }

		public float ScaleFactor { get; protected set; }
		public SummerGUIWindow ParentWindow  { get; private set; }

		public ClipBoundStackClass ClipBoundStack { get; private set; }

		protected SummerGUIWindow (string caption, int width, int height, SummerGUIWindow parent = null, GameWindowFlags flags = GameWindowFlags.Default)
			: base(width, height, caption, flags, GraphicsMode.Default, DisplayDevice.Default)				
		{
			try
			{								
							

				// give the former window a chance to flush it's buffers !
				//Thread.Sleep(20);

				LoadingErrorsQueue = new Queue<LoadingError>();
				ClipBoundStack = new ClipBoundStackClass(this);

				m_Context = new GraphicsContext(GraphicsMode.Default, WindowInfo, 3, 0, GraphicsContextFlags.ForwardCompatible);
				m_Context.MakeCurrent(WindowInfo);
				(m_Context as IGraphicsContextInternal).LoadAll();
				m_Context.ErrorChecking = false;

				// equals VSync = off, 
				// all else is flickering with this application
				Context.SwapInterval = 1;	

				this.LogInformation ("OpenGL Version: {0}", GL.GetString(StringName.Version));

				//Name = "MainForm";

				// BTW: Some of these WindowInfo implementations do have a 'Parent' property
				// which is unaccessible and not in use, I guess.
				ParentWindow = parent;

				DetectDPI ();
				//DetectDevice ();

				ChildWindows = new ClassicLinkedList<ChildFormWindow> ();
				Animator = new AnimationService (60);

				MaxDirtyPaint = 10;
				MaxDirtyLayout = 10;

				ThreadSleepOnEmptyUpdateFrame = 1;
				ThreadSleepOnEmptyRenderFrame = 1;

				OriginalWidth = Width;
				OriginalHeight = Height;		

				InitFonts ();
				InitCursors ();

				SetupViewport ();
				BackColor = Color4.White;
				GL.ClearColor(BackColor);

				ContextResolver.AddContext (this.Context.GetHashCode (), this);
				Controls = new RootContainer (this);

				DpiScaling = new DpiScalingAutomat (this);
			}
			catch (Exception ex)
			{
				ex.LogError ();
				base.Dispose();
				throw;
			}
		}


		public int OriginalWidth { get; private set; }
		public int OriginalHeight { get; private set; }
		public float LayoutFrameRate { get; set; }
		public float PaintFrameRate { get; set; }

		internal void gluPerspective(double fovy, double aspect, double zNear, double zFar)
		{
			double xmin, xmax, ymin, ymax;

			ymax = zNear * Math.Tan(fovy * Math.PI / 360.0);
			ymin = -ymax;

			xmin = ymin * aspect;
			xmax = ymax * aspect;

			GL.Frustum(xmin, xmax, ymin, ymax, zNear, zFar);
		}

		private Rectangle m_LastResizeBounds = Rectangle.Empty;

		protected override void OnResize (EventArgs e)
		{			
			base.OnResize(e);
			m_Context.Update(base.WindowInfo);

			if (Bounds != m_LastResizeBounds) {				
				m_LastResizeBounds = Bounds;
				SetupViewport ();
				Invalidate ();

				// *** Test
				// später aber erst, sobald das richtig funktioniert..
				// DetectDPI ();
				// DetectDevice ();

				if (WindowState == WindowState.Normal) {
					DefaultSize = Size;
					DefaultLocation = Location;
					//Console.WriteLine ("DefaultSize set to {0}", DefaultSize);
				}
			}				
		}

		public INativeWindow GlWindow
		{
			get{
				return this;
			}
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

		private void SetupViewport()
		{							
			GL.Viewport(0, 0, this.Width, this.Height);
			GL.MatrixMode(MatrixMode.Projection);
			GL.LoadIdentity ();
			GL.Ortho(0, this.Width, this.Height, 0, -1, 1);
			iDirtyLayout = MaxDirtyLayout * 2;	// be safe and allow 10 layouts
		}			

		public event EventHandler<EventArgs> Load;
		protected virtual void OnLoad(EventArgs e)
		{
			IsCreated = true;

			PerformanceTimer.Time (() => {
				InitializeWidgets ();
			}, 1, "Initialization of all Widgets");

			if (MainMenu != null) {
				MenuManager = new MenuManager (Controls);
				MenuManager.InitMenu (MainMenu);
			}

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

			// activate immediate swapping for flicker-free painting.
			Context.SwapInterval = 0;
		}

		protected Point DefaultLocation { get; set; }
		protected Size DefaultSize { get; set; }

		public virtual void OnLoadSettings()
		{			
			ConfigurationService.Instance.ConfigFile.Do (cfg => {
				if (!String.IsNullOrEmpty (Name) && this.WindowBorder == WindowBorder.Resizable) {				
					WindowState winState = (WindowState)cfg.GetSetting (this.Name, "WindowState", this.WindowState).SafeString ().ToEnum (this.WindowState);
					if (winState != WindowState.Minimized) {						
						int left = cfg.GetSetting (this.Name, "Left", this.Location.X).SafeInt ();
						int top = cfg.GetSetting (this.Name, "Top", this.Location.Y).SafeInt ();
						DefaultLocation = new Point (left, top);
						int width = cfg.GetSetting (this.Name, "Width", this.Width).SafeInt ();
						int height = cfg.GetSetting (this.Name, "Height", this.Height).SafeInt ();
						DefaultSize = new Size (width, height);

						if (winState != WindowState.Normal) {
							this.WindowState = winState;
							this.Context.Update (this.WindowInfo);
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
							cfg.SetSetting (this.Name, "Width", DefaultSize.Width);
							cfg.SetSetting (this.Name, "Height", DefaultSize.Height);
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
			
		public FontManager FontManager { get; private set; }
		public WindowResourceManager ResourceManager { get; private set; }

		void InitFonts()
		{
			if (FontManager != null)
				FontManager.Dispose ();

			FontManager = new FontManager (this);
			OnInitFonts ();

			// Preload some icons for messageboxes
			string test = "" + (char)FontAwesomeIcons.fa_info_circle
			              + (char)FontAwesomeIcons.fa_exclamation_circle
			              + (char)FontAwesomeIcons.fa_warning
			              + (char)FontAwesomeIcons.fa_times_circle
			              + (char)FontAwesomeIcons.fa_question_circle
			              + (char)FontAwesomeIcons.fa_life_ring
			              + (char)FontAwesomeIcons.fa_anchor;
			this.MeasureString (test, CommonFontTags.LargeIcons.ToString());
		}

		// Preload your fonts here
		protected virtual void OnInitFonts()
		{	
			//FontManager.InitAllFonts ();
			FontManager.InitDefaultFont();
		}

		void InitCursors()
		{						
			if (ResourceManager != null)
				ResourceManager.Dispose ();
			ResourceManager = new WindowResourceManager ();
			OnInitCursors ();
			OnLoadWindowIcon ();
		}

		protected virtual void OnLoadWindowIcon()
		{			
			string path = "Assets/Icons/SummerGUI64.ico".FixedExpandedPath();
			ResourceManager.LoadWindowIcon (this, path);
		}

		// Preload your cursors here
		protected virtual void OnInitCursors()
		{				
			ResourceManager.LoadCursorFromFile (this, "Assets/Cursors/VSplit.png", Cursors.VSplit);
			ResourceManager.LoadCursorFromFile (this, "Assets/Cursors/HSplit.png", Cursors.HSplit);
			ResourceManager.LoadCursorFromFile (this, "Assets/Cursors/Text.png", Cursors.Text);
			ResourceManager.LoadCursorFromFile (this, "Assets/Cursors/Wait.png", Cursors.Wait);
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
				ResourceManager.SetCursor (this, name);
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
				ResourceManager.SetCursor (this, name);
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
                ResourceManager.SetCursor(this, m_CurrentCursorName);                
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
				/***
				if (ThreadSleepOnEmptyUpdateFrame > 1) {
					ThreadSleepOnEmptyUpdateFrame = 1;
					ThreadSleepOnEmptyRenderFrame = 1;
				}
				if (ThreadSleepOnEmptyRenderFrame > 1) {					
					ThreadSleepOnEmptyRenderFrame = 1;
				}
				***/
				ThreadSleepOnEmptyUpdateFrame = 1;
				ThreadSleepOnEmptyRenderFrame = 1;
				RaiseSleepTimeCounter = 60;
			} catch {}
		}						

		protected override void OnKeyDown (OpenTK.Input.KeyboardKeyEventArgs e)
		{
			//this.LogDebug ("OnKeyDown {0}", e.Key);
			//base.OnKeyDown (e);
			if (HasChildWindow)
				return;
			ModifierKeys.ControlPressed = e.Control;
			ModifierKeys.ShiftPressed = e.Shift;
			if (e.Key == Key.LAlt || e.Key == Key.RAlt) {
				// render Mnemonics without additional handlers
				ModifierKeys.OnAltPressed();
				LowerSleepTime ();
				Invalidate ();
				return;
			}
			LowerSleepTime ();
			Controls.OnKeyDown (e);
			//this.LogDebug ("AltPressed: {0}", ModifierKeys.AltPressed);
		}

		protected override void OnKeyPress (KeyPressEventArgs e)
		{
			//this.LogDebug ("OnKeyPress {0}", e.KeyChar);
			//base.OnKeyPress (e);
			if (HasChildWindow)
				return;			
			Controls.OnKeyPress (e);
		}

		protected override void OnKeyUp (OpenTK.Input.KeyboardKeyEventArgs e)
		{			
			//this.LogDebug ("OnKeyUp {0}", e.Key);
			//base.OnKeyUp (e);
			if (HasChildWindow)
				return;
			ModifierKeys.ControlPressed = e.Control;
			ModifierKeys.ShiftPressed = e.Shift;

			// Bug in OpenTK: This is raised, even when an Alt-Key was never released !
			if (e.Key == Key.LAlt || e.Key == Key.RAlt) {
				// render Mnemonics without additional handlers
				ModifierKeys.OnAltReleased();
				LowerSleepTime ();
				Invalidate ();
			}
			//this.LogDebug ("AltPressed: {0}", ModifierKeys.AltPressed);
			Controls.OnKeyUp(e);
		}

		// finding out that this is a BUG took me some hours..
		// After each mouse down, a OnMouseWheel is fired, where the wheel isn't touched at all
		private bool m_MouseDownFlag = false;
		protected override void OnMouseDown (MouseButtonEventArgs e)
		{			
			if (HasChildWindow)
				return;
			//this.LogDebug ("OnMouseDown: {0}, Focused={1}", Name, Focused);
			m_MouseDownFlag = true;
			base.OnMouseDown (e);
			Controls.OnMouseDown (e);
		}

		protected override void OnMouseUp (MouseButtonEventArgs e)
		{
			if (HasChildWindow)
				return;			
			base.OnMouseUp (e);
			Controls.OnMouseUp (e);
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

		protected override void OnMouseWheel (MouseWheelEventArgs e)
		{			
			if (HasChildWindow || m_MouseDownFlag || e.Delta == 0)
				return;						
			//this.LogDebug ("OnMouseWheel: {0}, Focused={1}", Name, Focused);
			LowerSleepTime ();
			base.OnMouseWheel (e);
			Controls.OnMouseWheel (e);
		}

		public T AddChild<T>(T c) where T : Widget
		{
			return Controls.AddChild (c);
		}			

		public Color4 BackColor { get; set; }	

		public int MaxDirtyPaint { get; set; }
		public int MaxDirtyLayout { get; set; }

		private int iDirtyPaint = 5;
		private int iDirtyLayout = 0;
			
		public void Invalidate()
		{
			if (IsCreated)				
				Invalidate (MaxDirtyLayout);
		}

		/// <summary>
		/// this can be called from multiple threads
		/// </summary>
		public void Invalidate(int frames)
		{						
			if (IsExiting)
				return;

			if (frames <= 0)
				frames = MaxDirtyPaint;

			if (iDirtyLayout < MaxDirtyLayout)
				iDirtyLayout += 2;
			iDirtyPaint = Math.Max(1, Math.Min(iDirtyPaint + frames, MaxDirtyPaint));
		}
			
		public readonly object SyncObject = new object ();
		private void DoPaint(Rectangle bounds)
		{							
			int clipCount = ClipBoundStack.Count;
			if (clipCount > 0) {
				ClipBoundStack.Clear ();
				this.LogWarning ("ClipBoundStack was not empty, Count: {0}", clipCount);
			}

			GL.MatrixMode (MatrixMode.Modelview);
			GL.LoadIdentity ();

			GL.Clear (ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
		
			this.SetDefaultRenderingOptions ();
			GL.Scissor (0, 0, Width, Height);

			try {
				OnPaint (bounds);
				GL.Flush();
			} catch (Exception ex) {
				ex.LogError ("DoPaint");
			} finally {
				OnAfterPaint ();
			}
		}

		protected virtual void OnPaint(Rectangle bounds)
		{			
			this.Controls.Update (this);
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

		protected override void OnFocusedChanged (EventArgs e)
		{			
			if (IsExiting)
				return;
			//this.LogInformation ("OnFocusedChanged: {0}, {1}", Name, Focused);
			base.OnFocusedChanged (e);

			if (Focused && ActivateChildWindow ())
				return;

			Invalidate ();
		}


		protected override void OnVisibleChanged (EventArgs e)
		{
			base.OnVisibleChanged (e);
			if (IsExiting || !Visible)
				return;			
			Invalidate ();
		}

		protected override void OnWindowStateChanged (EventArgs e)
		{						
			if (IsExiting || !Visible)
				return;

			// works better without, causes some problems
			// when switching to and from fullscreen..
			/***
			if (WindowState == WindowState.Normal) {
				Location = DefaultLocation;
				Size = DefaultSize;
			}
			***/

			base.OnWindowStateChanged (e);

			if (HasChildWindow) {
				if (WindowState == WindowState.Minimized) {
					ActiveChildWindow.WindowState = WindowState.Minimized;
				} else {					
					ActivateChildWindow ();
				}
			}
				
			//Invalidate ();
		}

		protected override void OnTitleChanged (EventArgs e)
		{
			base.OnTitleChanged (e);
			Invalidate ();
		}

		protected override void OnIconChanged (EventArgs e)
		{
			base.OnIconChanged (e);
			Invalidate ();
		}

		protected override void OnWindowBorderChanged (EventArgs e)
		{
			base.OnWindowBorderChanged (e);
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
				this.Controls.OnLayout (this, this.ClientRectangle);
				iDirtyLayout--;
			}				
		}
			
		protected virtual void OnRenderFrame(double elapsedSeconds)
		{							
			if (Animator.IsStarted) {
				Animator.Animate ();
				Invalidate (1);
			}
			else if (iDirtyPaint <= 0) {
				Thread.Sleep (ThreadSleepOnEmptyRenderFrame);
				return;
			}
						
			DoPaint (this.ClientRectangle);
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
					if (child != null && !child.IsExiting && !child.IsDisposed) {
						try {
							child.Close ();
							child.Dispose ();	
						} catch (Exception ex) {
							ex.LogError ();
						}
					}
				}
			}

			ContextResolver.RemoveContext (this.m_Context.GetHashCode ());
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
			FontManager.ReScaleFonts ();
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
		public void ToggleFullScreen()
		{
			if (IsFullScreen) {
				if (BeforeFullscreenWindowState == WindowState.Minimized || BeforeFullscreenWindowState == WindowState.Fullscreen)
					BeforeFullscreenWindowState = WindowState.Normal;
				WindowState = BeforeFullscreenWindowState;
				this.BringToFront ();	// Window does not always have focus on Linux after it
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
				try
				{
					if (m_Context != null)
					{						
						//m_Context.MakeCurrent(null);

						m_Context.Dispose();
						m_Context = null;
					}						
				}
				finally
				{
					base.Dispose();
				}
			}
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool manual)
		{
			if (manual && !IsDisposed) {
				try {
					// first of all, stop all running animations
					if (Animator != null)
						Animator.Dispose ();
					
					if (Controls != null)
						Controls.Dispose ();

					ClipBoundStack.Clear();
					MainMenu = null;

					if (FontManager != null)
						FontManager.Dispose ();
					if (ResourceManager != null)
						ResourceManager.Dispose();
				} catch (Exception ex) {
					ex.LogError ();
				}					
			}

			base.Dispose ();
		}


		// IGameWindow Implementation

		const double MaxFrequency = 500.0; // Frequency cap for Update/RenderFrame events

		readonly Stopwatch watch = new Stopwatch();

		//#pragma warning disable 612,618
		//readonly IJoystickDriver LegacyJoystick =
		//	Factory.Default.CreateLegacyJoystickDriver();
		//#pragma warning restore 612,618


		IGraphicsContext m_Context;

		public IGraphicsContext Context
		{
			get {
				return m_Context;
			}
		}

		public IRootController Controller { get; protected set; }

		bool isExiting = false;
		public bool IsExiting
		{
			get{
				return isExiting;
			}
		}

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

		public void MakeCurrent()
		{
			try {
				EnsureUndisposed();
				Context.MakeCurrent(WindowInfo);	
			} catch (Exception ex) {
				ex.LogWarning ();
			}
		}

		public void SwapBuffers()
		{
			try {
				EnsureUndisposed();
				this.Context.SwapBuffers();	
			} catch (Exception ex) {
				ex.LogWarning ();
			}
		}

		protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
		{
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

				isExiting = true;
				OnUnload (EventArgs.Empty);
			}
		}			

		/***
		public VSyncMode VSync
		{
			get
			{
				EnsureUndisposed();
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
				EnsureUndisposed();
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
				EnsureUndisposed();
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
				EnsureUndisposed();
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
				EnsureUndisposed();
				return render_time;
			}
			protected set
			{
				EnsureUndisposed();
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
				EnsureUndisposed();
				if (TargetRenderPeriod < 0.001)
					return 0.0;
				return 1.0 / TargetRenderPeriod;
			}
			set
			{
				EnsureUndisposed();
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
				EnsureUndisposed();
				return target_render_period;
			}
			set
			{
				EnsureUndisposed();
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
				EnsureUndisposed();
				if (TargetUpdatePeriod == 0.0)
					return 0.0;
				return 1.0 / TargetUpdatePeriod;
			}
			set
			{
				EnsureUndisposed();
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
				EnsureUndisposed();
				return target_update_period;
			}
			set
			{
				EnsureUndisposed();
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
				EnsureUndisposed();
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
				EnsureUndisposed();
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
				EnsureUndisposed();
				return update_time;
			}
		}

		/// <summary>
		/// Gets or states the state of the NativeWindow.
		/// </summary>
		public override WindowState WindowState
		{
			get
			{
				return base.WindowState;
			}
			set
			{
				base.WindowState = value;
				if (Context != null)
					Context.Update(WindowInfo);
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
			float rate = Math.Min(60, Math.Max(30, DisplayDevice.Default.RefreshRate));
			this.Run (rate);
			// Run(0.0, 0.0);
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

			EnsureUndisposed();

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

				Visible = true;   // Make sure the GameWindow is visible.

				//OnResize(EventArgs.Empty);

				// On some platforms, ProcessEvents() does not return while the user is resizing or moving
				// the window. We can avoid this issue by raising UpdateFrame and RenderFrame events
				// whenever we encounter a size or move event.
				// Note: hack disabled. Threaded rendering provides a better solution to this issue.
				Move += DispatchUpdateAndRenderFrame;
				Resize += DispatchUpdateAndRenderFrame;

				OnLoad(EventArgs.Empty);
				OnResize(EventArgs.Empty);

				OnApplicationRunning ();
				IsInitialized = true;
				ShowLoadingErros();

				//this.LogVerbose("Entering main loop.");
				watch.Start();
				while (true)
				{
					OnProcessEvents();
					if (Exists && !IsExiting)
						DispatchUpdateAndRenderFrame(this, EventArgs.Empty);
					else
						return;					
				}
			}
			finally
			{
				Move -= DispatchUpdateAndRenderFrame;
				Resize -= DispatchUpdateAndRenderFrame;

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
			ProcessEvents ();            
		}

		double ClampElapsed(double elapsed)
		{
			return MathHelper.Clamp(elapsed, 0.0, 1.0);
		}

		void DispatchUpdateAndRenderFrame(object sender, EventArgs e)
		{
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
			if (Exists && !isExiting) OnUpdateFrame(elapsed);

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
			if (Exists && !isExiting) OnRenderFrame(elapsed);

			// Update RenderPeriod/UpdateFrequency properties
			render_period = elapsed;

			// Update RenderTime property
			render_timestamp = timestamp;
			timestamp = watch.Elapsed.TotalSeconds;
			render_time = timestamp - render_timestamp;
		}
	}
}

