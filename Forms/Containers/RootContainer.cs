using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Input;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using KS.Foundation;
using System.Diagnostics;

namespace SummerGUI
{	
	public static class RootMessages
	{
		public const string FocusChanged = "FocusChanged";
		public const string UpdateMenus = "UpdateMenus";
	}

	public class RootContainer : Container, IObservable<EventMessage>
	{
		public IGUIContext CTX { get; private set; }
		readonly BinarySortedList<Widget> Overlays;

		readonly TaskTimer HeartbeatTimer;
		readonly HashSet<Widget> HeartbeatSubscriptions;
		public SubMenuOverlay ContextMenuOverlay { get; private set; }

		public RootContainer(IGUIContext ctx) : this(ctx, "root") {}
		public RootContainer(IGUIContext ctx, string name)
			: base(name, Docking.Fill, new EmptyWidgetStyle())
		{			
			CTX = ctx;
			Overlays = new BinarySortedList<Widget> ();
			if (ctx != null)
				this.SetSize (ctx.Width, ctx.Height);

			m_Tooltip = new TooltipWidget ("root");
			m_Tooltip.MaxSize = new SizeF(320, 600);
			m_Tooltip.ZIndex = this.ZIndex + 10000;
			AddChild (m_Tooltip);
			CanFocus = false;
			TabIndex = -1;
			TooltipDelayAction = new DelayedAction (250, TooltipAction);
			HeartbeatTimer = new TaskTimer (500, Heartbeat);
			HeartbeatTimer.Start ();
			HeartbeatSubscriptions = new HashSet<Widget> ();
			Tooltip = null;
			Messages = new MessageQueue<EventMessage> (true);
		}

		public MessageQueue<EventMessage> Messages { get; private set; }
		public IDisposable Subscribe(IObserver<EventMessage> observer)
		{
			return Messages.Subscribe (observer);
		}

		public void RemoveWidget(Widget w)
		{
			UnregisterOverlay (w);
			UnsubscribeHeartbeat (w);
		}

		protected override void OnAddChild (Widget child)
		{
			base.OnAddChild (child);
			child.Parent = this;
			child.Root = this;
		}

		protected override void OnChildRemoved (Widget child)
		{
			base.OnChildRemoved (child);
			child.Parent = null;
			child.Root = null;
			RemoveWidget (child);
		}

		public void RegisterOverlay(Widget widget)
		{
			if (widget != null && !Overlays.Contains(widget))
				Overlays.Add (widget);
		}

		public void UnregisterOverlay(Widget widget)
		{
			if (widget != null)
				Overlays.Remove (widget);
		}

		public void ClearOverlays()
		{
            // Overlays.DisposeListObjects();
            Overlays.Clear ();
		}		
			
		public new void Invalidate(int frames = 0, Widget widget = null)
		{			
			CTX.Invalidate (frames);
		}

		// The root container has no parent.
		// Margins are set by parent, so the root container can't have Margins.
		public new Padding Margin { get; private set; }

		// The root container inherits directly from Container, 
		// which doesn't have a complex layout.
		// Add a more specialized container, 
		// if you want to layout the children within the root container.
		public new Padding Padding { get; private set; }
			
		public override void OnLayout (IGUIContext ctx, RectangleF bounds)
		{
			try {
				SetBounds(bounds);
				base.OnLayout (ctx, Bounds);				
			} catch (Exception ex) {
				ex.LogError ("RootContainer.OnLayout()");
			}
		}		

		protected override void LayoutChild (IGUIContext ctx, Widget child, RectangleF bounds)
		{
			if (child == null)
				return;
			if (child == ContextMenuOverlay)
				base.LayoutChild (ctx, child, GetContextMenuBounds(ContextMenuOverlay, ContextMenuOverlay.Bounds.Location));
			else
				base.LayoutChild (ctx, child, bounds);
		}

