using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using OpenTK;
using OpenTK.Input;
using KS.Foundation;


namespace SummerGUI
{		
	public class SubMenuOverlay : OverlayContainer
	{		
		public IGUIFont Font  { get; set; }
		public IGUIFont IconFont  { get; set; }
		public IGuiMenu Menu { get; set; }

		public SubMenuOverlay (string name, IGuiMenu items)
			: base (name, Docking.None, new SubMenuWidgetStyle())
		{			
			IsMenu = true;
			Menu = items;

			Padding = new Padding (6, 1, 10, 1);

			//Font = SummerGUIWindow.CurrentContext.FontManager.DefaultFont;
			Font = SummerGUIWindow.CurrentContext.FontManager.StatusFont;
			IconFont = SummerGUIWindow.CurrentContext.FontManager.SmallIcons;

			Styles.SetStyle (new SubMenuDisabledItemStyle (), WidgetStates.Disabled);
			Styles.SetStyle (new SubMenuSelectedItemStyle (), WidgetStates.Selected);
			Styles.SetStyle (new SubMenuActiveItemStyle (), WidgetStates.Active);

			CanFocus = false;
			TabIndex = -1;
		}			
			
		public override bool CanFocus {
			get {
				return base.CanFocus;
			}
			set {
				base.CanFocus = value;
			}
		}


		public override void OnClose ()
		{
			CloseSubMenu ();
			base.OnClose ();
		}			
			
		protected string ModifierString(IGuiMenuItem item)
		{
			if (item.HotKey == Key.Unknown)
				return null;
			
			string modifier = String.Empty;
			if (item.ModifierKey != 0) {
				if (item.ModifierKey.HasFlag (KeyModifiers.Shift)) {						
					modifier += "Shift+";
				}
				if (item.ModifierKey.HasFlag (KeyModifiers.Control)) {						
					modifier += "Ctrl+";
				}
				if (item.ModifierKey.HasFlag (KeyModifiers.Alt)) {						
					modifier += "Alt+";
				}						
			}

			return modifier + item.HotKey.ToString ();
		}	

		public virtual float LineHeight
		{
			get{				
				if (Font == null)
					return 35;
				return Font.TextBoxHeight;
			}
		}

		protected virtual float SeparatorHeight
		{
			get{
				//return Font.Height / 2;
				return LineHeight / 3f;
			}
		}
			
		public override SizeF PreferredSize (IGUIContext ctx, SizeF proposedSize)
		{
			if (CachedPreferredSize == SizeF.Empty) {
				if (Menu == null || Font == null)
					return base.PreferredSize (ctx, proposedSize);

				float maxWidth = 0;
				float height = 0;
				float lineHeight = LineHeight;
				float separatorHeight = SeparatorHeight;

				string space = new string (' ', 8);
				for (int i = 0; i < Menu.Count; i++) {
					IGuiMenuItem item = Menu [i];
					if (item.IsSeparator) {
						height += separatorHeight;
					} else {
						string line = item.Text;
						string modifier = ModifierString (Menu [i]);
						if (modifier != null)
							line += space + modifier;					
						float w = Font.MeasureMnemonicString (line).Width;
						if (w > maxWidth)
							maxWidth = w;
						height += lineHeight;
					}					
				}

				float iconWidth = LineHeight * 1f;
				CachedPreferredSize = new SizeF (iconWidth + maxWidth + Padding.Width, height + Padding.Height + Style.Border);
			}

			return CachedPreferredSize;
		}
			
		public override void OnLayout (IGUIContext ctx, RectangleF bounds)
		{
			base.OnLayout (ctx, bounds);

			SubMenuWidgetStyle style = Style as SubMenuWidgetStyle;
			if (style != null && Font != null)
				style.IconColumnWidth = LineHeight.Ceil();
		}

		protected override void LayoutChildren (IGUIContext ctx, RectangleF bounds)
		{
			if (Font != null && Menu != null)
				DocumentSize = new SizeF (bounds.Width, Menu.Count * LineHeight);
		}
			
