using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using KS.Foundation;

namespace SummerGUI
{
	/// <summary>
	/// Scrollable list that shows every <see cref="ColorItem"/> of the hosting
	/// <see cref="ColorComboBox"/>. It is a <see cref="ScrollableContainer"/> so the
	/// framework provides the vertical scrollbar for free; all item painting is
	/// delegated to the <see cref="ComboBoxBase.DrawItem"/> of the owning combo box,
	/// resolved as <c>Parent.Parent</c> (Parent = ColorComboBoxDropDown).
	/// Mirrors the scroll + paint layout of
	/// <see cref="ComboBoxDropDownOverlay"/>, but lives inside
	/// <see cref="ColorComboBoxDropDown"/>.
	/// </summary>
	public class ColorComboBoxDropDownList : ScrollableContainer, IComboBoxDropDown
	{
		public ColorComboBoxDropDownList(string name)
			: base(name, Docking.Fill, new DropDownWidgetStyle())
		{
			ScrollBars = ScrollBars.Vertical;
			Styles.SetStyle(new DropDownSelectedItemWidgetStyle(), WidgetStates.Selected);
			AutoScroll = true;
			CanFocus = true;

		}

		private int m_SelectedIndex = -1;

		/// <summary>
		/// The item currently highlighted in the drop-down list.
		/// Ensures the item stays visible via the scroll bar (see
		/// <see cref="OnLayout"/>) and requests a (re)layout so it runs.
		/// </summary>
		public int SelectedIndex
		{
			get { return m_SelectedIndex; }
			set
			{
				m_SelectedIndex = value;
				EnsureIndexVisible(m_SelectedIndex);
				Invalidate();
			}
		}

		// The scroll bar range (Maximum) is established in base.OnLayout's
		// SetUpScrollbars — so we may scroll to the selected item AFTER that.
		// Doing it on every layout pass is deterministic, regardless of whether
		// the index was set before or after the first layout (it was set, at
		// worst, right before a later pass), and it also keeps the selection in
		// view when the dropdown is resized.
		public override void OnLayout(IGUIContext ctx, RectangleF bounds)
		{
			base.OnLayout(ctx, bounds);
			EnsureIndexVisible(m_SelectedIndex);
		}

		public event EventHandler<EventArgs> ItemSelected;
		public void OnItemSelected()
		{
			if (ItemSelected != null && !IsDisposed)
				ItemSelected(this, EventArgs.Empty);
		}

		private ColorComboBox ComboBox
		{
			get { return Parent?.Parent as ColorComboBox; }
		}

		protected override void LayoutChildren(IGUIContext ctx, RectangleF bounds)
		{
			base.LayoutChildren(ctx, bounds);
			ColorComboBox combobox = ComboBox;
			if (combobox != null && combobox.ItemHeight > 0)
			{
				DocumentSize = new SizeF(bounds.Width, combobox.Count * combobox.ItemHeight);
			}
		}

		private int IndexAt(float y)
		{
			ColorComboBox combobox = ComboBox;
			if (combobox == null || combobox.ItemHeight <= 0)
				return -1;
			return (int)((y - Bounds.Top + VScrollBar?.Value) / combobox.ItemHeight);
		}

		public override void OnMouseMove(MouseMoveEventArgs e)
		{
			base.OnMouseMove(e);
			ColorComboBox combobox = ComboBox;
			if (combobox != null && combobox.ItemHeight > 0)
			{
				int idx = IndexAt(e.Y);
				if (idx != SelectedIndex)
				{
					SelectedIndex = idx;
					Invalidate();
				}
			}
		}

		public override bool OnMouseWheel(MouseWheelEventArgs e)
		{
			if (!base.OnMouseWheel(e))
				return false;
			ColorComboBox combobox = ComboBox;
			if (combobox != null && combobox.ItemHeight > 0)
			{
				SelectedIndex = IndexAt(e.Y);
				Invalidate();
			}
			return true;
		}

		public override void OnClick(MouseButtonEventArgs e)
		{
			base.OnClick(e);

			RectangleF scrollbounds = this.Bounds;
			if (VScrollBar != null && VScrollBar.IsVisibleEnabled)
				scrollbounds.Width -= VScrollBar.Width;
			if (e.X > scrollbounds.Right)
				return;

			ColorComboBox combobox = ComboBox;
			if (combobox == null || combobox.ItemHeight <= 0)
				return;

			int idx = IndexAt(e.Y);
			SelectedIndex = idx;
			Invalidate();
			if (idx < 0 || idx >= combobox.Items.Count)
				return;
			OnItemSelected();
			OnClose();
		}

