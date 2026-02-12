using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using KS.Foundation;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace SummerGUI
{
    public class ListBoxWidgetStyle : WidgetStyle
	{
		public override void InitStyle ()
		{
			SetBackColor (Theme.Colors.Base3);
			SetForeColor (Theme.Colors.Base02);
			SetBorderColor (Theme.Colors.Base02);
            Border = 1f;
		}
	}

	public class ListBoxSelectedItemStyle : WidgetStyle
	{
		public override void InitStyle ()
		{
			SetBackColor (Theme.Colors.HighLightBlue);
			SetForeColor (Theme.Colors.White);
			SetBorderColor (Color.Empty);
		}
	}

    public class ListBoxSelectedItemStyleInactive : WidgetStyle
	{
		public override void InitStyle ()
		{
			SetBackColor (Theme.Colors.HighLightBlue.ToGray());
			SetForeColor (Theme.Colors.White);
			SetBorderColor (Color.Empty);
		}
	}

	public abstract class ListBoxBase : ScrollableContainer
	{
        public WidgetStyle SelectedItemStyleInactive { get; private set; }
        public WidgetStyle SelectedItemStyle { get; private set; }

        public event EventHandler<EventArgs> SelectionChanged;
		public void OnSelectionChanged()
		{
			if (SelectionChanged != null && !IsDisposed)
				SelectionChanged (this, EventArgs.Empty);			
		}

        public ListBoxItemCollection Items { get; private set; }

		public int Count { 
			get {
				return Items.Count;
			}
		}

        float m_ItemHeight;
		[DpiScalable]
		public float ItemHeight 
		{ 
			get {
				return m_ItemHeight;
			}
			set {
				if (m_ItemHeight != value) {
					m_ItemHeight = value;
					OnItemHeightChanged ();
				}
			}
		}

        protected virtual void OnItemHeightChanged()
		{
			ResetCachedLayout ();
		}		

        protected int m_SelectedIndex = -1;
		public int SelectedIndex
		{
			get{
				return m_SelectedIndex;
			}
			set{
				if (m_SelectedIndex != value) {
					m_SelectedIndex = value;
					OnSelectionChanged ();				
				} else if (m_SelectedIndex >= 0 && m_SelectedIndex < Items.Count) {
					m_SelectedIndex = value;
					OnSelectionChanged ();					
				}
			}
		}

		public object SelectedValue
		{
			get{
				if (m_SelectedIndex < 0 || m_SelectedIndex >= Items.Count)
					return null;
				return Items[m_SelectedIndex].Value;
			}			
		}		

		public List<ListBoxItem> SelectedItems
		{
			get{
				return Items.Where (item => item.Selected).ToList ();
			}
		}

		public List<object> SelectedValues
		{
			get{
				return Items.Where (item => item.Selected).Select(item => item.Value).ToList ();
			}
		}

		public void ResetSelected()
		{
			for (int i = 0; i < Items.Count; i++)
				Items [i].Selected = false;
		}

		public bool MultiSelect { get; set; }

		public void SelectItem(ListBoxItem item)
		{
			if (!MultiSelect)
				ResetSelected ();
			item.Selected = true;
		}

		public void SelectIndex(int index)
		{
			if (!MultiSelect)
				ResetSelected ();
			Items[index].Selected = true;
		}			

		private bool m_Sorted;
		public bool Sorted 
		{ 
			get {
				return m_Sorted;
			}
			set {
				if (m_Sorted != value) {
					m_Sorted = value;
					if (m_Sorted) {
						Items.Sort ();
					}
				}
			}
		}			

		public ListBoxItem SelectedItem
		{
			get{
				if (Items == null || m_SelectedIndex < 0 || m_SelectedIndex >= Items.Count)
					return null;
				return Items[m_SelectedIndex];
			}
		}

        IGUIFont m_Font;
		public IGUIFont Font 
		{ 
			get {
				return m_Font;
			}
			set {
				if (m_Font != value) {
					m_Font = value;
					OnFontChanged ();
				}
			}
		}

		public virtual void OnFontChanged()
		{
			ResetCachedLayout ();
		}

        [DpiScalable]
		public Size TextMargin { get; set; }

		public ListBoxBase (string name)
			: base (name)
        {
            Styles.SetStyle (new ListBoxWidgetStyle(), WidgetStates.Default);
            
            SelectedItemStyleInactive = new ListBoxSelectedItemStyleInactive();
            SelectedItemStyle = new ListBoxSelectedItemStyle();
            
			ScrollBars = ScrollBars.Vertical;

            m_Font = FontManager.Manager.DefaultFont;
            m_ItemHeight = m_Font.LineHeight;
            AutoScroll = true;

            Items = new ListBoxItemCollection ();
			CanFocus = true;
            TabStop = true;

            TextMargin = new Size(6, 0);
        }
        
        protected override void LayoutChildren (IGUIContext ctx, RectangleF bounds)
		{										
			base.LayoutChildren(ctx, bounds);			
			DocumentSize = new SizeF (bounds.Width, Count * ItemHeight);
		}

        public abstract void DrawItem (IGUIContext ctx, RectangleF bounds, ListBoxItem item, IWidgetStyle style);

        public override void OnPaint (IGUIContext ctx, RectangleF bounds)
		{					
			base.OnPaint(ctx, bounds);

			float scrollOffsetY = 0;
			float scrollWidth = 0;
			if (VScrollBar != null && VScrollBar.Visible)
			{				
				scrollWidth = VScrollBar.Width;
				scrollOffsetY = VScrollBar.Value;
			}

			float itemHeight = ItemHeight;

			RectangleF clipRect = new RectangleF(bounds.Left, bounds.Top, bounds.Width - scrollWidth, bounds.Height);
			using (var clip = new ClipBoundClip(ctx, clipRect, false))
			{
				for (int i = 0; i < Items.Count; i++) {
					RectangleF itemBounds = new RectangleF (bounds.Left, (i * itemHeight) + bounds.Top - scrollOffsetY, 
						bounds.Width - scrollWidth, itemHeight);

					if (i == SelectedIndex) {
                        IWidgetStyle style;
                        if (IsFocused)
                            style = SelectedItemStyle;
                        else
                            style = SelectedItemStyleInactive;
						
						ctx.FillRectangle (style.BackColorBrush, itemBounds);
						DrawItem(ctx, itemBounds, Items[i], style);
					} else {
						DrawItem(ctx, itemBounds, Items[i], Style);
					}					
				}
			}
		}        

        public void EnsureIndexVisible(int idx)
		{
            if (VScrollBar == null)
                return;

            if (Count <= 0)
            {                
				VScrollBar.Value = 0;
                return;
            }

            float top = idx * ItemHeight;
            float bottom = top + ItemHeight;

            if (top < VScrollBar.Value || ItemHeight > Height)
			{
				VScrollBar.Value = top;
			}
			else if (bottom > VScrollBar.Value + Height)
			{
				VScrollBar.Value = top - (Height - ItemHeight);
			}

			this.Invalidate ();
		}		

		public void SeekIndex(int newIndex)
		{			
			if (newIndex < SelectedIndex && SelectedIndex == 0)
				return;

			if (newIndex > SelectedIndex && SelectedIndex == Count - 1)
				return;

			SelectedIndex = Math.Max(0, Math.Min(newIndex, Count - 1));
			EnsureIndexVisible (SelectedIndex);
		}

        public override bool OnKeyDown(KeyboardKeyEventArgs e)
        {
            if (!IsFocused)
                return false;

            bool handled = false;

            switch (e.Key)
            {
                case Keys.Home:
                    handled = true;
                    SeekIndex(0);
                    break;

                case Keys.End:
                    handled = true;
                    SeekIndex(Items.Count - 1);
                    break;

                case Keys.Up:
                    handled = true;
                    SeekIndex(SelectedIndex - 1);
                    break;

                case Keys.Down:
                    handled = true;
                    SeekIndex(SelectedIndex + 1);
                    break;
                
                case Keys.PageUp:
                    handled = true;
                    SeekIndex (SelectedIndex - (int)(Bounds.Height / ItemHeight));
                    break;

			    case Keys.PageDown:
                    handled = true;
                    SeekIndex (SelectedIndex + (int)(Bounds.Height / ItemHeight));
                    break;
            }            
            
            return handled || base.OnKeyDown(e);
        }

        public override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);

            RectangleF scrollbounds = this.Bounds;
			if (VScrollBar.IsVisibleEnabled)
				scrollbounds.Width -= VScrollBar.Width;

			if (e.X > scrollbounds.Right)
				return;
			
			if (ItemHeight > 0) {
				SelectedIndex = (int)((e.Y - Bounds.Top + VScrollBar.Value) / ItemHeight);
				Invalidate ();
			}

			if (SelectedIndex < 0 || SelectedIndex >= Items.Count)
				return;

			OnSelectionChanged();
        }

        protected override void CleanupManagedResources ()
		{
			Items.Clear ();
			base.CleanupManagedResources ();
		}
	}
}

