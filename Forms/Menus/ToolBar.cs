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

namespace SummerGUI
{		
	public class ToolBarImageButton : ImageButton
	{	
		public ToolBarImageButton(string name, string text, string imageKey)
			: base(name, Docking.Left, null, null, imageKey, new MainToolBarButtonStyle())
		{			
			Styles.SetStyle (new MainToolBarButtonStyle (), WidgetStates.Selected);
			Styles.SetStyle (new MainToolBarButtonHoverStyle (), WidgetStates.Hover);
			Styles.SetStyle (new MainToolBarButtonPressedStyle (), WidgetStates.Pressed);
			Styles.SetStyle (new MainToolBarButtonDisabledStyle (), WidgetStates.Disabled);

			Tooltip = text;
		}

		protected override void OnParentChanged ()
		{
			base.OnParentChanged ();
            if (Parent is ToolBarBase tb)
                ImageList = tb.Images;
        }

        public override Widget HitTest (float x, float y)
		{
			if (base.HitTest (x, y) != null)
				return this;
			return null;
		}
	}

	public class ToolBarCircleButton : CircleButton
	{
		public ToolBarCircleButton(string name, string text, string imageKey)
			: base(name, Docking.Left, null, null, imageKey, new CircleWidgetStyle())
		{			
			Styles.SetStyle (new CircleWidgetStyle (), WidgetStates.Selected);
			Styles.SetStyle (new CircleWidgetHoverStyle(), WidgetStates.Hover);
			Styles.SetStyle (new CircleWidgetPressedStyle(), WidgetStates.Pressed);
			Styles.SetStyle (new CircleWidgetDisabledStyle(), WidgetStates.Disabled);

			Tooltip = text;
			Margin = new Padding (1);
			Padding = Padding.Empty;
		}

		protected override void OnParentChanged ()
		{
			base.OnParentChanged ();
            if (Parent is ToolBarBase tb)
                ImageList = tb.Images;
        }

        public override Widget HitTest (float x, float y)
		{
			if (base.HitTest (x, y) != null)
				return this;
			return null;
		}
	}

	public class ToolBarButton : Button
	{		
		public IGuiMenuItem MenuItem { get; set; }

		public override bool Enabled {
			get {
				if (MenuItem != null)
					return MenuItem.Enabled;
				else
					return base.Enabled;
			}
			set {
                if (MenuItem != null)
					MenuItem.Enabled = value;
				base.Enabled = value;
			}
		}

		public override bool Visible {
			get {
				if (MenuItem != null)
					return MenuItem.Visible;
				else
					return base.Visible;
			}
			set {
				if (MenuItem != null)
					MenuItem.Visible = value;
				base.Visible = value;
			}
		}

		public override bool Checked {
			get {
				if (MenuItem != null)
					return MenuItem.Checked;
				else
					return base.Checked;
			}
			set {				
				if (MenuItem != null)
					MenuItem.Checked = value;
				base.Checked = value;
			}
		}

		public override char Icon {
			get {
				if (MenuItem != null && MenuItem.IsToggleButton && base.Icon == 0) {
					if (MenuItem.Checked)
						return (char)FontAwesomeIcons.fa_toggle_on;
					else
						return (char)FontAwesomeIcons.fa_toggle_off;
				}
				return base.Icon;
			}
			set {
				base.Icon = value;
			}
		}

		public ToolBarButton(string name, string text, char icon)
			: this (name, text, icon, ButtonDisplayStyles.ImageAndText, Color.Empty)
		{
		}

		public ToolBarButton(string name, string text, char icon, ButtonDisplayStyles displayStyle)
			: this (name, text, icon, displayStyle, Color.Empty)
		{
		}

		public ToolBarButton(string name, string text, char icon, ButtonDisplayStyles displayStyle, Color iconColor)
			: base(name, text, icon, new ComponentToolBarButtonStyle())
		{
			IsMenu = true;

			DisplayStyle = displayStyle;
			IconColor = iconColor;
			Styles.SetStyle (new ComponentToolBarButtonStyle (), WidgetStates.Selected);
			Styles.SetStyle (new ComponentToolBarButtonHoverStyle (), WidgetStates.Hover);
			Styles.SetStyle (new ComponentToolBarButtonPressedStyle (), WidgetStates.Pressed);
			Styles.SetStyle (new ComponentToolBarButtonDisabledStyle (), WidgetStates.Disabled);

			MaxSize = SizeMax;
			Margin = Padding.Empty;
			Padding = new Padding (6, 3, 6, 3);
			TextOffsetY = 0;
			this.SetFontByTag(CommonFontTags.Caption);
			CanFocus = false;
		}
			