		private IGuiMenuItem m_ActiveItem = null;
		public IGuiMenuItem ActiveItem 
		{
			get{
				return m_ActiveItem;
			}
			set{
				if (m_ActiveItem != value) {
					m_ActiveItem = value;
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

		public override void OnMouseMove (MouseMoveEventArgs e)
		{
			base.OnMouseMove (e);
			if (!Selected) {
				this.Select ();
				Root.SelectedMenu = this;
			}

			SetActiveItem (e.X, e.Y);
			Invalidate ();
		}			

		public override void OnMouseUp (MouseButtonEventArgs e)
		{
			base.OnMouseUp (e);

			// Execute and Close this
			SetActiveItem (e.X, e.Y);
			Invalidate ();
			if (m_ActiveItem != null) {
				if (m_ActiveItem.OnClick ())
					OnClose ();
			}
		}

		public override void OnMouseEnter (IGUIContext ctx)
		{
			base.OnMouseEnter (ctx);
			if (SubMenu != null)
				SubMenu.ActiveItem = null;	// this closes next level submenu
		}			

		private QuadTree Tree = null;

		public SubMenuOverlay SubMenu { get; private set; }
		public bool Expanded { get; private set; }

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

		private bool SetActiveItem(float x, float y)
		{
			LayoutItem li = FindItem (x, y);
			if (li.Equals(LayoutItem.Empty) || li.Item == null) {
				if (!Expanded)
					m_ActiveItem = null;
			} else if (li.Item != m_ActiveItem) {
				ActiveItem = li.Item as IGuiMenuItem;
				return true;
			}
			return false;
		}

		protected override void OnWidgetStateChanged ()
		{
			WidgetState = WidgetStates.Default;
		}

		RectangleF lastBounds = RectangleF.Empty;
		float[] itemStartPositions = null;

		public override void OnPaint (IGUIContext ctx, RectangleF bounds)
		{
			base.OnPaint (ctx, bounds);

			float lineHeight = LineHeight;
			float separatorHeight = SeparatorHeight;

			RectangleF rLine = bounds;
			rLine.Height = lineHeight;
			rLine.Width -= Padding.Right;
			rLine.Offset (0, Padding.Top);
			float textOffset = lineHeight + Padding.Left;

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

				float itemHeight = 0;

				if (item.IsSeparator) {
					itemHeight = separatorHeight;
					float y = rLine.Y + separatorHeight / 2;

					using (Pen pen = new Pen(Theme.Colors.Base02, ScaleFactor))
						ctx.DrawLine (pen, rLine.X + textOffset, y, rLine.X + rLine.Width, y);
					
					y++;
					using (Pen pen = new Pen(Theme.Colors.Base0, ScaleFactor))
						ctx.DrawLine (pen, rLine.X + textOffset, y, rLine.X + rLine.Width, y);
				} else {
					itemHeight = lineHeight;
					WidgetStates state = WidgetStates.Default;
					if (!item.Enabled)
						state = WidgetStates.Disabled;
					else if (item == m_ActiveItem) {
						if (IsFocused || Selected)
							state = WidgetStates.Active;
						else
							state = WidgetStates.Selected;
						ctx.FillRectangle (Styles.GetStyle (state).BackColorBrush, 
							new RectangleF(rLine.X + 1, rLine.Y, rLine.Width + Padding.Right - 2, rLine.Height));
					}

					IWidgetStyle style = Styles.GetStyle (state);

					if (IconFont != null) {
						char icon;
						if (item.IsToggleButton)
							icon = item.Checked ? (char)FontAwesomeIcons.fa_toggle_on : (char)FontAwesomeIcons.fa_toggle_off;
						else
							icon = (char)item.ImageIndex;

						if (icon > 0) {
							RectangleF rIcon = rLine;
							rIcon.Width = lineHeight;
							ctx.DrawString (icon.ToString (), IconFont, style.ForeColorBrush, rIcon, FontFormat.DefaultIconFontFormatCenter);
						}
					}

					if (Font != null) {
						RectangleF rText = new RectangleF(rLine.X + textOffset, rLine.Y + 1, rLine.Width - textOffset, rLine.Height - 1);
						ctx.DrawString (item.Text, Font, style.ForeColorBrush, rText, FontFormat.DefaultMnemonicLine);

						if (item.HasChildren) {
							if (IconFont != null) {
								ctx.DrawString (((char)FontAwesomeIcons.fa_caret_right).ToString(), IconFont, style.ForeColorBrush, rText, FontFormat.DefaultSingleLineFar);
							}
						} else {					
							string modifier = ModifierString (item);
							if (modifier != null)
								ctx.DrawString (modifier, Font, style.ForeColorBrush, rText, FontFormat.DefaultSingleLineFar);
						}
					}
				}

				if (buildTree) {
					try {
						Tree.Add (new LayoutItem (rLine, item));
						itemStartPositions[i] = rLine.Y;
					} catch (Exception ex) {
						ex.LogError ();
					}
				}

				rLine.Offset (0, itemHeight);
			}
		}
			
		public void MoveFirst()
		{
			if (Menu == null)
				return;
			m_ActiveItem = Menu.FirstOrDefault ();
			//while (!m_ActiveItem.Enabled && m_ActiveItem != Menu.LastOrDefault ())
			//	MoveNext ();
			if (m_ActiveItem == null || !m_ActiveItem.Enabled)
				MoveNext ();
			Invalidate ();
		}

		public void MoveLast()
		{
			if (Menu == null)
				return;
			m_ActiveItem = Menu.LastOrDefault();
			Invalidate ();
		}

		public void MoveNext()
		{
			if (Menu == null || Menu.Count < 1)
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
					m_ActiveItem = Menu[newIndex];
					Invalidate ();
				}
			}
		}

