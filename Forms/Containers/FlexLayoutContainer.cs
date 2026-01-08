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
				float extra;
				int visibleChildCount = 0;
				SizeF sz;

				switch (FlexDirection) {
				case FlexDirections.Row:
				case FlexDirections.RowReverse:
				case FlexDirections.RowCenter:
				//extra = Padding.Width;
					extra = ItemDistance;
					for (int i = 0; i < Children.Count; i++) {	
						Widget child = Children [i];
						if (child.Visible) {
							visibleChildCount++;
							sz = child.PreferredSize (ctx);
							w += sz.Width + child.Margin.Width;
							h = Math.Max (h, sz.Height);
						}
					}
					w += extra * (visibleChildCount - 1);
				//h += Padding.Height;
					h += Margin.Height;
					break;
				case FlexDirections.Column:
				case FlexDirections.ColumnReverse:
				case FlexDirections.ColumnCenter:
				//extra = Padding.Height;
					extra = ItemDistance;
					for (int i = 0; i < Children.Count; i++) {
						Widget child = Children [i];
						if (child.Visible) {
							sz = child.PreferredSize (ctx);
							w = Math.Max (w, sz.Width);
							h += sz.Height + extra + child.Margin.Height;
						}
					}
					w += extra;
					break;
				}
				
				CachedPreferredSize = new SizeF (w + Padding.Width, h + Padding.Height);
			}

			return CachedPreferredSize;
		}
			
		private void LayoutRowReverse(IGUIContext ctx, RectangleF bounds)
		{
			float dh = ItemDistance / 2;
			if (this.Children.Count > 0) {
				RectangleF r = bounds;
				r.X = bounds.Right;
				for (int i = Children.Count - 1; i >= 0; i--)
				//for (int i = 0; i < Children.Count; i++)
				{
					Widget child = Children [i];

					if (child.Visible) {
						float w = child.Bounds.Width;
						//r.X -= w + Padding.Right + child.Margin.Right;
						r.X -= w + dh + child.Margin.Right;
						r.Width = w;
						//r = r.Inflate (child.Margin);
						LayoutChild(ctx, child, r);
						//r.X -= Padding.Left + child.Margin.Left;
						r.X -= dh + child.Margin.Left;
					}
				}
			}
		}

		private void LayoutRowForward(IGUIContext ctx, RectangleF bounds)
		{
			float dh = ItemDistance / 2;
			if (this.Children.Count > 0) {
				RectangleF r = bounds;
				r.X = bounds.Left;
				for (int i = 0; i < Children.Count; i++)
				{
					Widget child = Children [i];

					if (child.Visible) {
						float w = child.Bounds.Width;
						r.X += dh + child.Margin.Left;
						r.Width = w;
						LayoutChild(ctx, child, r);
						r.X += w + dh + child.Margin.Right;
					}
				}
			}
		}

		private void LayoutRowCentered(IGUIContext ctx, RectangleF bounds)
		{
			if (this.Children.Count > 0) {

				float contentWidth = Children.Where(c => c.Visible).Sum (c => c.Bounds.Width + c.Margin.Width) 
					+ ((Children.Count(c => c.Visible)) * Padding.Width);

				RectangleF r = bounds;
				float dh = ItemDistance / 2;

				// ALWAYS ADD THE OFFSET FROM THE BOUNDS  !!

				r.X = ((r.Width) / 2f) - (contentWidth / 2f) + bounds.Left;
				for (int i = 0; i < Children.Count; i++)
				{
					Widget child = Children [i];
					if (child.Visible) {
						float w = child.Bounds.Width;
						r.X += dh + child.Margin.Left;
						r.Width = w;
						LayoutChild(ctx, child, r);
						r.X += w + dh + child.Margin.Right;
					}
				}
			}
		}

		protected override void LayoutChildren (IGUIContext ctx, RectangleF bounds)
		{
			switch (FlexDirection) {
			case FlexDirections.RowReverse:
				LayoutRowReverse (ctx, bounds);
				break;
			case FlexDirections.RowCenter:
				LayoutRowCentered (ctx, bounds);
				break;
			default:
				LayoutRowForward (ctx, bounds);
				//base.LayoutChildren (ctx, bounds);
				break;
			}				
		}

		protected override void LayoutChild (IGUIContext ctx, Widget child, RectangleF bounds)
		{
			base.LayoutChild (ctx, child, bounds);
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