		public override Widget HitTest (float x, float y)
		{
			if (base.HitTest (x, y) != null)
				return this;
			return null;
		}			

		public override void OnClick (OpenTK.Input.MouseButtonEventArgs e)
		{		
			base.OnClick (e);
			if (Parent as ToolBarBase != null)
				(Parent as ToolBarBase).OnButtonClick (this);			
		}
			
		public override void OnLayout (IGUIContext ctx, RectangleF bounds)
		{
			if (MenuItem != null) {
				// Update data from menu item
				base.Enabled = Enabled;
				base.Visible = Visible;
				base.Checked = Checked;
			}

			base.OnLayout (ctx, bounds);
		}			

		protected override void CleanupManagedResources ()
		{
			MenuItem = null;
			base.CleanupManagedResources ();
		}
	}

	public class ToolBarSeparator : Widget
	{
		public ToolBarSeparator(string name)
			: this(name, new MainToolBarSeparatorStyle())
		{
		}

		public ToolBarSeparator(string name, IWidgetStyle style)
			: base (name, Docking.Left, style)
		{
			m_Padding = new Padding (4, 6, 4, 6);
		}
			
		public override SizeF PreferredSize (IGUIContext ctx, SizeF proposedSize)
		{			
			return new SizeF (Padding.Width, Padding.Height);
		}
	}

	public class ToolBarLabel : TextWidget
	{
		public ToolBarLabel (string name, string text = null)
			: base(name)
		{
			m_Text = text;
			//InvalidateOnHeartBeat = true;
		}
	}


	// ***************** TOOL BAR ************************

	public abstract class ToolBarBase : Container
	{
		public IGuiMenu Menu { get; protected set; }
		public ImageList Images  { get; set; }

		protected ToolBarBase (string name, IGuiMenu menu, IWidgetStyle style) 
			: base(name, Docking.Top, style)
		{
			IsMenu = true;
			Menu = menu;
			if (Menu == null)
				Menu = new GuiMenu (name + "_menu");

			this.ZIndex = 1000;
			this.Padding = new Padding (3, 2, 3, 2);
			MinSize = new Size (0, 16);

			CanFocus = false;
			TabIndex = -1;

			RefreshMenu ();
		}			

		public virtual void RefreshMenu()
		{			
			if (Menu != null && Menu.Count > 0) {
				// clear all children with MenuItem
				Children.OfType<ToolBarButton> ().Where (c => c.MenuItem != null).ToList ().ForEach (btn => {
					if (Children.Remove(btn))
						btn.Dispose();
				});

				int separatorCount = 0;
				int subseparatorCount = 0;
				bool separatorFlag = false;
				foreach (var mainitem in Menu.Children) {
					if (mainitem != null && mainitem.Children != null && mainitem.Children.Count > 0) {
						subseparatorCount = 0;
						foreach (var item in mainitem.Children) {
							if (item != null && item.ShowOnToolBar) {
								if (item.IsSeparator && subseparatorCount > 1) {
									separatorFlag = true;
								}
								else if (item.HasImage && !item.IsToggleButton) {
									if (separatorFlag && Children.Count > 0) {
										separatorFlag = false;
										AddSeparator ("sep_" + (++separatorCount).ToString ());
									}
									AddMenuItem (item);
									subseparatorCount++;
								}
							}
						}
						separatorFlag = Children.Count > 0;
					}
				}

				/***
				Menu.SelectMany(m => m.Children)
					.Where(m => m != null && m.Parent != null && m.HasImage && m.ShowOnToolBar && !m.IsToggleButton)
					.ForEach (AddMenuItem);
				***/
			}
		}

		public virtual void OnButtonClick(object button)
		{
			ToolBarButton tb = button as ToolBarButton;
			if (tb != null) {
				IGuiMenuItem item = Menu.FindItem (tb.Name);
				if (item != null) {
					item.OnClick ();
				}
			}
		}

