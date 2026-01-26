using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using OpenTK;
using OpenTK.Input;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using KS.Foundation;
using System.Numerics;

namespace SummerGUI
{
	public abstract class Widget : DisposableObject, IStyleModelElement, IComparable<Widget>
	{
		// Autonumber Provider for Widget.ID
		static int NextID = 1;
		public int ID { get; private set; }
		public override int GetHashCode ()
		{
			return ID;
		}

		private bool m_IsMenu;
		public bool IsMenu 
		{ 
			get {
				return m_IsMenu;
			}
			set {
				if (m_IsMenu != value) {
					m_IsMenu = value;
					if (m_IsMenu) {
						CanFocus = false;
						CanSelect = true;
					}
				}
			}
		}

		// Important to know for a good understanding of this library:
		// This comparer is applied to the Children collection of a Container
		// and is responsible for the calling sequence order in most operations
		public int CompareTo (Widget other)
		{
			if (other == null)
				return 0;
			// First by ZIndex, reverted. Larger ZIndices come first !		
			int cmp = other.ZIndex.CompareTo (this.ZIndex);
			//int cmp = this.ZIndex.CompareTo (other.ZIndex);
			if (cmp == 0) {
				// Then by Docking, the enum was ordered accordingly
				cmp = other.Dock.CompareTo (this.Dock);
				if (cmp == 0)
					// IDs generated from static are selected forward.
					// This means: elements on the same level 
					// are shown in their order of insertion
					return this.ID.CompareTo (other.ID);
			}
			return cmp;
		}
			
		private WidgetStates m_WidgetState;
		public WidgetStates WidgetState
		{ 
			get {
				return m_WidgetState;
			}
			set {
				//if (!IsDisposed && m_WidgetState != value && Styles.HasStyle (value)) {
				if (!IsDisposed && m_WidgetState != value) {
					m_WidgetState = value;
					OnWidgetStateChanged ();
					Invalidate ();
				} else {
					//this.LogWarning ("Unable to set WidgetState, Widget doesn't own the corresponding Style.");
				}
			}
		}

		protected virtual void OnWidgetStateChanged()
		{
		}

		public virtual void UpdateStyle()
		{
			if (Enabled) {
				if (IsFocused)
					WidgetState = WidgetStates.Active;
				else if (Selected)
					WidgetState = WidgetStates.Selected;
				else
					WidgetState = WidgetStates.Default;
			}
			else
				WidgetState = WidgetStates.Disabled;			
		}
			
		public WidgetStyleProvider Styles { get; private set; }
		public virtual IWidgetStyle Style 
		{ 
			get {				
				return Styles.GetStyle (WidgetState);
			}
		}			
			
		public string Name { get; private set; }	

		protected Widget (string name) : this(name, Docking.None, null) {}
		protected Widget (string name, Docking dock, IWidgetStyle style)
		{
			unchecked {
				ID = Interlocked.Increment (ref NextID);					
			}

			if (name == null)
				Name = String.Empty;
			else
				Name = name;

			if (style != null)
				Styles = new WidgetStyleProvider (style);
			else
				Styles = new WidgetStyleProvider (new EmptyWidgetStyle());

			Dock = dock;
			ScaleFactor = 1f;

			CanSelect = true;
			// we want to compare sizes fast and implizit
			m_MaxSize = SizeMax;
			m_Visible = true;
			Enabled = true;
			AutoContextMenu = true;
		}        		

        public override bool Equals(object obj)
        {
            return (obj is Widget) && Equals((Widget)obj);
        }

        public bool Equals(Widget other)
        {
            return ID.Equals(other.ID);
        }

        private Container m_Parent;
		public Container Parent 
		{ 
			get {
				return m_Parent;
			}
			internal set {				
				if (value == this) {
					this.LogError ("Can't set parent to myself.");				
				} else if (!IsDisposed && m_Parent != value) {
					if (m_Parent != null)
						m_Parent.RemoveChild (this);					
					m_Parent = value;
					OnParentChanged ();
				}
			}
		}

