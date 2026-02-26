using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Diagnostics;
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
	public enum FlexDirections
	{
		Row,
		RowReverse,
		Column,
		ColumnReverse,
		RowCenter,
		ColumnCenter
	}

	public class FlexLayoutContainer : Container
	{
		public FlexDirections FlexDirection { get; set; }

		float m_ItemDistance;
		[DpiScalable]
		public float ItemDistance 
		{ 
			get {
				return m_ItemDistance;
			}
			set {
				if (m_ItemDistance != value) {
					m_ItemDistance = value;
					OnItemDistanceChanged();
				}
			}
		}

		protected virtual void OnItemDistanceChanged()
		{
			ResetCachedLayout();
		}

		public FlexLayoutContainer (string name) : this(name, Docking.Fill, new WidgetStyle()) {}
		public FlexLayoutContainer (string name, Docking dock, IWidgetStyle style)
			: base(name, dock, style)
		{
			Padding = new Padding (4);
			ItemDistance = 4;
		}
			
		public override SizeF PreferredSize (IGUIContext ctx, SizeF proposedSize)
		{			
			if (CachedPreferredSize == SizeF.Empty) {

				float w = 0, h = 0;				
				int visibleChildCount = 0;
				SizeF sz;

				switch (FlexDirection) {
				case FlexDirections.Row:
				case FlexDirections.RowReverse:
				case FlexDirections.RowCenter:
					for (int i = 0; i < Children.Count; i++) {	
						Widget child = Children [i];
						if (child.Visible) {
							visibleChildCount++;
							sz = child.PreferredSize (ctx);
							w += sz.Width + child.Margin.Width;
							h = Math.Max (h, sz.Height + child.Margin.Height);
						}
					}
					w += ItemDistance * (visibleChildCount - 1);
					break;
				case FlexDirections.Column:
				case FlexDirections.ColumnReverse:
				case FlexDirections.ColumnCenter:					
					for (int i = 0; i < Children.Count; i++) {
						Widget child = Children [i];
						if (child.Visible) {
							visibleChildCount++;
							sz = child.PreferredSize (ctx);
							h += sz.Height + child.Margin.Height;
							w = Math.Max (w, sz.Width + child.Margin.Width);
						}
					}
					h += ItemDistance * (visibleChildCount - 1);
					break;
				}
				
				CachedPreferredSize = new SizeF (w + Padding.Width, h + Padding.Height);				
			}

			return CachedPreferredSize;
		}

		private void LayoutRowForward(IGUIContext ctx, RectangleF bounds)
		{			
			if (Children.Count > 0) {
				RectangleF r = bounds;
				r.X = bounds.Left + Padding.Left;
				for (int i = 0; i < Children.Count; i++)
				{
					Widget child = Children [i];

					if (child.Visible) {
						float w = child.PreferredSize (ctx).Width;
						r.X += child.Margin.Left;
						r.Width = w + child.Margin.Width;
						LayoutChild(ctx, child, r);
						r.X += w + child.Margin.Right + ItemDistance;
					}
				}
			}
		}
			
		private void LayoutRowReverse(IGUIContext ctx, RectangleF bounds)
		{			
			if (Children.Count > 0) {
				RectangleF r = bounds;
				r.X = bounds.Right - Padding.Right;
				for (int i = Children.Count - 1; i >= 0; i--)				
				{
					Widget child = Children [i];

					if (child.Visible) {
						float w = child.PreferredSize (ctx).Width;
						r.X -= w + child.Margin.Right;
						r.Width = w + child.Margin.Width;
						LayoutChild(ctx, child, r);
						r.X -= child.Margin.Left + ItemDistance;
					}
				}
			}
		}		

		private void LayoutRowCentered(IGUIContext ctx, RectangleF bounds)
		{
			if (this.Children.Count > 0) {
				
				float contentWidth = Children.Where(c => c.Visible).Sum (c => c.Bounds.Width + c.Margin.Width);
				contentWidth += (Children.Count(c => c.Visible) - 1) * ItemDistance;

				RectangleF r = bounds;
				
				r.X = (r.Width - contentWidth) / 2f + bounds.Left;
				for (int i = 0; i < Children.Count; i++)
				{
					Widget child = Children [i];
					if (child.Visible) {
						float w = child.PreferredSize (ctx).Width;
						r.X += child.Margin.Left;
						r.Width = w + child.Margin.Width;
						LayoutChild(ctx, child, r);
						r.X += w + child.Margin.Right + ItemDistance;
					}
				}
			}
		}

		private void LayoutColumnForward(IGUIContext ctx, RectangleF bounds)
		{
			if (Children.Count > 0) {
				RectangleF r = bounds;
				r.Y = bounds.Top + Padding.Top;
				for (int i = 0; i < Children.Count; i++)
				{
					Widget child = Children [i];

					if (child.Visible) {						
						float h = child.PreferredSize (ctx).Height;
						r.Y += child.Margin.Top;
						r.Height = h + child.Margin.Height;
						LayoutChild(ctx, child, r);
						r.Y += h + child.Margin.Bottom + ItemDistance;
					}
				}
			}
		}        

		private void LayoutColumnReverse(IGUIContext ctx, RectangleF bounds)
		{
			if (Children.Count > 0) {
				RectangleF r = bounds;
				r.Y = bounds.Bottom - Padding.Bottom;
				for (int i = Children.Count - 1; i >= 0; i--)				
				{
					Widget child = Children [i];

					if (child.Visible) {
						float h = child.PreferredSize (ctx).Height;
						r.Y -= h + child.Margin.Bottom;
						r.Height = h + child.Margin.Height;
						LayoutChild(ctx, child, r);						
						r.Y -= child.Margin.Top + ItemDistance;
					}
				}
			}
		}

		private void LayoutColumnCentered(IGUIContext ctx, RectangleF bounds)
		{
			if (this.Children.Count > 0) {
				
				float contentHeight = Children.Where(c => c.Visible).Sum (c => c.Bounds.Height + c.Margin.Height);
				contentHeight += (Children.Count(c => c.Visible) - 1) * ItemDistance;

				RectangleF r = bounds;
				
				r.Y = (r.Height - contentHeight) / 2f + bounds.Top;
				for (int i = 0; i < Children.Count; i++)
				{
					Widget child = Children [i];
					if (child.Visible) {
						float h = child.PreferredSize (ctx).Height;
						r.Y += child.Margin.Top;
						r.Height = h + child.Margin.Height;
						LayoutChild(ctx, child, r);
						r.Y += h + child.Margin.Bottom + ItemDistance;
					}
				}
			}
		}

		protected override void LayoutChildren (IGUIContext ctx, RectangleF bounds)
		{
			switch (FlexDirection) {
			case FlexDirections.Row:
				LayoutRowForward (ctx, bounds);
				break;
			case FlexDirections.RowReverse:
				LayoutRowReverse (ctx, bounds);
				break;
			case FlexDirections.RowCenter:
				LayoutRowCentered (ctx, bounds);
				break;			
			case FlexDirections.Column:
				LayoutColumnForward (ctx, bounds);
				break;
			case FlexDirections.ColumnReverse:
				LayoutColumnReverse (ctx, bounds);
				break;
			case FlexDirections.ColumnCenter:
				LayoutColumnCentered (ctx, bounds);
				break;			
			}				
		}		
	}

	public class ButtonContainerWidgetStyle : WidgetStyle
	{
		public override void InitStyle ()
		{
			SetBackColor (Theme.Colors.Base3);
			//SetBackColor (Color.FromArgb(Theme.DefaultButtonAlpha, Theme.Colors.Base0), Color.FromArgb(Theme.DefaultButtonAlpha, Theme.Colors.Base00));
			//SetBackColor (Theme.Colors.Base1, Theme.Colors.Base00);
			SetForeColor (Theme.Colors.Base02);
			SetBorderColor (Color.Empty);
		}
	}

	public class ButtonContainer : FlexLayoutContainer 
	{		
		public ButtonContainer(string name) : this (name, Docking.Bottom, new ButtonContainerWidgetStyle()) {}
		public ButtonContainer(string name, Docking dock, IWidgetStyle style) : base (name, dock, style) {
			FlexDirection = FlexDirections.RowReverse;
			ItemDistance = 15;
			Padding = new Padding (6);			
		}

		protected override void OnAddChild (Widget child)
		{
			base.OnAddChild (child);
			Button btn = child as Button;
			if (btn != null) {				
				btn.MinSize = new SizeF (96, btn.MinSize.Height);				
			}
		}
			
		public override void Focus ()
		{
			Widget w = this.TabSupportingChildren ().FirstOrDefault ();
			if (w != null)
				w.Focus ();
			else
				base.Focus ();
		}

		public Button ActiveButton
		{
			get{
				Button w = Children.OfType<Button> ().FirstOrDefault (b => b.IsFocused);
				if (w == null)
					w = Children.OfType<Button>().FirstOrDefault(b => b.IsDefaultButton);
				return w;
			}
		}

		public override bool OnKeyDown (KeyboardKeyEventArgs e)
		{
			Button def = ActiveButton;
			if (def != null && def.OnKeyDown (e))
				return true;
			return base.OnKeyDown (e);
		}

		protected override void CleanupManagedResources ()
		{			
			base.CleanupManagedResources ();
		}
	}
}

