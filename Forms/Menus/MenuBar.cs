using System;
using System.Linq;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Input;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using KS.Foundation;

namespace SummerGUI
{	
	public class MenuBar : Container
	{
		public IGuiMenu Menu { get; set; }

		IGUIFont Font { get; set; }
		IGUIFont IconFont { get; set; }

		public MenuBar (string name, IGuiMenu menu)
			: base(name, Docking.Top, new MenuBarStyle())
		{
			IsMenu = true;
			Menu = menu;

			Dock = Docking.Top;
			ZIndex = 5000;

			Padding = new Padding (8, 3, 8, 3);

			Styles.SetStyle (new MenuBarActiveStyle (), WidgetStates.Active);
			Styles.SetStyle (new MenuBarDisabledStyle (), WidgetStates.Disabled);
			Styles.SetStyle (new MenuBarSelectedStyle (), WidgetStates.Selected);

			//Font = SummerGUIWindow.CurrentContext.FontManager.DefaultFont;
			Font = FontManager.Manager.StatusFont;
			IconFont = FontManager.Manager.SmallIcons;
			CanFocus = false;
			TabIndex = -1;
		}

		public override SizeF PreferredSize (IGUIContext ctx, SizeF proposedSize)
		{
			if (Font == null)
				return base.PreferredSize(ctx, proposedSize);

			return new SizeF(proposedSize.Width, LineHeight);
		}

		public override IWidgetStyle Style {
			get {
				return Styles.GetStyle (WidgetStates.Default);
			}
		}

		private QuadTree Tree = null;

		public LayoutItem FindItem(float x, float y)
		{					
			// for this function we ignore any threading errors 
			// that might happen when accessing the tree
			try {
				if (Tree != null) {
					ILayoutItem li = Tree.Query (new RectangleF (x, y, 1, 1)).FirstOrDefault ();
					if (li == null)
						return LayoutItem.Empty;
					return (LayoutItem)li;
				}
			} catch {}

			return LayoutItem.Empty;
		}
			
		//private IGuiMenuItem ActiveItem = null;
		private IGuiMenuItem m_ActiveItem = null;
		public IGuiMenuItem ActiveItem 
		{
			get{
				return m_ActiveItem;
			}
			set{
				if (m_ActiveItem != value) {
					m_ActiveItem = value;
					if (m_ActiveItem != null)
						EnsureItemVisible (m_ActiveItem);
					if (m_ActiveItem != null && !m_ActiveItem.Children.IsNullOrEmpty ())
						ShowSubMenu (m_ActiveItem);
					else if (Expanded)
						CloseSubMenu ();
					else
						Invalidate ();
				}
			}
		}

		public virtual void EnsureItemVisible(IGuiMenuItem item)
		{
			if (item == null)
				return;
			Invalidate ();
			// ToDo:
		}

		private bool SetActiveIcon(float x, float y)
		{
			LayoutItem li = FindItem (x, y);
			if (li.Equals(LayoutItem.Empty) || li.Item == null) {
				if (!Expanded)
					m_ActiveItem = null;
			} else if (li.Item != ActiveItem) {
				m_ActiveItem = li.Item as IGuiMenuItem;
				return true;
			}
			return false;
		}

		public override void OnMouseMove (MouseMoveEventArgs e)
		{			
			base.OnMouseMove (e);

			if (SetActiveIcon (e.Position.X, e.Position.Y) && Expanded)
				ShowSubMenu (ActiveItem);
			else
				Invalidate ();
		}			

		public override void OnMouseEnter (IGUIContext ctx)
		{
			base.OnMouseEnter (ctx);
			if (Expanded)
				Select ();
		}

		public override void OnMouseLeave (IGUIContext ctx)
		{
			if (!Expanded)
				ActiveItem = null;
			base.OnMouseLeave (ctx);
		}

		public override void OnMouseDown (MouseButtonEventArgs e)
		{
			base.OnMouseDown (e);

			SetActiveIcon (e.X, e.Y);
			if (ActiveItem != null) {				
				ShowSubMenu (ActiveItem);
			}
			else
				Invalidate ();
		}

		public void Collapse()
		{
			Expanded = false;
			this.Select ();
			Invalidate ();
		}

		public SubMenuOverlay SubMenu { get; private set; }
		public bool Expanded { get; private set; }

		private int m_LastActiveItemIndex = -1;

		private void ShowSubMenu(IGuiMenuItem mainmenuItem)
		{
			if (mainmenuItem == null || mainmenuItem.Children.IsNullOrEmpty ())
				return;

			if (SubMenu != null)				
				CloseSubMenu ();			

			SubMenu = new SubMenuOverlay ("sub", mainmenuItem.Children);

			SubMenu.ZIndex = Math.Max(SubMenu.ZIndex, this.ZIndex + 1);
			SubMenu.SetBounds (GetSubMenuBounds(SubMenu));
			this.AddChild (SubMenu);

			SubMenu.Closing += delegate {
				CloseSubMenu ();
				m_LastActiveItemIndex = ActiveItem == null ? -1 : Menu.IndexOf(ActiveItem);
				ActiveItem = null;
				Invalidate ();
			};				

			//SubMenu.Focus ();
			//this.Focus();
			this.Select();

			//bool bTest = SubMenu.CanFocus;

			Expanded = true;
			Invalidate ();
		}
			
