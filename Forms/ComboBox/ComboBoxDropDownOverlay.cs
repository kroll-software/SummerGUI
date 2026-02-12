using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;
using OpenTK;
using OpenTK.Input;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using KS.Foundation;

namespace SummerGUI
{
	public class DropDownWidgetStyle : WidgetStyle
	{
		public override void InitStyle ()
		{
			SetBackColor (Theme.Colors.Base3);
			SetForeColor (Theme.Colors.Base02);
			SetBorderColor (Theme.Colors.Base01);
		}
	}

	public class DropDownSelectedItemWidgetStyle : WidgetStyle
	{
		public override void InitStyle ()
		{
			SetBackColor (Theme.Colors.HighLightBlue);
			SetForeColor (Theme.Colors.White);
			SetBorderColor (Color.Empty);
		}
	}

	public class ComboBoxDropDownOverlay : OverlayContainer
	{		
		public event EventHandler<EventArgs> ItemSelected;
		public void OnItemSelected()
		{
			if (ItemSelected != null && !IsDisposed)
				ItemSelected (this, EventArgs.Empty);			
		}

		public ComboBoxDropDownOverlay ()
			: base ("dropdown", Docking.None, new DropDownWidgetStyle())
		{
			Styles.SetStyle (new DropDownSelectedItemWidgetStyle (), WidgetStates.Selected);			
			ScrollBars = ScrollBars.Vertical;			
			OverlayMode = OverlayModes.Overlay;
		}        
			
		protected override void LayoutChildren (IGUIContext ctx, RectangleF bounds)
		{				
			ComboBoxBase parent = Parent as ComboBoxBase;
			if (parent == null)
				return;
			
			base.LayoutChildren(ctx, bounds);			
			DocumentSize = new SizeF (bounds.Width, parent.Count * parent.ItemHeight);			
		}

		public int SelectedIndex { get; set; }

		public override void OnMouseMove (MouseMoveEventArgs e)
		{
			base.OnMouseMove (e);

			ComboBoxBase pb = Parent as ComboBoxBase;
			if (pb != null && pb.ItemHeight > 0) {
				SelectedIndex = (int)((e.Y - Bounds.Top + VScrollBar.Value) / pb.ItemHeight);
				Invalidate ();
			}
		}

        public override bool OnMouseWheel(MouseWheelEventArgs e)
        {
            if (!base.OnMouseWheel(e))
				return false;

			ComboBoxBase pb = Parent as ComboBoxBase;
			if (pb != null && pb.ItemHeight > 0) {
				SelectedIndex = (int)((e.Y - Bounds.Top + VScrollBar.Value) / pb.ItemHeight);
				Invalidate ();
			}
			
			return true;
        }        

		public override void OnClick(MouseButtonEventArgs e)
        {
            base.OnClick(e);

			RectangleF scrollbounds = this.Bounds;
			if (VScrollBar.IsVisibleEnabled)
				scrollbounds.Width -= VScrollBar.Width;

			if (e.X > scrollbounds.Right)
				return;

			ComboBoxBase pb = Parent as ComboBoxBase;
			if (pb != null && pb.ItemHeight > 0) {
				SelectedIndex = (int)((e.Y - Bounds.Top + VScrollBar.Value) / pb.ItemHeight);
				Invalidate ();
			}

			if (SelectedIndex < 0 || SelectedIndex >= pb.Items.Count)
				return;

			OnItemSelected();
			OnClose ();
        }
        	
		public override void OnPaint (IGUIContext ctx, RectangleF bounds)
		{			
			ComboBoxBase parent = Parent as ComboBoxBase;
			if (parent == null)
				return;			

			base.OnPaint(ctx, bounds);

			float scrollOffsetY = 0;
			float scrollWidth = 0;
			if (VScrollBar != null && VScrollBar.Visible)
			{				
				scrollWidth = VScrollBar.Width;
				scrollOffsetY = VScrollBar.Value;
			}

			float itemHeight = parent.ItemHeight;

			RectangleF clipRect = new RectangleF(bounds.Left, bounds.Top, bounds.Width - scrollWidth, bounds.Height);
			using (var clip = new ClipBoundClip(ctx, clipRect, false))
			{
				for (int i = 0; i < parent.Items.Count; i++) {
					RectangleF itemBounds = new RectangleF (bounds.Left, (i * itemHeight) + bounds.Top - scrollOffsetY, 
						bounds.Width - scrollWidth, itemHeight);

					if (i == SelectedIndex) {
						IWidgetStyle style = Styles.GetStyle (WidgetStates.Selected);
						ctx.FillRectangle (style.BackColorBrush, itemBounds);
						parent.DrawItem(ctx, itemBounds, parent.Items[i], style);
					} else {
						parent.DrawItem(ctx, itemBounds, parent.Items[i], Style);
					}					
				}
			}
		}        
			
		public void EnsureIndexVisible(int idx)
		{
			this.Invalidate ();
		}

		public void SeekIndex(int newIndex)
		{
			ComboBoxBase parent = Parent as ComboBoxBase;
			if (parent == null)
				return;

			if (newIndex < SelectedIndex && SelectedIndex == 0)
				return;

			if (newIndex > SelectedIndex && SelectedIndex == parent.Count - 1)
				return;

			SelectedIndex = Math.Max(0, Math.Min(newIndex, parent.Count - 1));
			EnsureIndexVisible (SelectedIndex);
		}

		public override bool OnKeyDown (KeyboardKeyEventArgs e)
		{				
			//if (base.OnKeyDown (e))
			//	return true;

			if (!IsFocused)
				return false;

			ComboBoxBase parent = Parent as ComboBoxBase;
			if (parent == null)
				return false;

			bool handled = false;

			switch (e.Key) {
			case Keys.Up:
				SeekIndex (SelectedIndex - 1);
				return true;
			case Keys.Down:
				SeekIndex (SelectedIndex + 1);
				return true;
			case Keys.PageUp:
				SeekIndex (SelectedIndex - (int)(Bounds.Height / parent.ItemHeight));
				return true;
			case Keys.PageDown:
				SeekIndex (SelectedIndex + (int)(Bounds.Height / parent.ItemHeight));
				return true;
			case Keys.Home:
				SeekIndex (0);
				return true;
			case Keys.End:
				SeekIndex (parent.Count - 1);
				return true;
			case Keys.Enter:
				if (SelectedIndex >= 0 && SelectedIndex < parent.Count) {					
					OnItemSelected();
					OnClose ();
					return true;
				}
				break;
			case Keys.Escape:
				OnClose ();
				return true;
			}
				
			return handled;
		}
	}
}