		public override void Update (IGUIContext ctx)
		{						
			// We start a new round here.
			// Ensure the stack is empty
			ctx.ClipBoundStack.Clear ();
			// The root container sets the first clip-bounds without combining
			// all later should combine the clips..
			using (var clip = new ClipBoundClip (ctx, Bounds, false)) {
				this.OnPaintBackground(ctx, Bounds);
				if (!clip.IsEmptyClip) {
					for (int i = Children.Count - 1; i >= 0; i--) {
						Widget child = Children [i];												
						if (child != null && child.Visible && child.ZIndex < 10000) {
							try {								
								using (var clipChild = new ClipBoundClip (ctx, child.Bounds, true)) {
									if (!clipChild.IsEmptyClip) {
										child.Update (ctx);
									}
								}
							} catch (Exception ex) {
								ex.LogError ();
							}
						}
					}
				}			
			}

			try {
				Overlays.RWLock.EnterReadLock();
				//for (int i = 0; i < Overlays.Count; i++) {
				for (int i = Overlays.Count - 1; i >= 0 ; i--) {
					Widget w = Overlays [i];
					if (w != null && w.Visible)
					{
						using (new ClipBoundClip(CTX, w.Bounds, false)) {
							w.Update (ctx);
						}
					}
				}
			} catch (Exception ex) {
				ex.LogError ();
			}
			finally {
				Overlays.RWLock.ExitReadLock ();
			}
		}
			
		public override bool OnKeyDown (KeyboardKeyEventArgs e)
		{		
			//Console.WriteLine (e.Key);

			if (SelectedMenu != null) {
				if (e.Key == Keys.Escape) {
					SelectedMenu = null;
				} else {					
					if (SelectedMenu.OnKeyDown (e)) {
						Invalidate ();
						return true;
					}
				}
			}

			Widget formerlyFocusedWidget = FocusedWidget;

			try {
				if (FocusedWidget != null && FocusedWidget != this && FocusedWidget.OnKeyDown (e))
					return true;

				if (e.Key == Keys.Tab) {				
					if (e.Shift)
						SelectPrevWidget ();
					else
						SelectNextWidget ();

					//this.LogVerbose ("Widget focused: {0}", FocusedWidget != null ? FocusedWidget.Name : "null");
					return true;
				}

				// bubble up from focused
				if (FocusedWidget != null) {
					Widget parent = FocusedWidget.Parent;
					while (parent != null && parent != this) {
						if (parent.OnKeyDown (e))
							return true;
						parent = parent.Parent;
					}
				}

				return base.OnKeyDown (e);	
			} catch (Exception ex) {
				ex.LogError ();
			} finally {
				if ((FocusedWidget == null || FocusedWidget == this) && formerlyFocusedWidget != null && !formerlyFocusedWidget.IsDisposed) {
					formerlyFocusedWidget.Focus ();
				}
			}		

			return false;
		}

		public override bool OnKeyPress (KeyPressEventArgs e)
		{
			if (FocusedWidget != null && FocusedWidget.OnKeyPress (e))
				return true;
			
			return base.OnKeyPress (e);
		}

		public override void OnKeyUp (KeyboardKeyEventArgs e)
		{
			if (FocusedWidget != null)
				FocusedWidget.OnKeyUp(e);			
		}

		public override bool OnMouseWheel (MouseWheelEventArgs e)
		{			
			Widget w = HitTest (e.X, e.Y);
			if (w != null && w.OnMouseWheel (e))
				return true;

			return base.OnMouseWheel (e);
		}

		private Widget m_LastMouseMoveWidget = null;
		public override void OnMouseMove (MouseMoveEventArgs e)
		{						
			TooltipDelayAction.Enabled = false;

			if (m_LastMouseDownWidget != null) {
				m_LastMouseDownWidget.OnMouseMove (e);
				return;
			}				

			Widget c = this.HitTest (e.X, e.Y);
			if (c != null && c == m_LastMouseMoveWidget) {								
				m_LastMouseMoveWidget.OnMouseMove (e);
				SetTooltipLocation (new PointF(e.X, e.Y));
				return;
			}
				
			HideTooltip ();
			if (c != m_LastMouseMoveWidget) {
				if (m_LastMouseMoveWidget != null)
					m_LastMouseMoveWidget.OnMouseLeave (CTX);
				m_LastMouseMoveWidget = c;

				if (c != null) {
					c.OnMouseEnter (CTX);
					if (c.Tooltip != null && m_LastMouseDownWidget == null)
						ShowTooltip (c.Tooltip, new PointF(e.X, e.Y));
				}
				else
					StopTooltipTimer ();
			}
		}	