		/// <summary>
		/// This scales the control,
		/// Call after setting your properties
		/// </summary>
		protected virtual void OnParentChanged()
		{
			if (IsDisposed)
				return;

			if (Parent != null) {
				RootContainer root = Parent.Root ?? Parent as RootContainer;
				if (root != null)
					Root = root;

				ResetCachedLayout ();
				Parent.ResetCachedLayout ();
			} else {
				Root = null;
			}
		}

		private RootContainer m_Root;
		public RootContainer Root
		{ 
			get {
				return m_Root;
			}
			internal set {				
				if (value == this) {
					this.LogError ("Can't set root to myself.");
				} else if (!IsDisposed && m_Root != value) {
					if (m_Root != null)
						m_Root.RemoveChild (this);					
					m_Root = value;
					OnRootChanged ();
				}
			}
		}

		protected virtual void OnRootChanged()
		{
			if (!IsDisposed && Root != null) {
				try {					
					if (Math.Abs (Parent.ScaleFactor - ScaleFactor) > float.Epsilon) {
						this.ParentWindow.DpiScaling.ScaleGUI (this);
					}
				} catch (Exception ex) {
					ex.LogError ();	
				}
				ResetCachedLayout ();
			}
		}			

		public SummerGUIWindow ParentWindow
		{
			get{				
				if (Root == null)
					return null;
				return Root.CTX as SummerGUIWindow;
			}
		}

		public bool IsInitialized { get; protected set; }
		public virtual void Initialize()
		{
			IsInitialized = true;
		}


		public object Tag { get; set; }

		// ******* Base Box Stuff ***************	

		public virtual void OnUpdateTheme (IGUIContext ctx)
		{			
			Styles.RefreshStyles ();
			ResetCachedLayout ();
		}

		public float ScaleFactor { get; private set; }
		public void OnScaleWidget(IGUIContext ctx, float scaleFactor) 
		{
			if (!IsDisposed) {
				if (scaleFactor < 0.25f || Math.Abs (scaleFactor - ScaleFactor) < 0.001)
					return;			
				OnScaleWidget (ctx, scaleFactor, scaleFactor / ScaleFactor);
				ScaleFactor = scaleFactor;
				ResetCachedLayout ();
			}
		}
			
		public static long ResetCachedLayoutCalledTotal = 0;

		#if DEBUG
		long ResetCachedLayoutCalled = 0;
		#endif

		public SizeF CachedPreferredSize { get; protected set; }
		protected virtual void ResetCachedLayout()
		{
			#if DEBUG
			unchecked {
				ResetCachedLayoutCalled++;
			}

			//Console.WriteLine ("ResetCachedLayout called {0} times for {1}", ResetCachedLayoutCalled.ToString("n0"), this.Name);
			#endif

			CachedPreferredSize = SizeF.Empty;

			// propagate to Parent, which means always up to root..
			// Das geschieht bereits genug, wenn durch Grössenänderung eines Kindes OnResize aufgerufen wird.
			//if (Parent != null)
			//	Parent.ResetCachedLayout ();

			// ToDo: Evtl. auch nur nach Size-Change.
			// Aber dann ist es evtl. schon zu spät..
			// Dann flubbelt die GUI, oder mann müsste mehrmals Invalidieren.
			// Ob man das bei 1/60 Sekunde überhaupt sehen kann ?
			//
			// Vorerst sind wir mal ganz konservativ..
			// Ein zählendes TextLabel führt zu ständigem Update 
			// eines ganzen Teilastes der GUI.
		}

		public virtual void Update(bool updateParent = false, int frames = 0)
		{			
			ResetCachedLayout ();
			if (updateParent && Parent != null)
				Parent.ResetCachedLayout ();
			if (frames >= 0)
				Invalidate (frames);
		}

