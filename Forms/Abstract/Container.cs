using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using KS.Foundation;

namespace SummerGUI
{		
	public abstract class Container : Widget
	{		
		protected Container (string name) : this(name, Docking.None, null) {}
		protected Container (string name, Docking dock, IWidgetStyle style)
			: base(name, dock, style)
		{			
			Children = new ChildCollection (this);
		}

		public ChildCollection Children { get; private set; }

		public T AddChild<T>(T child) where T : Widget
		{			
			if (child == null) {				
				this.LogError("Child must not be null.");
				return null;
			}

			try {
				OnAddChild(child);
				Children.Add (child);
				if (child.IsOverlay) {
					RootContainer root = Root ?? child.Root;
					if (root != null)
						root.RegisterOverlay (child);				
				}
			} catch (Exception ex) {
				ex.LogError ();
			}

			return (T)child;
		}

		protected virtual void OnAddChild(Widget child)
		{
			ResetCachedLayout ();
		}

		public virtual void RemoveChild(Widget child)
		{
			if (child == null)
				return;

			try {
				// cleanup all necessary entries from the Root Container
				RootContainer root = Root ?? child.Root;
				if (root != null)
					root.RemoveWidget (child);
				if (Children.Remove (child))
					OnChildRemoved(child);
			} catch (Exception ex) {
				ex.LogError ();
			}
		}

		protected virtual void OnChildRemoved(Widget child)
		{
			ResetCachedLayout ();
		}

		protected override void OnRootChanged ()
		{
			if (IsDisposed)
				return;
			base.OnRootChanged ();
			RootContainer root = Root;
			for (int i = 0; i < Children.Count; i++) {
				Children [i].Root = root;
			}
		}

		// Called when a child changed it's ZIndex or Docking
		public virtual void ReOrganize()
		{
			Children.NaturalMergeSort ();
			ResetCachedLayout ();
		}

		// ToDo: Do it unsafe
		public virtual Widget ChildByID(int id)
		{
			for (int i = 0; i < Children.Count; i++) {
				if (Children [i].ID == id)
					return Children [i];
			}
			return null;
		}

		// ToDo: Do it unsafe  // never called
		public virtual Widget ChildByName(string name)
		{
			for (int i = 0; i < Children.Count; i++) {
				if (Children [i].Name == name)
					return Children [i];
			}
			return null;
		}

		protected bool m_CanFocus;
		public override bool CanFocus {
			get {
				if (IsMenu)
					return false;
				return m_CanFocus || Children.Any(c => c.CanFocus);
			}
			set {
				m_CanFocus = value;
			}
		}

		public override void Focus ()
		{					
			if (!IsVisibleEnabled)
				return;		
			if (IsMenu) {
				Selected = true;
				return;
			}

			if (m_CanFocus) {
				IsFocused = true;
				return;
			}			

			Widget w = this.TabSupportingChildren ().FirstOrDefault ();
			if (w != null) {
				w.Focus ();
			} else if (CanFocus) {
				IsFocused = true;
			}
		}

		public virtual Widget SelectFirstChild()
		{
			return this.TabSupportingChildren().FirstOrDefault ();
		}

		public virtual Widget SelectPrevChild(Widget current)
		{
			if (current == null)
				return null;
			IList<Widget> list = this.TabSupportingChildren ().ToList ();
			int idx = list.IndexOf (current);
			if (idx > 0)
				return list [idx - 1];
			return null;
		}

		public virtual Widget SelectNextChild(Widget current)
		{
			if (current == null)
				return null;
			IList<Widget> list = this.TabSupportingChildren ().ToList ();
			int idx = list.IndexOf (current);
			if (idx < list.Count - 1)
				return list [idx + 1];
			return null;
		}

		public virtual Widget SelectLastChild()
		{
			return this.TabSupportingChildren().LastOrDefault();
		}			
			
		// ToDo: Do it unsafe
		public override RectangleF ClientRectangle {
			get {
				RectangleF r = base.ClientRectangle;	// = padded bounds
				for (int i = 0; i < Children.Count; i++) {
					Widget child = Children [i];
					if (child != null && child.Visible && !child.IsOverlay) {
						RectangleF cmb = child.MarginBounds;	// Child-Margin-Bounds

						switch (child.Dock) {
						case Docking.Top:
							r.Height -= cmb.Height;
							r.Y += cmb.Height;
							break;

						case Docking.Left:
							r.Width -= cmb.Width;
							r.X += cmb.Width;
							break;

						case Docking.Right:
							r.Width -= cmb.Width;
							break;

						case Docking.Bottom:
							r.Height -= cmb.Height;
							break;
						}
					}
				}

				return r;
			}
		}
			
		public override bool OnMouseWheel (MouseWheelEventArgs e)
		{						
			foreach (Widget c in Children)
				if (c.OnMouseWheel (e)) {					
					return true;
				}

			if (base.OnMouseWheel (e)) {				
				return true;
			}

			return false;
		}