		public override Widget HitTest (float x, float y)
		{
			Widget widget = null;
			try {
				Overlays.RWLock.EnterReadLock();
				for (int i = 0; i < Overlays.Count; i++) {
					Widget w = Overlays [i];
					widget = w.HitTest(x, y);
					if (widget != null)
						break;
				}
			} catch (Exception ex) {
				ex.LogError ();
			}
			finally {
				Overlays.RWLock.ExitReadLock ();
			}

			if (widget == null)
				widget = base.HitTest (x, y);
			if (widget == this)
				return null;
			return widget;
		}

		private bool m_Enabled = true;
		public override bool Enabled {
			get {
				return m_Enabled;
			}
			set {
				m_Enabled = value;
			}
		}
        
		public void ResetLastMouseDownWidget()
		{
			m_LastMouseDownWidget = null;
		}

		private Widget m_LastMouseDownWidget = null;
		public override void OnMouseDown (MouseButtonEventArgs e)
		{
			if (!Visible || !Enabled)
				return;

			HideTooltip ();
			
			Widget c = this.HitTest (e.X, e.Y);
			//if (c == null || !Overlays.Contains (c)) 
			if (c == null || !c.IsOverlay)
			{
				int overlayCount = Overlays.Count;
				if (!Overlays.OfType<IOverlayWidget> ().Any (ov => ov.OverlayMode == OverlayModes.Modal)) {
					Overlays.OfType<IOverlayWidget> ().ToList ().ForEach (ov => ov.OnClose ());
					if (Overlays.Count != overlayCount) {
						if (FocusedWidget != null && FocusedWidget.IsOverlay)
							FocusedWidget = null;
						Invalidate ();
						return;
					}
				}
			}

			m_LastMouseDownWidget = c;
			if (c != null && !c.IsDisposed) {				
				if (c != FocusedWidget)
					c.Focus ();
				c.InvokeMouseDown (e);
			}			
		}

		public override void OnMouseUp (MouseButtonEventArgs e)
		{
			if (m_LastMouseDownWidget != null) {
				m_LastMouseDownWidget.InvokeMouseUp (e);
				Widget w = m_LastMouseDownWidget;
				m_LastMouseDownWidget = null;

				if (e.Button == MouseButton.Right && w.Visible) {
					// Show a ContextMenu
					string name = w.Name;
					MenuManager manager = CTX.MenuManager;
					if (manager == null || manager.Menu == null || manager.Menu.Count == 0) {
						this.LogWarning ("SummerGuiWindow.MenuManager is not initialized");
						return;
					}

					while (w != null && w != this) {
						if (w.Visible) {
							IGuiMenu menu = manager.GetContextMenuForWidget (w, name);
							if (menu == null) {
								return;
							} else {
								if (w != FocusedWidget && w.CanFocus)
									w.Focus ();
								//ShowContextMenu (e.Position.ToPointF (), menu);
								ShowContextMenu (e.Position, menu);
							}

							/***
							if (!w.OnSetupContextMenu (name))
								break;
							IGuiMenu menu = w.ContextMenu;
							if (menu != null) {
								if (menu.Enabled) {
									if (w != m_FocusedWidget && w.CanFocus)
										w.Focus ();
									ShowContextMenu (e.Position.ToPointF (), menu);
								}
								break;
							}
							***/
						}
						w = w.Parent;
					}
				}
			}
		}

		// *** Tooltip Service ***

		private readonly TooltipWidget m_Tooltip = null;

		// *** The next both functions ()Show and Hide) are PUBLIC, the rest is PRIVATE.
		public void ShowTooltip(string text, PointF location)
		{	
			if (String.IsNullOrEmpty (text)) {
				HideTooltip ();
				return;
			}
				
			m_Tooltip.Text = text;
			SetTooltipLocation (location);

			if (TooltipDelayAction.Enabled) {
				TooltipDelayAction.Stop();
				TooltipDelayAction.Start();
			}
		}

		public void HideTooltip()
		{				
			if (m_Tooltip != null) {				
				TooltipDelayAction.Stop();
				m_Tooltip.OnClose();
				Invalidate ();
			}
		}

		private void SetTooltipLocation(PointF location)
		{						
			PointF newPoint = new PointF(location.X + 8f, location.Y + 21f);
			if (m_Tooltip.Visible && m_Tooltip.StartLocos.Distance (newPoint) > 4)
				HideTooltip ();
			else {
				m_Tooltip.Locos = new PointF (location.X + 8f, location.Y + 21f);
			}
		}			