		protected virtual void OnScaleWidget(IGUIContext ctx, float absoluteScaleFactor, float relativeScaleFactor)
		{		
			ResetCachedLayout ();
		}			

		public RectangleF Bounds  { get; protected set; }

		int m_ZIndex;
		public int ZIndex 
		{ 
			get {
				return m_ZIndex;
			}
			set {
				if (m_ZIndex != value) {
					m_ZIndex = value;
					if (Parent != null) {
						Parent.ReOrganize ();
					}
				}
			}
		}	
		public bool IsOverlay
		{
			get{
				return ZIndex >= 10000;
			}
		}

		SizeF m_MinSize;
		[DpiScalable]
		public SizeF MinSize  
		{ 
			get {
				return m_MinSize;
			}
			set {
				if (m_MinSize != value) {
					m_MinSize = value;
					OnMinSizeChanged ();
					SetBounds (Bounds);
				}
			}
		}

		protected virtual void OnMinSizeChanged()
		{
			ResetCachedLayout ();
		}

		SizeF m_MaxSize;
		[DpiScalable]
		public SizeF MaxSize
		{ 
			get {
				return m_MaxSize;
			}
			set {
				if (m_MaxSize != value) {
					m_MaxSize = value;
					OnMaxSizeChanged ();
					SetBounds (Bounds);
				}
			}
		}

		protected virtual void OnMaxSizeChanged()
		{
			ResetCachedLayout ();
		}

		public static readonly SizeF SizeMax = new SizeF (float.MaxValue, float.MaxValue);

		private Docking m_Dock;
		public Docking Dock  
		{ 
			get {
				return m_Dock;
			}
			set {
				if (m_Dock != value) {
					m_Dock = value;
					if (Parent != null) {
						Parent.ReOrganize ();
					}
				}
			}
		}

		public Alignment HAlign  { get; set; }
		public Alignment VAlign  { get; set; }

		protected Padding m_Margin;
		[DpiScalable]
		public Padding Margin 
		{ 
			get {
				return m_Margin;
			}
			set {
				if (m_Margin != value) {
					m_Margin = value;
					OnMarginChanged ();
				}
			}
		}
		protected virtual void OnMarginChanged ()
		{
			ResetCachedLayout ();
		}
			
		protected Padding m_Padding;
		[DpiScalable]
		public Padding Padding
		{ 
			get {
				return m_Padding;
			}
			set {
				if (m_Padding != value) {
					m_Padding = value;
					OnPaddingChanged ();
				}
			}
		}
		protected virtual void OnPaddingChanged ()
		{
			ResetCachedLayout ();
		}

		private bool m_Visible;
		public virtual bool Visible 
		{
			get {
				return m_Visible;
			}
			set {
				if (m_Visible != value) {
					m_Visible = value;
					OnVisibleChanged ();
				}
			}
		}
		protected virtual void OnVisibleChanged()
		{			
			Update (true);
		}

		private bool m_Enabled = true;
		public virtual bool Enabled {
			get {				
				return m_Enabled && Parent != null && Parent.Enabled;
			}
			set {				
				if (m_Enabled != value) {
					m_Enabled = value;
					OnEnabledChanged ();
				}
			}
		}

		protected virtual void OnEnabledChanged()
		{			
			UpdateStyle ();
			ResetCachedLayout ();
			Invalidate (1);
		}

		public bool IsVisibleEnabled
		{
			get{
				return Visible && Enabled;
			}
		}
			
		public bool TabStop { get; set; }
		public virtual int TabIndex { get; set; }

		private Cursors m_Cursor;
		public Cursors Cursor 
		{ 
			get {
				return m_Cursor;
			}
			set {
				if (m_Cursor != value) {
					m_Cursor = value;
					RefreshCursor ();
				}
			}
		}


		// *** Focusing ***

