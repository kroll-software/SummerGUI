using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Input;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using KS.Foundation;
using SummerGUI.Menus;
using OpenTK.Windowing.Common;

namespace SummerGUI
{
	public class MobileMenuWidgetStyle : WidgetStyle
	{
		public override void InitStyle ()
		{
			//SetBackColor (Color.FromArgb(220, Theme.Colors.Cyan));
			SetBackColor (Color.Empty);
			SetForeColor (Theme.Colors.Base2);
			SetBorderColor (Color.Empty);
		}
	}


	//public class SideMenu : SubMenuOverlay
	public class SideMenuBar : Widget
	{				
		public IGuiMenu Menu { get; set; }

		public SideMenuBar (string name, IGuiMenu menu)
			: base(name, Docking.Fill, new EmptyWidgetStyle())
			//: base(name, menu)
		{			
			IsMenu = true;
			Menu = menu;
			MenuDirtyFlag = true;

			this.SetIconFontByTag(CommonFontTags.SmallIcons);
			this.SetFontByTag(CommonFontTags.Menu);
			//ZIndex = 3000;

			Styles.SetStyle (new SubMenuDisabledItemStyle (), WidgetStates.Disabled);
			Styles.SetStyle (new SubMenuSelectedItemStyle (), WidgetStates.Selected);
			Styles.SetStyle (new SubMenuActiveItemStyle (), WidgetStates.Active);

			ZIndex = 0;
			CanFocus = false;
			//TabIndex = -1;

			LastExpandedItems = new BinarySortedList<IGuiMenuItem> ();
		}



		protected override void OnRootChanged ()
		{
			base.OnRootChanged ();

			if (Root != null) {				
				UpdateLineHeight ();
			}
		}

		public IGUIFont Font { get; set; }
		public IGUIFont IconFont { get; set; }

		protected override void OnWidgetStateChanged ()
		{
			// we use the different styles for painting the items only
			WidgetState = WidgetStates.Default;
		}			

		protected void UpdateLineHeight()
		{
			try {
				if (Parent != null && ParentWindow != null)
					LineHeight =  WidgetExtensions.GetFont(CommonFontTags.Menu).TextBoxHeight;
			} catch (Exception ex) {
				ex.LogError ();
			}
		}

		protected override void OnScaleWidget (IGUIContext ctx, float absoluteScaleFactor, float relativeScaleFactor)
		{
			base.OnScaleWidget (ctx, absoluteScaleFactor, relativeScaleFactor);
			UpdateLineHeight ();
		}


		protected override void OnParentChanged ()
		{
			base.OnParentChanged ();

			ScrollableContainer sc = Parent as ScrollableContainer;
			if (!IsOverlay && sc != null) {
				sc.ScrollBars = ScrollBars.Vertical;
				sc.AutoScroll = true;
				sc.BackColor = Theme.Colors.Base0;
			}				
		}			