		public override bool OnKeyDown (KeyboardKeyEventArgs e)
		{
			foreach (Widget c in Children)
				if (c.Visible && c.Enabled && c.OnKeyDown (e)) {			
					return true;
				}

			if (base.OnKeyDown (e)) {				
				return true;
			}

			return false;
		}

		public override bool OnKeyPress (KeyPressEventArgs e)
		{
			foreach (Widget c in Children)
				if (c.Visible && c.Enabled && c.OnKeyPress (e)) {			
					return true;
				}

			if (base.OnKeyPress (e)) {				
				return true;
			}

			return false;
		}

		public override Widget HitTest(float x, float y)
		{		
			if (base.HitTest (x, y) == null)
				return null;

			if (Children.Count == 0)
				return this;

			for (int i = 0; i < Children.Count; i++) {
				Widget child = Children [i];
				if (child != null) {
					Widget c = child.HitTest (x, y);
					if (c != null)
						return c;
				}
			}

			return this;
		}			
			
		public event EventHandler<EventArgs> AfterLayout;
		public virtual void OnAfterLayout(IGUIContext ctx, RectangleF bounds)
		{
			if (AfterLayout != null)
				AfterLayout (this, EventArgs.Empty);
		}			


		/// <summary>
		/// Call base.OnLayout() at last, after you Layouted your control
		/// </summary>
		/// <param name = "ctx"></param>
		/// <param name="bounds">Bounding box.</param>
		public override void OnLayout(IGUIContext ctx, RectangleF bounds)
		{								
			if (IsLayoutSuspended || !Visible)
				return;

			base.OnLayout (ctx, bounds);
			LayoutChildren (ctx, PaddingBounds);	// Base = Bounds with Padding		
			OnAfterLayout (ctx, bounds);
		}
			
		/// <summary>
		/// Layouts the children. Gives derived classes a chance to have their own logic
		/// </summary>
		/// <param name="ctx">Context.</param>
		/// <param name="bounds">Bounds.</param>
		protected virtual void LayoutChildren(IGUIContext ctx, RectangleF bounds)
		{
			if (this.Children.Count > 0) {				
				RectangleF r = bounds;
				// iterate forward by ZIndex
				for (int i = 0; i < Children.Count; i++)
				{
					Widget child = Children [i];

					if (child.Visible) {
						LayoutChild(ctx, child, r);
						if (child.IsOverlay || child.Bounds.IsEmpty)
							continue;

						RectangleF cmb = child.MarginBounds;	// Child-Margin-Bounds

						switch (child.Dock) {
						case Docking.Top:							
							r.Height -= cmb.Height;
							r.Y += cmb.Height;
							break;
						case Docking.Left:
							r.Width -= cmb.Width;
							r.X += cmb.Width;
							break;
						case Docking.Right:
							r.Width -= cmb.Width;
							// Definitiv jedenfalls nicht immer
							// sonst wird r.X schnell negativ
							//r.X -= child.Margin.Width;	
							break;
						case Docking.Bottom:
							r.Height -= cmb.Height;
							//r.Y -= child.Margin.Height;	// s.o. Definitiv jedenfalls nicht immer
							break;
						case Docking.Fill:
							// ok, we'll take that one
							break;
						}							
					}
				}
			}
		}

		/// <summary>
		/// Layouts the child. Gives derived classes a chance to have their own logic
		/// </summary>
		/// <param name = "child"></param>
		/// <param name="ctx">Context.</param>
		/// <param name="bounds">Bounds.</param>
		protected virtual void LayoutChild(IGUIContext ctx, Widget child, RectangleF bounds)
		{
			child.OnLayout (ctx, bounds);
		}
			
		// RECURSIVE Update Function
		public override void Update(IGUIContext ctx)
		{
			using (var clip = new ClipBoundClip (ctx, Bounds, true)) {
				if (!clip.IsEmptyClip) {
					base.Update (ctx);
					// GEMALT WIRD RÜCKWÄRTS
					for (int i = Children.Count - 1; i >= 0; i--) {
						Widget child = Children [i];						
						if (child != null && child.Visible && !child.IsOverlay) {
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
		}	


		public virtual void Clear()
		{
			if (Children == null)
				return;
			Children.Clear ();
		}

		public override void OnUpdateTheme (IGUIContext ctx)
		{
			base.OnUpdateTheme (ctx);
			foreach (Widget c in Children)
				c.OnUpdateTheme (ctx);
		}			

		protected override void CleanupManagedResources ()
		{
			if (Children != null) {
				for (int i = 0; i < Children.Count; i++) {
					Widget c = Children [i];
					if (c != null)
						c.Dispose ();
				}
				Children.Clear ();
				Children.Parent = null;
			}
			base.CleanupManagedResources();
		}			
	}		
}