		readonly DelayedAction TooltipDelayAction;
		void TooltipAction()
		{			
			try {				
				if (IsDisposed || !TooltipDelayAction.Enabled)
					return;				
				TooltipDelayAction.Stop ();
				m_Tooltip.Visible = false;
				if (String.IsNullOrEmpty(m_Tooltip.Text)) {
					return;
				}
				SizeF sz = m_Tooltip.PreferredSize (CTX);
				if (sz != SizeF.Empty) {
					RectangleF ttBounds = new RectangleF (m_Tooltip.Locos, sz);
					if (ttBounds.Width > m_Tooltip.MaxSize.Width) {
						m_Tooltip.Text = m_Tooltip.Text.WrapText((int)m_Tooltip.MaxSize.Width);
					}
					if (ttBounds.Right > Width)
						ttBounds.X -= ttBounds.Right - Width + 4;
					m_Tooltip.SetBounds(ttBounds);
					m_Tooltip.Visible = true;
					m_Tooltip.OnShow();
					Invalidate(1);
				}
			} catch (Exception ex) {
				ex.LogError ();
			}
		}
			
		private void StartTooltipTimer()
		{						
			TooltipDelayAction.Start ();
		}

		public void StopTooltipTimer()
		{
			TooltipDelayAction.Stop ();
			//TooltipDelayAction.Enabled = false;
			m_Tooltip.Text = null;
			m_Tooltip.Locos = PointF.Empty;
		}


		// *** Heartbeat Service ***

		void Heartbeat()
		{
			bool invalidateFlag = false;
			invalidateFlag = FocusedWidget != null && FocusedWidget.OnHeartBeat ();
			foreach (Widget w in HeartbeatSubscriptions) {
				if (w.Visible && w != FocusedWidget)
				{
					bool b = w.OnHeartBeat ();
					if (b)
					{
						string test = w.Name;
					}

					invalidateFlag |= b;
				}
			}
			if (invalidateFlag) {
				Invalidate (1);
			}

			if (!TooltipDelayAction.Enabled) {
				if (m_Tooltip.Text != null) {
					TooltipDelayAction.Start ();
				}
			}
		}

		public void SubscribeHeartbeat(Widget w)
		{
			HeartbeatSubscriptions.Add (w);
		}

		public void UnsubscribeHeartbeat(Widget w)
		{
			HeartbeatSubscriptions.Remove (w);
		}

		// *** Focus / Menu-Selection ***

		// Focused Widgets call this to invoke a main menu update
		public void UpdateMenus<T>(T sender) where T: class, IGuiMenuInterface
		{
			//Console.WriteLine ("UpdateMenus called {0}", Environment.TickCount);
			if (sender != null) {
				Messages.SendMessage (new EventMessage (sender, RootMessages.UpdateMenus, true));
			}
		}

		public void OnFocusChanged(Widget sender)
		{
			Messages.SendMessage (new EventMessage(sender, RootMessages.FocusChanged, true));
		}


		private Widget _selectedMenu = null;
		public Widget SelectedMenu 
		{ 
			get {
				return _selectedMenu;
			}
			set {
				if (value != null && !value.IsMenu) {
					this.LogWarning("A normal widget tries to register as a SelectedMenu");
					return;
				}
				if (_selectedMenu != value) {
					Widget prev = _selectedMenu;
					_selectedMenu = value;
					if (prev != null)
						prev.UpdateStyle();					
				}
			}
		}

		private Widget _focusedWidget = null;
		public Widget FocusedWidget 
		{ 
			get {
				return _focusedWidget;
			}
			set {
				if (value != null && value.IsMenu) {
					this.LogWarning("A Menu widget tries to register as FocusedWidget");
					return;
				}
				if (_focusedWidget != value) {					
					Widget prev = _focusedWidget;
					_focusedWidget = value;
					if (prev != null)
						prev.OnLostFocus ();
					if (_focusedWidget != null && _focusedWidget.CanFocus)
						_focusedWidget.OnGotFocus ();	
					OnFocusChanged (_focusedWidget);
				}
			}
		}
			