		private QuadTree Tree = null;
		public MenuControlItem FindItem(float x, float y)
		{		
			// for this function we ignore any threading errors 
			// that might happen when accessing the tree
			try {
				if (Tree != null) {
					ILayoutItem li = Tree.Query (new RectangleF (x, y, 1, 1)).FirstOrDefault ();
					if (li == null)
						return MenuControlItem.Empty;
					return (MenuControlItem)li;
				}
			} catch {}

			return MenuControlItem.Empty;
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
				}
			}
		}			
			
		public void EnsureItemVisible(IGuiMenuItem item)
		{
			Invalidate ();
		}

		private bool SetActiveItem(float x, float y)
		{
			MenuControlItem li = FindItem (x, y);
			if (li.Equals(MenuControlItem.Empty) || li.Item == null) {				
				m_ActiveItem = null;
			} else {
				if (li.ToolTip) {
					Root.ShowTooltip (li.Item.Text, new PointF(x, y));
				} else {
					Root.HideTooltip ();
				}

				if (li.Item != m_ActiveItem) {
					ActiveItem = li.Item;
					return true;
				}
			}

			return false;
		}

		public override void OnMouseMove (MouseMoveEventArgs e)
		{
			base.OnMouseMove (e);
			SetActiveItem (e.X, e.Y);
			Invalidate ();
		}			

		public BinarySortedList<IGuiMenuItem> LastExpandedItems { get; private set; }
		public IGuiMenuItem LastExpandedItem { get; set; }

		public void TrimToScreen()
		{
			if (IsDisposed || Parent == null || Parent.Height < 0.1)
				return;

			try {
				float clientHeight = Parent.Height - Parent.Children.Where (c => c != this && c.Visible && (c.Dock == Docking.Top || c.Dock == Docking.Bottom)).Sum (cw => cw.Height);
				while (LastExpandedItems.Count > 0) {
					int visibleCount = Menu.ExpandedItemsWithoutSeparators ();
					float height = visibleCount * LineHeight;
					if (height > clientHeight && LastExpandedItems.Count > 0) {
						var r = LastExpandedItems.First;
						LastExpandedItems.RemoveFirst ();
						if (LastExpandedItem == null || (r != LastExpandedItem && !r.IsParentOf (LastExpandedItem)))
							r.Collapse ();
					} else {
						break;
					}
				}	
			} catch (Exception ex) {
				ex.LogError ();
			} finally {				
				MenuDirtyFlag = true;
				Update (true);
				(Parent as SideMenuContainer).Do(p => p.OnCollapseExpand ());
			}
		}			

		public override void OnClick (MouseButtonEventArgs e)
		{
			base.OnClick (e);
			SetActiveItem (e.X, e.Y);
			if (m_ActiveItem != null) {
				if (m_ActiveItem.HasChildren) {
					IGuiMenuItem active = m_ActiveItem;
					active.Collapsed = !active.Collapsed;
					if (!active.Collapsed && active.HasChildren) {
						LastExpandedItem = active;
						active.ClickCount++;
						TrimToScreen ();
					} else {
						LastExpandedItem = null;
						MenuDirtyFlag = true;
						Update (true);
						(Parent as SideMenuContainer).Do(p => p.OnCollapseExpand ());
					}						
					if (!active.Collapsed) {						
						LastExpandedItems.Add(active); 
					}
					Invalidate ();
				} else if (m_ActiveItem.OnClick () && AutoClose) {
					var pw = ParentWindow;
					if (pw != null) {
						OnClose ();
					}
				}
			}
		}			

		public override void OnMouseLeave (IGUIContext ctx)
		{
			base.OnMouseLeave (ctx);
			m_ActiveItem = null;
		}

		public event EventHandler<EventArgs> Close;
		public virtual void OnClose()
		{
			if (Close != null)
				Close (this, EventArgs.Empty);
		}

		public void QueueDraw(IGuiMenuItem w, int level, Queue<IGuiMenuItem> q)
		{			
			//this.LogInformation ("level {0}: {1}", level, w.Text);
			w.Level = level;
			if (!w.IsSeparator)
				q.Enqueue (w);
			if (w.Children != null && !w.Collapsed)
				w.Children.ForEach (c => QueueDraw(c, level + 1, q));
		}
			
		public bool MenuDirtyFlag;

		public override void OnLayout (IGUIContext ctx, RectangleF bounds)
		{			
			base.OnLayout (ctx, bounds);

			if (MenuDirtyFlag) {
				MenuDirtyFlag = false;
				Tree = null;
				var q = new Queue<IGuiMenuItem> ();
				Menu.ForEach (m => QueueDraw (m, 0, q));
				//Queue = q;
				Concurrency.LockFreeUpdate (ref Queue, q);
				ItemCount = q.Count;
			}				

			this.SetSize (Bounds.Width, ItemCount * LineHeight);
		}

		public float LineHeight { get; protected set; }
		public int ItemCount { get; private set; }
		public Queue<IGuiMenuItem> Queue;

		RectangleF lastBounds = RectangleF.Empty;

		public override void OnPaint (IGUIContext ctx, RectangleF bounds)
		{									
			bounds.Inflate (Padding);

			float icoWidth = LineHeight * 0.85f;

			bool buildTree = false;
			if (lastBounds != bounds || Tree == null || MenuDirtyFlag) {
				try {
					lastBounds = bounds;
					Tree = new QuadTree (bounds);
					buildTree = true;
				} catch (Exception ex) {
					ex.LogError ();
				}
			}

			RectangleF itemRect = bounds;

			try {
				foreach (var w in Queue) {
					if (w.IsSeparator) {

					} else {
						//float stringWidth = 0;
						bool toolTipFlag = false;

						itemRect.Height = LineHeight - 1;
						if (ctx.ClipBoundStack.IsOnScreen(itemRect)) {
							float indent = 0;
							Brush foreBrush = Theme.Brushes.Base2;
							Brush backBrush = Theme.Brushes.Base01;

							switch (w.Level) {
							case 0:								
								backBrush = Theme.Brushes.Base1;
								if (w.Enabled)
									foreBrush = Theme.Brushes.Base02;
								else
									foreBrush = Theme.Brushes.Base01;
								break;
							case 1:								
								indent = 8;
								if (!w.Enabled)
									foreBrush = Theme.Brushes.Base02;
								break;
							case 2:								
								indent = 16;
								backBrush = Theme.Brushes.Base02;
								if (w.Enabled)
									foreBrush = Theme.Brushes.Base1;
								else
									//foreColor = Theme.Colors.Base1.Lerp(Color.Black, 0.35);
									foreBrush = Theme.Brushes.Base01;
								break;					
							}

							if (w != null && w == LastExpandedItem && w.Enabled) {
								using (Brush sb = new SolidBrush(backBrush.Color.Lerp(Theme.Colors.Cyan, 0.85))) {
									ctx.FillRectangle (sb, itemRect);
								}
								foreBrush = Theme.Brushes.White;							
							} else if (w != null && w == m_ActiveItem && w.Enabled) {
								using (Brush sb = new SolidBrush(backBrush.Color.Lerp(Theme.Colors.Orange, 0.85))) {
									ctx.FillRectangle (sb, itemRect);
								}
								foreBrush = Theme.Brushes.White;
							} else {
								ctx.FillRectangle (backBrush, itemRect);
							}

							indent *= ScaleFactor;
								
							RectangleF tb = itemRect;

							if (w.HasChildren) {
								char dropDownTriangle = w.Collapsed ? (char)FontAwesomeIcons.fa_caret_right : (char)FontAwesomeIcons.fa_caret_down;
								RectangleF ib = new RectangleF(itemRect.Right - icoWidth, itemRect.Top, icoWidth, itemRect.Height);
								ctx.DrawString (dropDownTriangle.ToString(), this.IconFont, foreBrush, ib, FontFormat.DefaultSingleBaseLineCentered);
								if (w.Level == 0) {
									tb.Width -= icoWidth * 2;
									tb.Offset(icoWidth, 0);
								} else {
									tb.Width -= icoWidth + 2;
								}
							}

							if (w.Level == 0) {								
								tb.Offset(0, 2);
								toolTipFlag = ctx.DrawString (w.DisplayString.ToUpper(), this.Font, foreBrush, tb, FontFormat.DefaultSingleLineCentered)
									.Width > tb.Width;
							} else {								
								if (w.ImageIndex > 0 || w.IsToggleButton) {
									RectangleF ib = itemRect;
									ib.Width = icoWidth;
									ib.Offset(indent, 0);
									string c;
									if (w.IsToggleButton) {
										if (w.Checked)
											c = ((char)FontAwesomeIcons.fa_toggle_on).ToString();
										else
											c = ((char)FontAwesomeIcons.fa_toggle_off).ToString();
									} else 
										c = ((char)w.ImageIndex).ToString();

									ctx.DrawString (c, this.IconFont, foreBrush, ib, FontFormat.DefaultSingleBaseLine);
								}									

								tb.Offset(icoWidth + indent, 1);
								tb.Width -= icoWidth + indent;
								toolTipFlag = ctx.DrawString (w.DisplayString, this.Font, foreBrush, tb, FontFormat.DefaultSingleLine)
									.Width > tb.Width;
							}
						}

						if (buildTree) {
							try {								
								Tree.Add (new MenuControlItem (itemRect, w, toolTipFlag));
							} catch (Exception ex) {
								ex.LogError ();
							}
						}

						itemRect.Offset (0, LineHeight);
					}
				}
			} catch (Exception ex) {
				ex.LogError ();
			}
		}

		public override SizeF PreferredSize (IGUIContext ctx, SizeF proposedSize)
		{			
			return new SizeF(200, Math.Max(Menu.Count, ItemCount) * LineHeight);
		}

		// **** Options ****

		public bool AutoCollapse { get; internal set; }
		public bool AutoClose { get; internal set; }

		protected override void CleanupManagedResources ()
		{			
			LastExpandedItems.Clear ();
			LastExpandedItem = null;
			Menu = null;
			Font = null;
			IconFont = null;
			base.CleanupManagedResources ();
		}
	}
}

