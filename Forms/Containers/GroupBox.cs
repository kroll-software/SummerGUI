using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using KS.Foundation;
using System.Drawing;

namespace SummerGUI
{
	public enum GroupBoxBorderStyles
	{
		None,
		Single,
		Double
	}

	public class GroupBoxWidgetStyle : WidgetStyle
	{
		
	}

	public class GroupBox : Container
	{
		public string Caption { get; set; }
		public GroupBoxBorderStyles BorderStyle { get; set; }

		public IGUIFont Font {get; set; }

		public GroupBox (string name, string caption = null)
			: base (name)
		{
			Caption = caption;
			BorderStyle = GroupBoxBorderStyles.Single;
			ForeColor = Theme.Colors.Base00;
			this.SetFontByTag(CommonFontTags.Small);

			//this.BorderColor = Theme.Colors.Base1;
			this.BorderColor = Theme.Colors.Silver;

			this.Padding = new Padding(12, 16, 12, 12);			
		}

		public virtual void DrawBorder(IGUIContext ctx, System.Drawing.RectangleF bounds)
		{
			float margin = 4f * ScaleFactor;
			RectangleF r = new RectangleF(bounds.X + margin, bounds.Top + (Font.Height / 2), bounds.Width - (margin * 2), bounds.Height - (Font.Height / 2) - margin);

			Pen pen = new Pen(this.BorderColor);
			ctx.DrawRoundedRectangle(pen, r, 6f);			
		}

        protected override void OnParentChanged()
        {
            base.OnParentChanged();

			if (this.BackColor != Color.Empty)
				return;

			// Try to get a valid BackColor from Parent, recursive
			
			Color c = Color.Empty;
			Widget parent = Parent;			
			while (c != Color.Empty && parent != null)
			{
				c = parent.BackColor;
				parent = parent.Parent;
			}

			if (c == Color.Empty)
				c = Color.White;

			this.BackColor = c;			
        }

		public virtual void DrawCaption(IGUIContext ctx, System.Drawing.RectangleF bounds)
		{
			if (Font == null || String.IsNullOrEmpty(Caption))
				return;

			float margin = 16f * ScaleFactor;
			float backMargin = 4f * ScaleFactor;
			RectangleF r = new RectangleF(bounds.X + margin, bounds.Top, bounds.Width - margin, Font.LineHeight);

			SizeF sz = ctx.MeasureString(Caption, Font, r, FontFormat.DefaultSingleLine);
			Brush back = new SolidBrush(this.BackColor);			
			RectangleF rback = new RectangleF(r.Left - backMargin, r.Top, sz.Width + (backMargin * 2), r.Height);
			ctx.FillRectangle(back, rback);

			Brush brush = new SolidBrush(this.ForeColor);
			ctx.DrawString(Caption, Font, brush, r, FontFormat.DefaultSingleLine);
		}

		public override void OnPaintBackground (IGUIContext ctx, System.Drawing.RectangleF bounds)
		{
			base.OnPaintBackground (ctx, bounds);
			DrawBorder (ctx, bounds);
			DrawCaption (ctx, bounds);
		}			
	}
}