		// *** Tab-Key Selection ***
		public void SelectNextWidget()
		{
			try {
				Widget current = FocusedWidget;
				if (current == null) {
					this.TabIntoFirst ();
				} else {
					Container parent = current.Parent;
					Widget lastFocusedWidget = current;
					while (parent != null) {
						if (parent.SelectNextChild (lastFocusedWidget).TabIntoFirst ()) {
							Invalidate ();
							return;					
						}
						lastFocusedWidget = parent;
						parent = parent.Parent;
					}	
					if (parent == null) {
						if (!this.TabIntoFirst ()) {
							current.Parent.TabIntoFirst ();
						}
					}
				}	
			} catch (Exception ex) {
				ex.LogError ();
			} finally {
				Invalidate ();
			}
		}

		public void SelectPrevWidget()
		{
			try {
				Widget current = FocusedWidget;
				if (current == null) {
					this.TabIntoLast ();
				} else {
					Container parent = current.Parent;
					Widget lastFocusedWidget = current;
					while (parent != null) {
						if (parent.SelectPrevChild (lastFocusedWidget).TabIntoLast ()) {
							Invalidate ();
							return;					
						}
						lastFocusedWidget = parent;
						parent = parent.Parent;
					}
					if (parent == null) {
						if (!this.TabIntoLast ()) {
							current.Parent.TabIntoLast ();
						}
					}
				}	
			} catch (Exception ex) {
				ex.LogError ();
			} finally {
				Invalidate ();
			}				
		}

		// *** Context Menu Service ***

		public void CloseContextMenu()
		{		
			if (ContextMenuOverlay != null) {				
				RemoveChild (ContextMenuOverlay);
				ContextMenuOverlay.Dispose ();
				ContextMenuOverlay = null;
			}
			//this.Focus ();
		}

		public void ShowContextMenu(PointF location, IGuiMenuItem[] items)
		{			
			GuiMenu menu = new GuiMenu ("contextmenu");
			menu.AddRange (items);
			ShowContextMenu (location, menu);
		}		

		object contextMenuLock = new object ();
		public void ShowContextMenu(PointF location, IGuiMenu menu)
		{			
			lock (contextMenuLock) {
				if (ContextMenuOverlay != null) {									
					CloseContextMenu ();
				}

				ContextMenuOverlay = new SubMenuOverlay ("contextmenu", menu);
				ContextMenuOverlay.Closing += delegate {
					CloseContextMenu();
				};

				AddChild (ContextMenuOverlay);
				ContextMenuOverlay.SetBounds (GetContextMenuBounds (ContextMenuOverlay, location));
				ContextMenuOverlay.CanFocus = false;
				ContextMenuOverlay.Visible = true;
				ContextMenuOverlay.Selected = true;
				Invalidate ();
			}
		}

		RectangleF GetContextMenuBounds(SubMenuOverlay sub, PointF location)
		{						
			if (Width < 5 || Height < 5)
				return RectangleF.Empty;

			SizeF sz = sub.PreferredSize (CTX);

			float w = Math.Min (sz.Width, Width);
			float h = Math.Min (sz.Height, Height);

			float y = location.Y;

			RectangleF rsub = new RectangleF (
				location.X, 
				y, 
				w, 
				h);

			if (rsub.Right > Width)
				rsub.Offset (Width - rsub.Right, 0);

			if (rsub.Bottom > Height)
				rsub.Offset (0, Height - rsub.Bottom);

			return rsub;
		}

		// *** Cleanup ***
			
		protected override void CleanupManagedResources ()
		{
			if (Messages != null)
				Messages.Dispose ();

			//Overlays.DisposeListObjects ();	// istn't it already disposed by owner ??
			ClearOverlays();

			TooltipDelayAction.Dispose ();

            HeartbeatTimer.Dispose ();
			HeartbeatSubscriptions.Clear ();

			if (m_Tooltip != null)
				m_Tooltip.Dispose ();
			if (ContextMenuOverlay != null)
				ContextMenuOverlay.Dispose ();

			base.CleanupManagedResources();
		}

		protected override void CleanupUnmanagedResources ()
		{			
			if (!Overlays.IsNullOrEmpty()) {
				Overlays.DisposeListObjects ();
				Overlays.Clear ();
			}

			_focusedWidget = null;
			m_LastMouseDownWidget = null;
			m_LastMouseMoveWidget = null;
			base.CleanupUnmanagedResources ();
		}			
	}
}

