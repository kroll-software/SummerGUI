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
	public class BackgroundImageSettings
	{
		public TextureImage Image { get; set; }
		public ImageSizeModes SizeMode { get; set; } = ImageSizeModes.None;
		public Alignment HAlign { get; set; } = Alignment.Center;
		public Alignment VAlign { get; set; } = Alignment.Center;
		public Padding Padding { get; set; } = Padding.Empty;
		public float ScaleFactor { get; set; } = 1.0f;

		// Optional: Ein Konstruktor für schnelles Zuweisen
		public BackgroundImageSettings(TextureImage image)
		{
			Image = image;
		}
	}

	public abstract class Container : Widget
	{		
		public BackgroundImageSettings BackgroundImage { get; set; }

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

		protected RectangleF GetBackgroundDestRect(RectangleF bounds)
		{           
			if (BackgroundImage == null || BackgroundImage.Image == null || BackgroundImage.Image.Width <= 0 || BackgroundImage.Image.Height <= 0)
				return RectangleF.Empty;           

			// 1. Canvas berechnen: Bounds minus Padding und Border
			// Ich nehme an, Style.Border und Padding sind in der Basisklasse/Context verfügbar
			float border = Style.Border;
			float borderX2 = border * 2f;

			RectangleF canvas = new RectangleF (
				bounds.Left + Padding.Left + BackgroundImage.Padding.Left + border, 
				bounds.Top + Padding.Top + BackgroundImage.Padding.Top + border, 
				bounds.Width - Padding.Width - BackgroundImage.Padding.Width - borderX2, 
				bounds.Height - Padding.Height - BackgroundImage.Padding.Height - borderX2
			);

			if (canvas.Width <= 0 || canvas.Height <= 0)
				return RectangleF.Empty;

			float imgW = (float)BackgroundImage.Image.Width;
			float imgH = (float)BackgroundImage.Image.Height;
			
			float destW = imgW;
			float destH = imgH;			
			float zoom = (BackgroundImage.ScaleFactor <= 0) ? 1.0f : BackgroundImage.ScaleFactor;

			// 2. Größe basierend auf BackgroundImageSizeMode
			switch (BackgroundImage.SizeMode) 
			{
				case ImageSizeModes.ShrinkToFitHorizontal:
					if (imgW > canvas.Width) {
						zoom = canvas.Width / imgW;
						destW = imgW * zoom;
						destH = imgH * zoom;
					} 
					break;
				case ImageSizeModes.ShrinkToFitVertical:
					if (imgH > canvas.Height) {
						zoom = canvas.Height / imgH;
						destW = imgW * zoom;
						destH = imgH * zoom;
					}
					break;
				case ImageSizeModes.ShrinkToFit:
					if (imgW > canvas.Width || imgH > canvas.Height) {
						zoom = Math.Min(canvas.Width / imgW, canvas.Height / imgH);
						destW = imgW * zoom;
						destH = imgH * zoom;
					}
					break;
				case ImageSizeModes.AlwaysFit:
					zoom = Math.Min(canvas.Width / imgW, canvas.Height / imgH);
					destW = imgW * zoom;
					destH = imgH * zoom;            
					break;
				case ImageSizeModes.Stretch:
					destW = canvas.Width;
					destH = canvas.Height;
					break;
				case ImageSizeModes.TileHorizontal: destW = canvas.Width; break;
				case ImageSizeModes.TileVertical:   destH = canvas.Height; break;
				case ImageSizeModes.TileAll:
					destW = canvas.Width;
					destH = canvas.Height;
					break;
				case ImageSizeModes.None:
				case ImageSizeModes.AutoSize:
				default:
					// Bleibt Originalgröße
					break;
			}

			// 3. Alignment berechnen
			float destX = canvas.X; 
			float destY = canvas.Y;

			if (BackgroundImage.HAlign == Alignment.Center)
				destX += Math.Max(0, (canvas.Width - destW) / 2f);
			else if (BackgroundImage.HAlign == Alignment.Far)
				destX += canvas.Width - destW;

			if (BackgroundImage.VAlign == Alignment.Center)
				destY += Math.Max(0, (canvas.Height - destH) / 2f);
			else if (BackgroundImage.VAlign == Alignment.Far)
				destY += canvas.Height - destH;

			return new RectangleF(destX, destY, destW, destH);
		}

        public override void OnPaintBackground(IGUIContext ctx, RectangleF bounds)
		{
			// Erst den Standard-Hintergrund (Farbe) zeichnen
			base.OnPaintBackground(ctx, bounds);

			if (BackgroundImage?.Image != null)
			{
				//RectangleF uvRect = new RectangleF(0, 0, 1, 1);

				RectangleF destRect = GetBackgroundDestRect(bounds);
				if (destRect.Width > 0 && destRect.Height > 0)
				{
					// Prüfen, ob wir kacheln müssen
					if (BackgroundImage.SizeMode == ImageSizeModes.TileAll || 
						BackgroundImage.SizeMode == ImageSizeModes.TileHorizontal || 
						BackgroundImage.SizeMode == ImageSizeModes.TileVertical)
					{
						// Falls dein IGUIContext eine Tile-Funktion hat, nutze diese.
						// Ansonsten hier ein generischer Ansatz über UV-Mapping:
						float uvW = (BackgroundImage.SizeMode == ImageSizeModes.TileHorizontal || BackgroundImage.SizeMode == ImageSizeModes.TileAll) 
									? destRect.Width / BackgroundImage.Image.Width : 1.0f;
						float uvH = (BackgroundImage.SizeMode == ImageSizeModes.TileVertical || BackgroundImage.SizeMode == ImageSizeModes.TileAll) 
									? destRect.Height / BackgroundImage.Image.Height : 1.0f;
						
						RectangleF uvRect = new RectangleF(0, 0, uvW, uvH);
						
						//BackgroundImage.Image.Paint (ctx, destRect);
						BackgroundImage.Image.Paint(ctx, destRect, uvRect, tile: true);
					}
					else
					{
						// Normales Zeichnen (Stretch oder Aligned)
						BackgroundImage.Image.Paint (ctx, destRect);
					}
				}
			}
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

        protected override void CleanupUnmanagedResources()
        {
			if (BackgroundImage != null && BackgroundImage.Image != null)
			{
				BackgroundImage.Image.Dispose();
				BackgroundImage.Image = null;
				BackgroundImage = null;
			}
            base.CleanupUnmanagedResources();
        }
	}		
}