		public event EventHandler<EventArgs> GotFocus;
		public virtual void OnGotFocus()
		{
			UpdateStyle ();
			IsMouseOrKeyDown = false;
			if (GotFocus != null)
				GotFocus (this, EventArgs.Empty);
		}			

		public event EventHandler<EventArgs> LostFocus;
		public virtual void OnLostFocus()
		{
			UpdateStyle ();
			if (LostFocus != null)
				LostFocus (this, EventArgs.Empty);
		}
			
		public virtual bool IsFocused  
		{ 
			get {				
				RootContainer root = this.Root;
				if (root == null)
					return false;					
				return root.FocusedWidget == this;
			}
			set {				
				if (!IsDisposed && Parent != null) {
					if (value && IsMenu) {
						Selected = true;
						return;
					}
					RootContainer root = this.Root;
					if (root != null && root.FocusedWidget != this) {
						if (value && CanFocus)
							root.FocusedWidget = this;
						else
							root.FocusedWidget = null;
						ResetCachedLayout ();
						Invalidate ();
					}
				}
			}
		}

		public virtual void Focus()
		{
			if (IsMenu) {
				Select ();
				return;
			}

			if (CanFocus && IsVisibleEnabled)
				IsFocused = true;
		}

		public virtual bool CanFocus { get; set; }
		public virtual bool CanSelect { get; set; }

		private bool m_Selected;
		public virtual bool Selected  
		{ 
			get {
				if (IsMenu) {
					RootContainer root = this.Root;
					if (root == null)
						return false;
					return root.SelectedMenu == this;
				} else {
					return m_Selected;
				}
			} 
			set {				
				if (IsMenu) {
					if (!IsDisposed && Parent != null) {					
						RootContainer root = this.Root;
						if (root != null && root.SelectedMenu != this) {
							if (value && CanSelect)
								root.SelectedMenu = this;
							else
								root.SelectedMenu = null;
							UpdateStyle ();
							Update ();
						}
					}
				} else {
					if (!CanSelect)
						value = false;
					if (m_Selected != value) {
						m_Selected = value;
						UpdateStyle ();
						Update ();
					}				
				}
			}
		}

		public void Select()
		{
			if (CanSelect && IsVisibleEnabled)
				Selected = true;
		}			

		// *** Metrics ***

		public SizeF Size
		{ 
			get {
				return Bounds.Size;
			}
		}

		public PointF TooltipLocation
		{ 
			get {
				return Bounds.Location;
			}
		}

		public float Left
		{ 
			get {
				return Bounds.Left;
			}
		}

		public float Top
		{ 
			get {
				return Bounds.Top;
			}
		}

		public float Width
		{ 
			get {
				return Bounds.Width;
			}
		}

		public float Height
		{ 
			get {
				return Bounds.Height;
			}
		}

		public float Bottom
		{ 
			get {
				return Bounds.Bottom;
			}
		}

		public float Right
		{ 
			get {
				return Bounds.Right;
			}
		}
			
		public virtual RectangleF ClientRectangle
		{
			get{				
				return new RectangleF (					
					Bounds.Left + Padding.Left,
					Bounds.Top + Padding.Top,
					Math.Max(0, Bounds.Width - Padding.Width),
					Math.Max(0, Bounds.Height - Padding.Height)
				);
			}
		}

		public virtual RectangleF PaddingBounds
		{
			get{				
				return new RectangleF (
					Bounds.Left + Padding.Left,
					Bounds.Top + Padding.Top,
					Math.Max(0, Bounds.Width - Padding.Width),
					Math.Max(0, Bounds.Height - Padding.Height)
				);
			}
		}
		
		public virtual RectangleF MarginBounds
		{
			get{				
				return new RectangleF (
					Bounds.Left - Margin.Left,
					Bounds.Top - Margin.Top,
					Bounds.Width + Margin.Width,
					Bounds.Height + Margin.Height
				);
			}
		}				

		// ********** set size and location ***

		public void SetSize(SizeF sz)
		{
			SetSize (sz.Width, sz.Height);
		}

