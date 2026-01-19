using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using KS.Foundation;

namespace SummerGUI.Splitting
{
	public abstract class SplitterBase : Widget
	{
		public SplitOrientation Orientation { get; private set; }
		public bool Fixed { get; set; }

		[DpiScalable]
		public float SplitterWidth { get; set; }

		/// <summary>
		/// [0..1] : relative distance in %
		/// >= 1 : absolute distance value
		/// negative values are allowed and mean: Distance from right
		/// </summary>
		/// <value>The distance.</value>
		//[DpiScalable]
		// see customized scaling below
		//[DpiScalable]

		private float m_Distance;
		public float Distance 
		{ 
			get {
				return m_Distance;
			}
			set {
				// attempts to set Distance to zero should fail, because the GUI would be unusable.
				if (m_Distance != value && Math.Abs (value) > 0.001) {					
					m_Distance = value;
					OnDistanceChanged ();
				}
			}
		}

		public virtual void OnDistanceChanged()
		{
			Update (true);
		}

		[DpiScalable]
		public float MinDistanceNear  { get; set; }

		[DpiScalable]
		public float MinDistanceFar  { get; set; }

		protected SplitterBase(string name, SplitOrientation orientation)
			: this(name, orientation, new SplitContainerSplitterStyle ()) {}

		protected SplitterBase(string name, SplitOrientation orientation, IWidgetStyle style)
			: base(name, Docking.None, style)
		{			
			//Styles.SetStyle (new SplitContainerSplitterStyle (), WidgetStates.Pressed);
			Styles.SetStyle (style, WidgetStates.Pressed);
			ZIndex = 1100;	// must be above Scrollbars!
			Orientation = orientation;
			SplitterWidth = 4;
			MinDistanceNear = 100;		
			MinDistanceFar = 50;
			Distance = 0.25f;
		}

		protected override void OnScaleWidget (IGUIContext ctx, float absoluteScaleFactor, float relativeScaleFactor)
		{
			base.OnScaleWidget (ctx, absoluteScaleFactor, relativeScaleFactor);

			// only scale if we have an absolute distance (Distance > 1)
			float absDist = Math.Abs(Distance);
			if (absDist > 1 && absDist * relativeScaleFactor > 1) {
				Distance *= relativeScaleFactor;
			}
		}

		protected bool IsDragging;
		public override void OnMouseDown (MouseButtonEventArgs e)
		{
			IsDragging = true;
			base.OnMouseDown (e);
		}

		public override void OnMouseUp (MouseButtonEventArgs e)
		{
			base.OnMouseUp (e);
			IsDragging = false;
		}
	}

	public class HorizontalSplitter : SplitterBase
	{
		public HorizontalSplitter() : this("splitter", new SplitContainerSplitterStyle()) {}
		public HorizontalSplitter(string name) : this(name, new SplitContainerSplitterStyle()) {}
		public HorizontalSplitter(string name, IWidgetStyle style) : base(name, SplitOrientation.Horizontal, style) {}

		public override SizeF PreferredSize (IGUIContext ctx, SizeF proposedSize)
		{
			return new SizeF(proposedSize.Width, SplitterWidth);
		}			

		public override void OnLayout (IGUIContext ctx, RectangleF bounds)
		{			
			if (Distance > 0) {
				if (Distance < 1) {
					float oldDistance = Distance;
					float distance = Math.Max(bounds.Height * Distance, MathF.Min(bounds.Height - MinDistanceFar, bounds.Height * Distance));
					float y = Math.Max (bounds.Top + MinDistanceNear, bounds.Top + distance);
					SetBounds (new RectangleF (bounds.Left, y, bounds.Width, SplitterWidth));
					Distance = Math.Max(oldDistance, distance / bounds.Height);
				} else {
					float oldDistance = Distance;
					float distance = Math.Max(MinDistanceNear, Math.Min(bounds.Height - MinDistanceFar, Distance));
					SetBounds (new RectangleF (bounds.Left, bounds.Top + distance, bounds.Width, SplitterWidth));
					Distance = Math.Max(1.05f, Math.Max(oldDistance, distance));
				}
			} else if (Distance < 0) {
				if (Distance > -1) {
					float oldDistance = Distance;
					float distance = (Math.Min (Distance * bounds.Height, Math.Max(MinDistanceNear, Math.Abs(Distance) * bounds.Height)));
					float y = Math.Max (bounds.Top + MinDistanceNear, bounds.Bottom + distance);
					SetBounds (new RectangleF (bounds.Left, y, bounds.Width, SplitterWidth));
					Distance = Math.Max(-0.95f, Math.Min(-0.05f, Math.Min(oldDistance, distance / bounds.Height)));
				} else {
					float oldDistance = Distance;
					float distance = -(Math.Max (MinDistanceFar, Math.Abs(Distance)));
					float y = Math.Max (bounds.Top + MinDistanceNear, bounds.Bottom + distance);
					SetBounds (new RectangleF (bounds.Left, y, bounds.Width, SplitterWidth));
					Distance = Math.Min(-1.05f, Math.Min(oldDistance, distance));
				}
			} else {
				// Distance == 0
				SetBounds (new RectangleF (bounds.Left, bounds.Top, bounds.Width, SplitterWidth));
			}
		}

