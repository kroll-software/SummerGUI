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
	
	public abstract class ComboBoxBase : Container
	{
		public event EventHandler<EventArgs> DropDownChanged;
		public virtual void OnDropDownChanged()
		{
			if (DropDownChanged != null)
				DropDownChanged (this, EventArgs.Empty);
		}

		public event EventHandler<EventArgs> ItemSelected;
		public void OnItemSelected()
		{
			if (ItemSelected != null)
				ItemSelected (this, EventArgs.Empty);
		}

		public event EventHandler<EventArgs> SelectedIndexChanged;
		public void OnSelectedIndexChanged()
		{
			if (SelectedIndexChanged != null)
				SelectedIndexChanged (this, EventArgs.Empty);
		}

		public event EventHandler<EventArgs> TextChanged;
		public void OnTextChanged()
		{
			if (TextChanged != null)
				TextChanged (this, EventArgs.Empty);
		}
				
		public ComboBoxItemCollection Items { get; private set; }

		public int Count { 
			get {
				return Items.Count;
			}
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
					OnSelectedIndexChanged ();
					// ToDo: Attention: this sets the Text
					if (m_SelectedIndex >= 0 && m_SelectedIndex < Items.Count)
						Text = Items [m_SelectedIndex].Text;					
				} else if (m_SelectedIndex >= 0 && m_SelectedIndex < Items.Count && Text != Items [m_SelectedIndex].Text) {
					m_SelectedIndex = value;
					OnSelectedIndexChanged ();
					Text = Items [m_SelectedIndex].Text;
				}
			}
		}

		public abstract string Text { get; set; }

		public List<ComboBoxItem> SelectedItems
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

		public void SelectItem(ComboBoxItem item)
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

		public ComboBoxItem SelectedItem
		{
			get{
				if (Items == null || m_SelectedIndex < 0 || m_SelectedIndex >= Items.Count)
					return null;
				return Items[m_SelectedIndex];
			}
		}

		private float m_DropDownWidth;
		public float DropDownWidth 
		{ 
			get {
				if (m_DropDownWidth <= 0)
					return Width;
				return m_DropDownWidth;
			}
			set {
				m_DropDownWidth = value;
			}
		}

		private float m_DropDownHeight;
		public float DropDownHeight 
		{ 
			get {
				if (m_DropDownHeight <= 0)
					return ItemHeight * Math.Min(Items.Count, 8);
				return m_DropDownHeight;
			}
			set {
				m_DropDownHeight = value;
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
			
		protected ComboBoxButton Button { get; private set; }
		protected ComboBoxDropDownOverlay DropDownWindow { get; private set; }

		protected ComboBoxBase (string name)
			: base (name)
		{
			Items = new ComboBoxItemCollection ();
			CanFocus = true;
		}

		protected void InsertButton()
		{
			Button = new ComboBoxButton ();
			AddChild (Button);
		}

		public override bool Enabled {
			get {				
				return base.Enabled;
			}
			set {				
				base.Enabled = value;	// ToDo: Ist das noch notwendig?
				if (Children != null) {
					for (int i = 0; i < Children.Count; i++)
						Children [i].Enabled = value;
				}
			}
		}			
			
		public void EnsureIndexVisible(int index)
		{
			if (index < 0 || index >= Items.Count)
				return;

			// ToDo:
		}			

		public bool IsDropedDown //{ get; protected set; }
		{ 
			get {
				return DropDownWindow != null && !DropDownWindow.IsDisposed;
			}
		}
			
		public void ToggleDropDown()
		{
			if (IsDropedDown)
				DropUp ();
			else
				DropDown ();
		}

		public void DropDown()
		{
			if (IsDropedDown)
				return;			

			if (DropDownWindow == null) {
				DropDownWindow = new ComboBoxDropDownOverlay ();

				DropDownWindow.SelectedIndex = SelectedIndex;
				
				AddChild (DropDownWindow);
				DropDownWindow.Focus ();

				DropDownWindow.ItemSelected += delegate {
					SelectedIndex = DropDownWindow.SelectedIndex;	
				};					

				DropDownWindow.Closing += delegate {
					if (!Button.IsFocused) {
						DropUp ();
						this.Focus ();
					}	
				};
			}				

			OnDropDownChanged ();
		}

		public void DropUp()
		{
			if (!IsDropedDown)
				return;			

			if (DropDownWindow != null) {
				DropDownWindow.Visible = false;
				RemoveChild(DropDownWindow);
				DropDownWindow.Dispose();
				DropDownWindow = null;
			}
				
			Invalidate ();
			OnDropDownChanged ();
		}

		public abstract void DrawItem (IGUIContext ctx, RectangleF bounds, ComboBoxItem item, IWidgetStyle style);

		protected override void LayoutChild (IGUIContext ctx, Widget child, RectangleF bounds)
		{
			if (child != null && child == DropDownWindow)
				base.LayoutChild (ctx, child, GetDropDownBounds());
			else
				base.LayoutChild (ctx, child, bounds);
		}

		protected virtual RectangleF GetDropDownBounds()
		{
			SummerGUIWindow aw = ParentWindow;
			if (aw == null)
				return RectangleF.Empty;

			//Rectangle bounds = ClientRectangle;
			RectangleF bounds = MarginBounds;

			float spaceAbove = bounds.Top;
			float spaceBelow = aw.Height - bounds.Bottom;

			float desiredHeight = DropDownHeight;
			float desiredWidth = DropDownWidth;

			float maxspace = Math.Max (spaceAbove, spaceBelow);
			if (desiredHeight > maxspace)
				desiredHeight = (maxspace / ItemHeight) * ItemHeight;

			if (desiredHeight < 1 || desiredWidth < 1)
				return RectangleF.Empty;

			if (spaceBelow >= desiredHeight)
				return new RectangleF (bounds.Left, bounds.Bottom, desiredWidth, desiredHeight);

			if (spaceAbove >= desiredHeight)
				return new RectangleF (bounds.Left, bounds.Top - desiredHeight, desiredWidth, desiredHeight);

			return RectangleF.Empty;
		}			

		public override bool OnKeyDown (KeyboardKeyEventArgs e)
		{
			/***
			if (base.OnKeyDown (e))
				return true;
			***/

			if (!IsFocused && !Children.Any(c => c.IsFocused))
				return false;

			switch (e.Key) {
			case Keys.Enter:
			case Keys.Down:
			case Keys.PageDown:
				if (!IsDropedDown) {
					DropDown ();
					return true;
				}
				break;
			}

			return false;
		}

		protected override void CleanupManagedResources ()
		{
			Items.Clear ();
			base.CleanupManagedResources ();
		}			
	}
}