		public void SetSize(float width, float height)
		{
			SetBounds (Bounds.X, Bounds.Y, width, height);
		}
			
		public void SetLocation(PointF point)
		{
			SetLocation (point.X, point.Y);
		}

		public void SetLocation(float left, float top)
		{
			SetBounds(left, top, Bounds.Width, Bounds.Height);
		}

		public void SetBounds(RectangleF newBounds)
		{
			SetBounds (newBounds.X, newBounds.Y, newBounds.Width, newBounds.Height);
		}

		public void SetBounds(PointF newLocation, SizeF newSize)
		{
			SetBounds (newLocation.X, newLocation.Y, newSize.Width, newSize.Height);
		}

		public virtual void SetBounds(float x, float y, float width, float height)
		{
			Bounds = new RectangleF (
				x,
				y,
				Math.Max (MinSize.Width, Math.Min (MaxSize.Width, width)),
				Math.Max (MinSize.Height, Math.Min (MaxSize.Height, height))		
			);
		}

		public bool HasSize
		{
			get{
				return Bounds.Width > 0 && Bounds.Height > 0;
			}
		}

		public bool CanPaint
		{
			get{
				return Visible && HasSize;
			}
		}

		public bool HandlesEnterKey { get; set; }
		public bool HandlesEscapeKey { get; set; }

		// ********* Painting *****************

		public bool InvalidateOnHeartBeat { get; set; }
		bool invalidateFlag;

		public virtual void Invalidate(int frames = 0, Widget widget = null)
		{
			if (IsLayoutSuspended)
				return;
			if (InvalidateOnHeartBeat)
				invalidateFlag = true;
			else {
				RootContainer root = Root;
				if (root != null)
					root.Invalidate (frames, widget);
			}
		}

		public virtual bool OnHeartBeat()
		{
			return InvalidateOnHeartBeat && invalidateFlag;
		}			

		public SizeF PreferredSize(IGUIContext ctx)
		{
			return PreferredSize (ctx, SizeF.Empty);
		}

		public virtual SizeF PreferredSize(IGUIContext ctx, SizeF proposedSize)
		{
			return proposedSize;			
		}

		public virtual void OnMouseEnter (IGUIContext ctx)
		{	
			if (Cursor != Cursors.Default)
				ctx.SetCursor (Cursor);
			WidgetState = WidgetStates.Hover;
		}

		public void RefreshCursor()
		{
			RootContainer root = Root;
			if (root != null)
				root.CTX.SetCursor (Cursor);
		}

		public virtual void OnMouseLeave (IGUIContext ctx)
		{
			ctx.SetCursor (Cursors.Default);
			UpdateStyle ();
		}

		private DateTime m_LastMouseDownDate = DateTime.MinValue;
		private PointF m_LastMouseDownMousePosition = PointF.Empty;
		private PointF m_LastMouseDownUpperLeft = PointF.Empty;
		public PointF LastMouseDownMousePosition
		{
			get{
				return m_LastMouseDownMousePosition;
			}
		}

		public PointF LastMouseDownUpperLeft
		{
			get{
				return m_LastMouseDownUpperLeft;
			}
		}

		/// <summary>
		/// Call base.OnMouseDown(e) before your implementation.
		/// You must check for Visibility and Enabled then.
		/// </summary>
		/// <param name="e">E.</param>	
		internal virtual void InvokeMouseDown (MouseButtonEventArgs e)
		{
			if (!IsVisibleEnabled)
				return;

			//Debug.WriteLine ("Widget.InvokeMouseDown()");

			bool bDouble = e.Button == MouseButton.Left && m_LastMouseDownDate != DateTime.MinValue
               && ((DateTime.Now - m_LastMouseDownDate).TotalMilliseconds < 350)
               && ClickDistance (e) < 4;			

			m_LastMouseDownMousePosition = new PointF (e.X, e.Y);
			m_LastMouseDownUpperLeft = new PointF (TooltipLocation.X, TooltipLocation.Y);
			m_LastMouseDownDate = DateTime.Now;

			if (e.Button == MouseButton.Left)
				WidgetState = WidgetStates.Pressed;

			OnMouseDown (e);
			if (bDouble)				
				OnDoubleClick (e);			

			if (Styles.HasStyle (m_WidgetState))
				Invalidate ();
		}			