		public void MovePrev()
		{
			if (Menu == null || Menu.Count < 2)
				return;
			
			if (ActiveItem == null) {				
				if (Parent as MenuBar != null) {						
					(Parent as MenuBar).Collapse ();
					return;
				} else {
					MoveLast ();
				}
			} else {
				int index = Menu.IndexOf (ActiveItem);
				if (index >= 0) {
					int newIndex = index - 1;
					while (newIndex != index && (newIndex < 0 || !Menu [newIndex].Enabled || Menu [newIndex].IsSeparator)) {
						newIndex--;
						if (newIndex < 0) {
							if (Parent as MenuBar != null) {	
								(Parent as MenuBar).Collapse ();
								return;
							} else {
								newIndex = Menu.Count - 1;
							}
						}
					}
					m_ActiveItem = Menu[newIndex];
					Invalidate ();
				}
			}
		}

		public virtual void EnsureItemVisible(IGuiMenuItem item)
		{
			if (item == null)
				return;

			// ToDo:
		}			
			
		public override bool OnKeyDown (KeyboardKeyEventArgs e)
		{
			if (base.OnKeyDown (e))
				return true;

			if (!(Visible && Enabled && Selected))
				return false;

			switch (e.Key) {
			case Key.Escape:
				OnClose ();
				return true;
			case Key.Up:
				MovePrev ();
				return true;
			case Key.Down:
				MoveNext ();
				return true;
			case Key.PageDown:
			case Key.End:
				MoveLast ();
				return true;
			case Key.PageUp:
			case Key.Home:
				MoveFirst ();
				return true;
				
			case Key.Left:
				if (Parent as SubMenuOverlay != null) {
					Parent.Select ();
					(Parent as SubMenuOverlay).CloseSubMenu ();
					return true;
				}
				if (Parent as MenuBar != null) {	
					Parent.Select ();
					(Parent as MenuBar).MovePrev ();
					return true;
				}					
				break;
			case Key.Right:
				if (m_ActiveItem != null && !m_ActiveItem.Children.IsNullOrEmpty ()) {
					ShowSubMenu (m_ActiveItem);
					if (SubMenu != null) {
						SubMenu.Select ();
						SubMenu.MoveFirst ();
						Invalidate (); 
					}
					return true;
				}
				if (Parent as MenuBar != null) {
					Parent.Select ();
					(Parent as MenuBar).MoveNext();
					return true;
				}
				break;
			case Key.Space:
				if (m_ActiveItem != null) {
					if (m_ActiveItem.OnClick ())
						Invalidate ();
					return true;
				}
				break;
			case Key.Enter:
				if (m_ActiveItem != null && !m_ActiveItem.Children.IsNullOrEmpty ()) {
					ShowSubMenu (m_ActiveItem);
					if (SubMenu != null) {
						SubMenu.Select ();
						SubMenu.MoveFirst ();
						Invalidate (); 
					}
					return true;
				}
				if (m_ActiveItem != null) {					
					if (m_ActiveItem.OnClick()) {
						OnClose ();
						return true;
					}
				}
				break;
			default:
				if (Menu != null && e.Key != Key.AltLeft) {
					char keyChar = e.Key.ToString () [0];
					//this.LogVerbose ("{0}, {1}", e.Key.ToString(), ModifierKeys.AltPressed.ToString());

					for (int i = 0; i < Menu.Count; i++) {
						IGuiMenuItem mi = Menu [i];
						if (mi == null || !(mi.Visible && mi.Enabled))
							continue;
						if (mi.ProcessInputKey (e))	// iterates recursive through all children
							return true;						
						if (ModifierKeys.AltPressed && keyChar == mi.Mnemonic) {							
							if (mi.OnClick()) {
								OnClose ();
								return true;
							}
							else if (!mi.Children.IsNullOrEmpty ()) {
								if (mi != ActiveItem) {
									CloseSubMenu ();
									ActiveItem = mi;
									Invalidate ();
									this.Select ();
								}
								//return true;
							}
						}
					}
				}
				break;
			}

			if (SubMenu != null && SubMenu.OnKeyDown (e))
				return true;

			return false;
		}