		public ToolBarButton AddMenuItem(IGuiMenuItem item)
		{
			if (item != null) {				
				ToolBarButton btn = AddMenuItem (item.Name, (char)item.ImageIndex, item.Text);
				if (btn != null)
					btn.MenuItem = item;
				return btn;
			}
			return null;
		}

		public ToolBarButton AddMenuItem(string name, char icon, string tooltip)
		{
			//if (icon > 0) {				
				ToolBarButton btn = new ToolBarButton (name, null, icon);
				btn.Tooltip = tooltip.StripMnemonics ();
				btn.SetIconFontByTag(CommonFontTags.LargeIcons);
				btn.Styles.GetStyle (WidgetStates.Default).ForeColorBrush.Color = Theme.Colors.Base3;
				btn.Styles.GetStyle (WidgetStates.Disabled).ForeColorBrush.Color = 
					Theme.GetContextDisabledForeColor(ColorContexts.Default);
				return AddChild(btn);
			//}
			//return null;
		}

		public void AddSeparator(string name)
		{						
			AddChild(new ToolBarSeparator (name));
		}

		public override bool Visible {
			get {
				return base.Visible && Children.Count > 0;
			}
			set {
				base.Visible = value;
			}
		}

		public override SizeF PreferredSize (IGUIContext ctx, SizeF proposedSize)
		{
			if (CachedPreferredSize == SizeF.Empty) {
				if (Children.Count == 0)
					return proposedSize;

				float w = 0;
				float h = 0;
				float rh = 0;

				foreach (Widget child in Children) {
					SizeF sz = child.PreferredSize (ctx, proposedSize);
					w += sz.Width;
					if (rh < sz.Height)
						rh = sz.Height;				
				}

				w += Padding.Width;
				h += rh + Padding.Height;

				CachedPreferredSize = new SizeF (proposedSize.Width, h);
			}
			return CachedPreferredSize;
		}

		protected override void CleanupManagedResources ()
		{			
			Menu = null;
			Images = null;
			base.CleanupManagedResources ();
		}
	}


	public class ApplicationToolBar : ToolBarBase
	{		
		public ToolBarButton HamburgerMenu { get; private set; }

		public ApplicationToolBar (string name, IGuiMenu menu) : this(name, menu, new MainToolBarStyle()) {}
		public ApplicationToolBar (string name, IGuiMenu menu, IWidgetStyle style) 
			: base(name, menu, style)
		{			
			HamburgerMenu = new ToolBarButton ("hamburger", null, (char)FontAwesomeIcons.fa_navicon);
			HamburgerMenu.Dock = Docking.Right;
			HamburgerMenu.ZIndex += 100;
			HamburgerMenu.Margin = new Padding (6, 3, 6, 3);
			HamburgerMenu.CanFocus = false;
			HamburgerMenu.Tooltip = "Sidebar Menu";
			//HamburgerMenu.IsToggleButton = true;

			//HamburgerMenu.Margin = new Padding (4);
			HamburgerMenu.SetIconFontByTag(CommonFontTags.MediumIcons);
			AddChild (HamburgerMenu);

			RefreshMenu ();
		}

		protected override void OnAddChild (Widget child)
		{
			base.OnAddChild (child);

			ToolBarButton button = child as ToolBarButton;
			if (button != null) {				
				if (button.Name == "hamburger") {					
					button.Styles.SetStyle (new MainToolBarButtonHoverStyle (), WidgetStates.Default);
					button.Styles.SetStyle (new MainToolBarButtonHoverStyle (), WidgetStates.Selected);
					button.Styles.SetStyle (new MainToolBarButtonHoverStyle (), WidgetStates.Hover);
					button.Styles.GetStyle (WidgetStates.Hover).BackColorBrush.Color = Theme.Colors.Base02;
					button.Styles.SetStyle (new MainToolBarButtonPressedStyle (), WidgetStates.Pressed);
				} else {					
					button.Styles.SetStyle (new MainToolBarButtonStyle (), WidgetStates.Default);
					button.Styles.SetStyle (new MainToolBarButtonStyle (), WidgetStates.Selected);
					button.Styles.SetStyle (new MainToolBarButtonHoverStyle (), WidgetStates.Hover);
					button.Styles.SetStyle (new MainToolBarButtonPressedStyle (), WidgetStates.Pressed);
				}
				button.Styles.SetStyle (new MainToolBarButtonDisabledStyle (), WidgetStates.Disabled);
				button.Padding = new Padding (6, 3);
				//CanFocus = false;
				button.SetIconFontByTag(CommonFontTags.MediumIcons);
				ResetCachedLayout ();
			}
		}