		private void CloseSubMenu()
		{		
			Expanded = false;
			if (SubMenu != null) {				
				RemoveChild (SubMenu);
				SubMenu.Dispose ();
				SubMenu = null;
			}
			//this.Focus ();
		}			

		protected override void LayoutChild (IGUIContext ctx, Widget child, RectangleF bounds)
		{
			if (child != null && child == SubMenu)
				base.LayoutChild (ctx, child, GetSubMenuBounds(SubMenu));
			else
				base.LayoutChild (ctx, child, bounds);
		}

		protected virtual RectangleF GetSubMenuBounds(SubMenuOverlay sub)
		{
			SummerGUIWindow aw = ParentWindow;
			if (aw == null)
				return RectangleF.Empty;

			RectangleF bounds = MarginBounds;

			try {				
				bounds.Offset (itemStartPositions[Menu.IndexOf(ActiveItem)], 0);
			} catch (Exception ex) {
				ex.LogError ();
			}				

			float spaceAbove = bounds.Top;
			float spaceBelow = aw.Height - bounds.Bottom;

			SizeF sz = sub.PreferredSize (aw);

			float desiredHeight = sz.Height;
			float desiredWidth = sz.Width;
			float itemHeight = sub.LineHeight;

			float maxspace = Math.Max (spaceAbove, spaceBelow);
			if (desiredHeight > maxspace)
				desiredHeight = maxspace / itemHeight * itemHeight;

			RectangleF result = RectangleF.Empty;
			if (desiredHeight > 0 && desiredWidth > 0) {				
				if (spaceBelow >= desiredHeight)
					result = new RectangleF (bounds.Left, bounds.Bottom, desiredWidth, desiredHeight);
				else if (spaceAbove >= desiredHeight)
					result = new RectangleF (bounds.Left, bounds.Top - desiredHeight, desiredWidth, desiredHeight);
			}

			if (!result.IsEmpty) {
				if (result.Left < 1)
					result.Offset (Math.Abs(result.Left) + 3, -2);
				else
					result.Offset (-2, -2);
			}

			return result;
		}

		private float LineHeight 
		{
			get{
				//return Font.Height * 1.3334f + Padding.Height;
				return Font.Height * 1.5f + Padding.Height;
			}
		}

		RectangleF lastBounds = RectangleF.Empty;
		float[] itemStartPositions = null;

		public override void OnPaint (IGUIContext ctx, RectangleF bounds)
		{			
			float x = bounds.Left;
			float y = bounds.Top + Padding.Top;
			float line = 0;
			float lineHeight = LineHeight;
			//int lineHeight = Bounds.Height;

			bool buildTree = false;
			if (lastBounds != bounds || Tree == null) {				
				try {
					lastBounds = bounds;
					Tree = new QuadTree (bounds);	
					itemStartPositions = new float[Menu.Count];
					buildTree = true;
				} catch (Exception ex) {
					ex.LogError ();
				}
			}

			for (int i = 0; i < Menu.Children.Count; i++) {
				IGuiMenuItem item = Menu.Children [i];
				if (item == null || !item.Visible)
					continue;

				float iconWidth = 0;
				float textWidth = 0;

				if (IconFont != null && item.ImageIndex > 0) {
					iconWidth = IconFont.Measure (((char)item.ImageIndex).ToString()).Width;
				}

				if (Font != null) {
					textWidth = Font.MeasureMnemonicString (item.Text).Width;
					if (textWidth > 0)
						iconWidth *= 1.35f;
				}

				float itemWidth = iconWidth + textWidth + Padding.Width;

				if (x + itemWidth > bounds.Width) {
					line++;
					x = bounds.Left;
					y += lineHeight;
				}

				RectangleF itemBounds = new RectangleF (x, y, itemWidth, lineHeight - Padding.Height);
				if (buildTree) {
					try {
						Tree.Add (new LayoutItem (itemBounds, item));
						itemStartPositions[i] = itemBounds.X;
					} catch (Exception ex) {
						ex.LogError ();
					}
				}

				WidgetStates state = WidgetStates.Default;
				if (!item.Enabled)
					state = WidgetStates.Disabled;
				else if (item == ActiveItem) {
					if (IsFocused || Selected)
						state = WidgetStates.Active;
					else
						state = WidgetStates.Selected;					
					ctx.FillRectangle (Styles.GetStyle (state).BackColorBrush,
						// irgendjemand scheint den mit bereits abgezogenem Padding aufzurufen, was falsch wäre
						new RectangleF(itemBounds.X, itemBounds.Y - Padding.Top, itemBounds.Width, itemBounds.Height + Padding.Height));
				}
				
				Brush brush = Styles.GetStyle (state).ForeColorBrush;

				itemBounds.Offset (Padding.Left, 1);
				itemBounds.Width -= Padding.Left;

				if (iconWidth > 0) {					
					ctx.DrawString (((char)item.ImageIndex).ToString (), IconFont, brush, itemBounds, FontFormat.DefaultIconFontFormatLeft);
					itemBounds.Offset (iconWidth, 0);
				}

				if (textWidth > 0) {					
					ctx.DrawString (item.Text, Font, brush, itemBounds, FontFormat.DefaultMnemonicLine);
				}

				x += itemWidth;
				if (x > bounds.Width) {
					line++;
					x = bounds.Left;
					y += lineHeight;
				}
			}
		}
			