		protected double ClickDistance (MouseButtonEventArgs e)
		{
			if (e == null || m_LastMouseDownMousePosition == Point.Empty)
				return -1;
			try {
				return Math.Sqrt (((double)(m_LastMouseDownMousePosition.X - e.X)).Power2 () 
					+ ((double)(m_LastMouseDownMousePosition.Y - e.Y)).Power2 ());	
			} catch (Exception ex) {
				ex.LogWarning ();
				return -1;
			}
		}

		/// <summary>
		/// Raises the OnClick event, when mouse is still hover.
		/// </summary>
		/// <param name="e">E.</param>
		internal virtual void InvokeMouseUp (MouseButtonEventArgs e)
		{
			OnMouseUp (e);	// ensure to always call OnMouseUp
			if (!Enabled)
				WidgetState = WidgetStates.Disabled;
			else if (HitTest (e.X, e.Y) == this) {
				WidgetState = WidgetStates.Hover;
				if (e.Button == MouseButton.Left && ClickDistance(e) < 16)
					OnClick (e);
			} else
				UpdateStyle ();
		}			

		public void OnClick ()
		{
			if (IsVisibleEnabled)
				OnClick (null);
		}

		/// <summary>
		/// Occurs when click. EventArgs can be null, when not invoked by mouse or touch.
		/// </summary>
		public event EventHandler<MouseButtonEventArgs> Click;
		public virtual void OnClick(MouseButtonEventArgs e) 
		{
			if (Click != null)
				Click (this, e);
		}

		// These are designed empty, so that you can't do anything wrong when overriding
		protected bool IsMouseOrKeyDown { get; private set; }
		public virtual void OnMouseDown (MouseButtonEventArgs e) 
		{
			UpdateMenus ();
			IsMouseOrKeyDown = true;
		}
		public virtual void OnDoubleClick(MouseButtonEventArgs e) {}

		public virtual void OnMouseMove (MouseMoveEventArgs e) {}
		public virtual bool OnMouseWheel (MouseWheelEventArgs e) {	return false; }
		public virtual bool OnKeyDown (KeyboardKeyEventArgs e)	
		{ 
			UpdateMenus ();
			IsMouseOrKeyDown = true;
			return false;
		}
		public virtual bool OnKeyPress (KeyPressEventArgs e)	{ return false; }

		public virtual void OnMouseUp (MouseButtonEventArgs e) 
		{
			IsMouseOrKeyDown = false;
			UpdateMenus ();
		}

		public virtual void OnKeyUp (KeyboardKeyEventArgs e) 
		{
			IsMouseOrKeyDown = false;
			UpdateMenus ();
		}

		public virtual void UpdateMenus()
		{
			if (!IsMouseOrKeyDown && !IsDisposed && this as IGuiMenuInterface != null && Root != null)
				Root.UpdateMenus (this as IGuiMenuInterface);
		}

		public virtual Widget HitTest(float x, float y)
		{			
			if (!Visible || !Enabled)
				return null;

			return Bounds.IntersectsWith (new RectangleF (x, y, 1f, 1f)) ? this : null;
		}

		public string Tooltip { get; set; }

		public virtual IGuiMenu ContextMenu { get; set; }
		public bool AutoContextMenu { get; set; }
		public event EventHandler<EventArgs> SetupContextMenu;
		public virtual bool OnSetupContextMenu(string widgetname)
		{
			if (SetupContextMenu != null)
				SetupContextMenu (this, EventArgs.Empty);
			return AutoContextMenu || ContextMenu != null;
		}