		public override void Clear ()
		{			
			for (int i = Children.Count - 1; i >= 0; i--) {
				Widget w = Children [i];
				if (w != null && w.Name != "hamburger")
					Children.RemoveAt (i);
			}
		}
			
		public bool HamburgerMenuVisible 
		{
			get {
				return HamburgerMenu != null && HamburgerMenu.Visible;
			}
		}

		/*** ***/
		public override void OnLayout (IGUIContext ctx, RectangleF bounds)
		{			
			if (IsLayoutSuspended)
				return;
			bounds.Height = PreferredSize (ctx).Height;
			this.SetBounds (bounds);
			float maxRight = bounds.Right;
			if (HamburgerMenuVisible) {
				HamburgerMenu.OnLayout (ctx, bounds);
				maxRight = Right - Padding.Width - (HamburgerMenu.Width * 2);
			}

			float x = Left + Padding.Left;
			//Children.OfType<ToolBarButton>().Where(c => !c.IsOverlay && c.MenuItem != null)
			Children.OfType<Widget>().Where(c => !c.IsOverlay && c != HamburgerMenu).ForEach (c => {
				if (c.IsOverlay) {
				}
				else if (x > maxRight) {
					c.Visible = false;
				}
				else {
					c.Visible = true;
					c.OnLayout(ctx, new RectangleF(x, Bounds.Top + Padding.Top + c.Margin.Top, c.PreferredSize(ctx).Width + c.Margin.Width, Bounds.Height - Padding.Height));
					x += c.Width + c.Margin.Width;
				}
			});
		}
	}

	public class ComponentToolBar : ToolBarBase
	{
		private IGUIFont Font;

		public ComponentToolBar(string name) : this (name, null) {}
		public ComponentToolBar(string name, IGuiMenu menu)
			: base(name, menu, new ComponentToolBarStyle())
		{
			Font = SummerGUIWindow.CurrentContext.FontManager.DefaultFont;
			Margin = Padding.Empty;
			Padding = new Padding (5, 1, 5, 1);
		}

		protected override void OnAddChild (Widget child)
		{
			ToolBarButton button = child as ToolBarButton;
			if (button != null) {
				button.Styles.SetStyle (new ComponentToolBarButtonStyle (), WidgetStates.Selected);
				button.Styles.SetStyle (new ComponentToolBarButtonStyle (), WidgetStates.Default);
				button.Styles.SetStyle (new ComponentToolBarButtonHoverStyle (), WidgetStates.Hover);
				button.Styles.SetStyle (new ComponentToolBarButtonPressedStyle (), WidgetStates.Pressed);
				button.Styles.SetStyle (new ComponentToolBarButtonDisabledStyle (), WidgetStates.Disabled);

				button.SetFontByTag(CommonFontTags.Default);
				button.SetIconFontByTag(CommonFontTags.SmallIcons);
			}
		}

		public override void RefreshMenu()
		{			
			if (Menu != null && Menu.Count > 0) {
				// clear all children with MenuItem
				Children.OfType<ToolBarButton> ().Where (c => c.MenuItem != null).ToList ().ForEach (btn => {
					if (Children.Remove(btn))
						btn.Dispose();
				});

				int separatorCount = 0;
				IGuiMenuItem lastItem = null;
				foreach (var item in Menu.Children) {
					if (item != null && item.ShowOnToolBar) {
						if (item.IsSeparator && lastItem != null && !lastItem.IsSeparator) {
							AddSeparator ("sep_" + (++separatorCount).ToString ());
						} else {
							AddMenuItem (item);
						}
						lastItem = item;
					}
				}
			}
		}

		public override SizeF PreferredSize (IGUIContext ctx, SizeF proposedSize)
		{
			if (Font == null)
				return base.PreferredSize (ctx, proposedSize);
			else				
				return new SizeF (proposedSize.Width, Font.TextBoxHeight + Padding.Height);
		}
			
		protected override void CleanupManagedResources ()
		{			
			Font = null;
			base.CleanupManagedResources ();
		}			
	}
}