		public override void OnMouseEnter (IGUIContext ctx)
		{			
			if (Visible && Enabled && !Fixed && ZIndex > 0)
				Cursor = Cursors.HSplit;
			else
				Cursor = Cursors.Default;

			base.OnMouseEnter (ctx);
		}

		public override void OnMouseMove (MouseMoveEventArgs e)
		{						
			if (!IsDragging || WidgetState != WidgetStates.Pressed)
				return;
			
			if (Visible && Enabled && !Fixed && ZIndex > 0) {
				float h = Parent.Bounds.Height;
				float y = e.Y - (LastMouseDownMousePosition.Y - LastMouseDownUpperLeft.Y) - Parent.Bounds.Top;

				if (Distance > 0) {
					if (Distance < 1) {						
						float distance = Math.Max (MinDistanceNear / h, y / h);
						Distance = Math.Max(0.05f, Math.Min(0.95f, distance));
					} else {
						float distance = Math.Max (MinDistanceNear, y);
						Distance = Math.Max(1.05f, distance);
					}						
				} else if (Distance < 0) {
					if (Distance > -1) {						
						float distance = -(Math.Max (MinDistanceFar / h, (h - y) / h));
						Distance = Math.Min(-0.05f, Math.Max(-0.95f, distance));
					} else {
						float distance = -(Math.Max (MinDistanceFar, (h - y)));
						Distance = Math.Min(-1.05f, distance);
					}
				}
			}

			//base.OnMouseMove (e);
		}
	}

	public class VerticalSplitter : SplitterBase
	{
		public VerticalSplitter() : this("splitter", new SplitContainerSplitterStyle()) {}
		public VerticalSplitter(string name) : this(name, new SplitContainerSplitterStyle()) {}
		public VerticalSplitter(string name, IWidgetStyle style) : base(name, SplitOrientation.Vertical, style) {}

		public override SizeF PreferredSize (IGUIContext ctx, SizeF proposedSize)
		{
			return new SizeF(SplitterWidth, proposedSize.Height);
		}

		public override void OnLayout (IGUIContext ctx, RectangleF bounds)
		{
			if (bounds.Width < 1)
				return;

			// Base layouts on Parent.ClientRectangle
			// we have a special layout and no children
			// So this is a rare spacial case where we don't call the base class
			// base.OnLayout (ctx, bounds);


			if (Distance > 0) {
				if (Distance < 1) {
					float oldDistance = Distance;
					float distance = Math.Max(bounds.Width * Distance, MathF.Min(bounds.Width - MinDistanceFar, bounds.Width * Distance));
					float x = MathF.Max (bounds.Left + MinDistanceNear, bounds.Left + distance);
					SetBounds (new RectangleF (x, bounds.Top, SplitterWidth, bounds.Height));
					Distance = MathF.Max(oldDistance, distance / bounds.Width);

					//Console.WriteLine ("Distance: {0}", Distance);
				}
				else {
					float oldDistance = Distance;
					float distance = MathF.Max(MinDistanceNear, MathF.Min(bounds.Width - MinDistanceFar, Distance));
					SetBounds (new RectangleF (bounds.Left + distance, bounds.Top, SplitterWidth, bounds.Height));
					Distance = MathF.Max(1f, MathF.Max(oldDistance, distance));
				}
			} else if (Distance < 0) {				
				if (Distance > -1) {					
					float oldDistance = Distance;
					float distance = MathF.Min (Distance * bounds.Width, MathF.Max(MinDistanceNear, MathF.Abs(Distance) * bounds.Width));
					float x = MathF.Max (bounds.Left + MinDistanceNear, bounds.Right + distance);
					SetBounds (new RectangleF (x, bounds.Top, SplitterWidth, bounds.Height));
					Distance = MathF.Max(-0.95f, MathF.Min(-0.05f, MathF.Min(oldDistance, distance / bounds.Width)));

					//Console.WriteLine ("Distance: {0}", Distance);
				}
				else {					
					float oldDistance = Distance;
					float distance = - MathF.Max (MinDistanceFar, MathF.Abs(Distance));
					float x = MathF.Max (bounds.Left + MinDistanceNear, bounds.Right + distance);
					SetBounds (new RectangleF (x, bounds.Top, SplitterWidth, bounds.Height));
					Distance = MathF.Min(-1.05f, MathF.Min(oldDistance, distance));
				}
			} else {
				// damit wäre er verschwunden, und das wollen wir nicht..
				// Distance == 0
				//SetBounds (new RectangleF (bounds.Left, bounds.Top, SplitterWidth, bounds.Height));
			}
		}