		public override void OnPaint(IGUIContext ctx, RectangleF bounds)
		{
			ColorComboBox combobox = ComboBox;
			if (combobox == null)
				return;

			base.OnPaint(ctx, bounds);

			float scrollOffsetY = 0;
			float scrollWidth = 0;
			if (VScrollBar != null && VScrollBar.Visible)
			{
				scrollWidth = VScrollBar.Width;
				scrollOffsetY = VScrollBar.Value;
			}

			float itemHeight = combobox.ItemHeight;
			if (itemHeight <= 0)
				return;

			RectangleF clipRect = new RectangleF(bounds.Left, bounds.Top, bounds.Width - scrollWidth, bounds.Height);
			using (var clip = new ClipBoundClip(ctx, clipRect, false))
			{
				for (int i = 0; i < combobox.Items.Count; i++)
				{
					RectangleF itemBounds = new RectangleF(bounds.Left, (i * itemHeight) + bounds.Top - scrollOffsetY,
						bounds.Width - scrollWidth, itemHeight);

					if (i == SelectedIndex)
					{
						IWidgetStyle style = Styles.GetStyle(WidgetStates.Selected);
						ctx.FillRectangle(style.BackColorBrush, itemBounds);
						combobox.DrawItem(ctx, itemBounds, combobox.Items[i], style);
					}
					else
					{
						combobox.DrawItem(ctx, itemBounds, combobox.Items[i], Style);
					}
				}
			}
		}

		private void SeekIndex(int newIndex)
		{
			ColorComboBox combobox = ComboBox;
			if (combobox == null)
				return;
			SelectedIndex = Math.Max(0, Math.Min(newIndex, combobox.Count - 1));
			EnsureIndexVisible(SelectedIndex);
			Invalidate();
		}

		/// <summary>
		/// Scrolls the vertical bar (if any) so the given item is visible.
		/// No-op when the item already fits into the viewport.
		/// </summary>
		public void EnsureIndexVisible(int index)
		{
			ColorComboBox combobox = ComboBox;
			if (combobox == null || index < 0 || index >= combobox.Count)
				return;

			VerticalScrollBar vsb = VScrollBar;
			if (vsb == null || !vsb.IsVisibleEnabled)
				return;

			float itemHeight = combobox.ItemHeight;
			float viewportHeight = Bounds.Height;
			if (itemHeight <= 0 || viewportHeight <= 0)
				return;

			float itemTop = index * itemHeight;
			float itemBottom = itemTop + itemHeight;
			float viewTop = vsb.Value;
			float viewBottom = viewTop + viewportHeight;

			if (itemTop < viewTop)
				vsb.Value = itemTop;
			else if (itemBottom > viewBottom)
				vsb.Value = itemBottom - viewportHeight;
		}
		// (ensure-scroll is armed by the SelectedIndex setter / on creation,
		//  and fired from OnLayout once the scroll range is valid.)

		public override bool OnKeyDown(KeyboardKeyEventArgs e)
		{
			if (!IsFocused)
				return false;
			ColorComboBox combobox = ComboBox;
			if (combobox == null)
				return false;

			switch (e.Key)
			{
				case Keys.Up:
					SeekIndex(SelectedIndex - 1);
					return true;
				case Keys.Down:
					SeekIndex(SelectedIndex + 1);
					return true;
				case Keys.PageUp:
					SeekIndex(SelectedIndex - (int)(Bounds.Height / combobox.ItemHeight));
					return true;
				case Keys.PageDown:
					SeekIndex(SelectedIndex + (int)(Bounds.Height / combobox.ItemHeight));
					return true;
				case Keys.Home:
					SeekIndex(0);
					return true;
				case Keys.End:
					SeekIndex(combobox.Count - 1);
					return true;
				case Keys.Enter:
					if (SelectedIndex >= 0 && SelectedIndex < combobox.Count)
					{
						OnItemSelected();
						OnClose();
					}
					return true;
				case Keys.Escape:
					OnClose();
					return true;
			}
			return false;
		}

		// --- IOverlayWidget (required by IComboBoxDropDown) ---
		public OverlayModes OverlayMode { get; protected set; } = OverlayModes.Overlay;
		public event EventHandler<EventArgs> Closing;
		public virtual void OnClose()
		{
			if (Closing != null && !IsDisposed)
				Closing(this, EventArgs.Empty);
		}
	}
}