		public void MoveFirst()
		{
			if (Menu == null)
				return;
			ActiveItem = Menu.FirstOrDefault ();
			Invalidate ();
		}

		public void MoveLast()
		{
			if (Menu == null)
				return;
			ActiveItem = Menu.LastOrDefault();
			Invalidate ();
		}

		public void MoveNext()
		{
			if (Menu == null || Menu.Count < 2)
				return;
			if (ActiveItem == null)
				MoveFirst();
			else {
				int index = Menu.IndexOf (ActiveItem);
				if (index >= 0) {
					int newIndex = index + 1;
					while (newIndex != index && (newIndex >= Menu.Count || !Menu [newIndex].Enabled || Menu [newIndex].IsSeparator)) {
						newIndex++;
						if (newIndex >= Menu.Count)
							newIndex = 0;
					}
					ActiveItem = Menu[newIndex];
					Invalidate ();
				}
			}
		}

		public void MovePrev()
		{
			if (Menu == null || Menu.Count < 2)
				return;
			if (ActiveItem == null)
				MoveLast();
			else {
				int index = Menu.IndexOf (ActiveItem);
				if (index >= 0) {
					int newIndex = index - 1;
					while (newIndex != index && (newIndex < 0 || !Menu [newIndex].Enabled || Menu [newIndex].IsSeparator)) {
						newIndex--;
						if (newIndex < 0)
							newIndex = Menu.Count - 1;
					}
					ActiveItem = Menu[newIndex];
					Invalidate ();
				}
			}
		}

		public override void Focus ()
		{
			Select ();
			Root.SelectedMenu = this;
		}			

		public override bool OnKeyDown (KeyboardKeyEventArgs e)
		{			
			/***
			if (base.OnKeyDown (e))
				return true;
			***/

			switch (e.Key) {
			case Keys.Home:
				if (Selected) {
					MoveFirst ();
					return true;
				}
				break;
			case Keys.End:
				if (Selected) {
					MoveLast ();
					return true;
				}
				break;
			case Keys.Left:
				if (Selected) {
					MovePrev ();
					return true;
				}
				break;
			case Keys.Right:
				if (Selected) {
					MoveNext ();
					return true;
				}
				break;
			case Keys.Down:
			case Keys.PageDown:
			case Keys.PageUp:
			case Keys.Enter:				
				if (Selected && SubMenu != null) {
					SubMenu.Select ();
					Expanded = true;
					if (e.Key == Keys.PageDown)
						SubMenu.MoveLast ();
					else
						SubMenu.MoveFirst ();
					return true;
				}
				break;
			case Keys.F10:
				if (Selected) {
					return false;
				}
				if (ActiveItem == null && !Menu.IsNullOrEmpty ()) {					
					if (m_LastActiveItemIndex >= 0 && m_LastActiveItemIndex < Menu.Count) {
						ActiveItem = Menu [m_LastActiveItemIndex];
					} else {
						ActiveItem = Menu.FirstOrDefault ();
						m_LastActiveItemIndex = 0;
					}
				}
				this.Select ();
				MoveNext ();
				MovePrev ();
				this.Update ();
				return true;
			case Keys.Escape:
				CloseSubMenu ();
				FocusPreviouseWidget ();
				return true;
			default:
				if (Menu != null) {
					char keyChar = e.Key.ToString () [0];
					for (int i = 0; i < Menu.Count; i++) {												
						IGuiMenuItem mi = Menu [i];
						if (mi == null || !(mi.Visible && mi.Enabled))
							continue;
						if (mi.ProcessInputKey (e))	// iterates recursive through all children
							return true;
						
						// Alt-Key / Mnemonics for this Selected menu only
						if (ModifierKeys.AltPressed && keyChar == mi.Mnemonic) {
							if (mi != ActiveItem) {
								CloseSubMenu ();
								ActiveItem = mi;
								m_LastActiveItemIndex = Menu.IndexOf (mi);
								Invalidate ();
								this.Select();
							}
							//return true;
						}							
					}
				}
				break;
			}

			if (SubMenu != null && SubMenu.OnKeyDown (e))
				return true;
				
			return false;
		}

		public void FocusPreviouseWidget()
		{
			if (Selected || Expanded) {
				if (ActiveItem != null && Menu != null)
					m_LastActiveItemIndex = Menu.IndexOf (ActiveItem);
				m_ActiveItem = null;
				//CloseSubMenu ();
				Invalidate ();
			}
		}

		protected override void CleanupManagedResources ()
		{
			CloseSubMenu ();
			base.CleanupManagedResources ();
		}

		protected override void CleanupUnmanagedResources ()
		{			
			m_ActiveItem = null;
			base.CleanupUnmanagedResources ();
		}
	}
}