		// Sub Menu..

		protected override void OnAddChild (Widget child)
		{
			base.OnAddChild (child);
			child.ZIndex = Math.Max(child.ZIndex, this.ZIndex + 1);
		}

		protected void ShowSubMenu(IGuiMenuItem mainmenuItem)
		{
			if (mainmenuItem == null || mainmenuItem.Children.IsNullOrEmpty ())
				return;

			if (SubMenu != null)				
				CloseSubMenu ();			

			SubMenu = AddChild (new SubMenuOverlay ("sub" + (Children.Count + 1).ToString(), mainmenuItem.Children));
			SubMenu.SetBounds (GetSubMenuBounds(SubMenu));
			SubMenu.Closing += delegate {
				CloseSubMenu ();
				m_ActiveItem = null;
				this.OnClose();
			};

			//SubMenu.Select ();

			Expanded = true;
			Invalidate ();
		}

		public void CloseSubMenu()
		{		
			Expanded = false;
			if (SubMenu != null) {				
				RemoveChild (SubMenu);
				SubMenu.Dispose ();
				SubMenu = null;
			}
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
			if (aw == null || aw.Width < 5 || aw.Height < 5)
				return RectangleF.Empty;

			RectangleF bounds = Bounds;
			SizeF sz = sub.PreferredSize (aw);

			float w = Math.Min (sz.Width, aw.Width);
			float h = Math.Min (sz.Height, aw.Height);

			float y = 0;

			try {				
				y = itemStartPositions[Menu.IndexOf (m_ActiveItem)] - Padding.Top;
			} catch (Exception ex) {
				ex.LogError ();
			}				

			RectangleF rsub = new RectangleF (
				bounds.Right, 
				y, 
				w, 
				h);

			try {
				float ofs = 5 * ParentWindow.ScaleFactor;
				rsub.Offset (-ofs, -ofs);	
			} catch (Exception ex) {
				ex.LogError ();
			}

			if (rsub.Right > aw.Width)
				rsub.Offset (aw.Width - rsub.Right, 0);

			if (rsub.Bottom > aw.Height)
				rsub.Offset (0, aw.Height - rsub.Bottom);

			return rsub;
		}

		protected override void CleanupManagedResources ()
		{
			CloseSubMenu ();
			base.CleanupManagedResources ();
		}

		protected override void CleanupUnmanagedResources ()
		{			
			m_ActiveItem = null;
			Menu = null;
			base.CleanupUnmanagedResources ();
		}
	}
}