		public override void OnMouseEnter (IGUIContext ctx)
		{			
			if (Visible && Enabled && !Fixed && ZIndex > 0)
				Cursor = Cursors.VSplit;
			else
				Cursor = Cursors.Default;

			base.OnMouseEnter (ctx);
		}

		public override void OnMouseMove (MouseMoveEventArgs e)
		{			
			if (!IsDragging || WidgetState != WidgetStates.Pressed)
				return;
					
			if (Visible && Enabled && !Fixed && ZIndex > 0) {
				float w = Parent.Bounds.Width;
				float x = e.X - (LastMouseDownMousePosition.X - LastMouseDownUpperLeft.X) - Parent.Bounds.Left;

				if (Distance > 0) {
					if (Distance < 1) {						
						float distance = Math.Max (MinDistanceNear / w, x / w);
						Distance = Math.Max(0.05f, Math.Min(0.95f, distance));
					} else {
						float distance = Math.Max (MinDistanceNear, x);
						Distance = Math.Max(1.05f, distance);
					}
				} else if (Distance < 0) {
					if (Distance > -1) {						
						float distance = -(Math.Max (MinDistanceFar / w, (w - x) / w));
						Distance = Math.Min(-0.05f, Math.Max(-0.95f, distance));
					} else {
						float distance = -(Math.Max (MinDistanceFar, (w - x)));
						Distance = Math.Min(-1.05f, distance);
					}
				}
			}				

			//base.OnMouseMove (e);
		}
	}

	public class SplitContainerTransparentSplitterStyle : WidgetStyle
	{
		public override void InitStyle ()
		{
			//SetBackColor (Color.FromArgb(220, Theme.CurrentTheme.StatusBar.BackColor));
			SetBackColor (Color.Empty);
			SetForeColor(Color.Empty);
			SetBorderColor(Color.Empty);
		}
	}

	public class SplitContainerSplitterStyle : WidgetStyle
	{
		public override void InitStyle ()
		{
			//SetBackColor (Color.FromArgb(220, Theme.CurrentTheme.StatusBar.BackColor));
			SetBackColor (Theme.CurrentTheme.StatusBar.BackColor);
			SetForeColor(Color.Empty);
			SetBorderColor(Color.Empty);
		}
	}

	/***
	public class SplitContainerPanelStyle : WidgetStyle
	{
		public override void InitStyle ()
		{
			SetBackColor (Color.Empty);
			SetForeColor(Color.Empty);
			//SetBorderColor(Theme.Colors.Base00);
			SetBorderColor(Color.Empty);
		}
	}
	***/

	public class SplitContainerPanelStyle : GradientWidgetStyle
	{
		public override void InitStyle ()
		{
			SetBackColor (Theme.Colors.Base01, Theme.Colors.Base02);
			SetForeColor(Color.Empty);
			//SetBorderColor(Theme.Colors.Base00);
			SetBorderColor(Color.Empty);
		}
	}

	public class SplitContainerContentPanelStyle : WidgetStyle
	{
		public override void InitStyle ()
		{
			SetBackColor (Theme.Colors.White);
			SetForeColor(Theme.Colors.Base02);
			SetBorderColor(Theme.Colors.Base00);
		}
	}

	public class SplitContainerStyle : WidgetStyle
	{
		public override void InitStyle ()
		{
			SetBackColor (Color.Empty);
			SetForeColor(Color.Empty);
			SetBorderColor(Theme.Colors.Base00);
		}
	}
}