		private int m_SuspensionCounter = 0;
		public void SuspendLayout()
		{
			m_SuspensionCounter++;
		}

		public void ResumeLayout()
		{
			if (m_SuspensionCounter > 0)
				m_SuspensionCounter--;
			if (m_SuspensionCounter == 0 && !IsDisposed)
				PerformLayout ();
		}

		public bool IsLayoutSuspended
		{
			get{
				return m_SuspensionCounter > 0;
			}
		}

		public void PerformLayout()
		{
			Invalidate ();
		}		

		/// <summary>
		/// When overriding, call base.OnLayout() finally
		/// </summary>
		/// <param name = "ctx"></param>
		/// <param name="bounds">Bounding box.</param>
		public virtual void OnLayout(IGUIContext ctx, RectangleF bounds)
		{			
			if (IsLayoutSuspended)
				return;

			RectangleF oldBounds = Bounds;

			/*** ***/
			RectangleF rBounds = new RectangleF (
				bounds.Left + Margin.Left,
				bounds.Top + Margin.Top,
				Math.Max(0, bounds.Width - Margin.Width),
				Math.Max(0, bounds.Height - Margin.Height)
			);				
				
			if (Dock == Docking.Fill) {				
				SetBounds (rBounds);
			} else {				
				RectangleF pref = new RectangleF (rBounds.Location, PreferredSize (ctx, rBounds.Size));
				
				// NEU: Sanitize MinSize / MaxSize
				pref.Width = Math.Max(MinSize.Width, MathF.Min(pref.Width, MaxSize.Width));
				pref.Height = Math.Max(MinSize.Height, MathF.Min(pref.Height, MaxSize.Height));

				pref.Intersect (rBounds);				
							
				// ToDo:
				// Layout this box
				switch (this.Dock) {
				case Docking.Left:					
					SetBounds (new RectangleF (rBounds.Left, rBounds.Top, pref.Width, rBounds.Height));
					break;
				case Docking.Top:
					SetBounds (new RectangleF (rBounds.Left, rBounds.Top, rBounds.Width, pref.Height));
					break;
				case Docking.Right:
					SetBounds (new RectangleF (rBounds.Right - pref.Width, rBounds.Top, pref.Width, rBounds.Height));
					break;
				case Docking.Bottom:
					SetBounds (new RectangleF (rBounds.Left, rBounds.Bottom - pref.Height, rBounds.Width, pref.Height));
					break;
				/***
				case Docking.Fill:
					SetBounds (rBounds);
					break;
				***/
				case Docking.None:
					SetBounds (pref);
					break;
				}
			}

			//if (Bounds.Size != oldBounds.Size) {				
			if (Bounds != oldBounds) {
				OnResize ();
			}
		}

		public virtual void OnResize()
		{						
			ResetCachedLayout ();
			if (Parent != null)
				Parent.ResetCachedLayout ();
		}

		/// <summary>
		/// You may call or not call base.OnPaintBackground()
		/// </summary>
		/// <param name="ctx">Context.</param>
		/// <param name="bounds">Bounds.</param>
		public virtual void OnPaintBackground(IGUIContext ctx, RectangleF bounds)
		{			
			if (Style != null)
				Style.PaintBackground (ctx, this);		
		}
			
		/// <summary>
		/// This is entirely on your own. parent.OnPaint() does nothing.
		/// </summary>
		/// <param name="ctx">Context.</param>
		/// <param name="bounds">Bounds.</param>
		public virtual void OnPaint(IGUIContext ctx, RectangleF bounds)
		{			
		}

		// RECURSIVE Update Function
		public virtual void Update(IGUIContext ctx)
		{
			//string test = this.Name;
			//
			if (!Visible || Bounds.Width <= 0 || Bounds.Height <= 0 || Parent == null || !Parent.Visible)
				return;

			try {				
				OnPaintBackground(ctx, Bounds);
				OnPaint (ctx, Bounds);
			} catch (Exception ex) {
				ex.LogError ();
			}
		}			

		public Color BackColor
		{
			get{
				return Style == null ? Color.Empty : Style.BackColorBrush.Color;
			}
			set {
				if (Style != null)
					Style.BackColorBrush.Color = value;
			}
		}

		public Color ForeColor
		{
			get{
				return Style == null ? Color.Empty : Style.ForeColorPen.Color;
			}
			set {
				if (Style != null) {
					Style.ForeColorBrush.Color = value;
					Style.ForeColorPen.Color = value;
				}
			}
		}

		public Color BorderColor
		{
			get{
				return Style == null ? Color.Empty : Style.BorderColorPen.Color;
			}
			set{
				if (Style != null)
					Style.BorderColorPen.Color = value;
			}
		}

		public float Border
		{
			get{
				return Style == null ? 0 : Style.BorderColorPen.Width;
			}
			set{
				if (Style != null && Style.Border != value) {
					Style.Border = value;
					ResetCachedLayout ();
				}
			}
		}
			
		public override string ToString ()
		{
			return string.Format ("{0} [Name:{1}]", GetType().Name, Name);
		}

		// Animation Support
		public event EventHandler<EventArgs> AnimationCompleted;
		public void OnAnimationCompleted()
		{
			if (AnimationCompleted != null)
				AnimationCompleted (this, EventArgs.Empty);
		}

		/***
		[System.ComponentModel.Browsable(false)]
		internal string _FontTag;
		[System.ComponentModel.Browsable(false)]
		internal string _IconFontTag;
		***/

		protected override void CleanupManagedResources ()
		{
			m_Parent = null;
			m_Root = null;
			ContextMenu = null;
			base.CleanupManagedResources ();
		}			
	}

	public static class WidgetExtensions
	{
		public static bool SetFontByTag(this Widget w, CommonFontTags tag)
		{
			return w.SetFontByTag (tag.ToString ());
		}

		public static bool SetFontByTag(this Widget w, string fontTag)
		{
			if (w != null && !w.IsDisposed && ReflectionUtils.HasProperty(w.GetType(), "Font")) {
				
				ReflectionUtils.SetPropertyValue (w, "Font", GetFont (fontTag));
				return true;				
			}
			return false;
		}

		public static bool SetIconFontByTag(this Widget w, CommonFontTags tag)
		{
			return w.SetIconFontByTag (tag.ToString());
		}

		public static bool SetIconFontByTag(this Widget w, string iconFontTag)
		{
			if (w != null && !w.IsDisposed && ReflectionUtils.HasProperty(w.GetType(), "IconFont")) {				
				ReflectionUtils.SetPropertyValue (w, "IconFont", GetIconFont (iconFontTag));
				return true;				
			}
			return false;
		}

		public static IGUIFont GetFont(CommonFontTags tag)
		{
			return GetFont (tag.ToString ());
		}

		public static IGUIFont GetFont(string fontTag)
		{
			if (String.IsNullOrEmpty(fontTag))
				return FontManager.Manager [CommonFontTags.Default.ToString()];
			else
				return FontManager.Manager [fontTag];
		}

		public static IGUIFont GetIconFont(CommonFontTags tag)
		{
			return GetIconFont (tag.ToString());
		}

		public static IGUIFont GetIconFont(string iconFontTag)
		{			
			if (String.IsNullOrEmpty(iconFontTag))
				return FontManager.Manager [CommonFontTags.SmallIcons.ToString()];
			else
				return FontManager.Manager [iconFontTag];
		}

		public static void SetContextMenu(this Widget w, string menuTag)
		{
			w.ParentWindow.Do(window => window.MainMenu.Do(menu => menu.FindItem(menuTag)
				.Do(item => w.ContextMenu = item.Children)));
		}
	}
}

