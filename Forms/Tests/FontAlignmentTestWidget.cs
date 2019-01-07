using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using KS.Foundation;

namespace SummerGUI
{
	public class FontAlignmentTestWidget : Widget
	{
		public IGUIFont Font { get; set; }
		public string Text { get; set; }
		public FontFormat Format { get; set; }

		public FontAlignmentTestWidget ()
			: base ("test", Docking.Fill, new WidgetStyle())
		{	
			Style.BackColorBrush.Color = Color.Lime;
			Text = "AbcdefghijklmnOPQrstUVwxyz";

			Margin = Padding.Empty;
			Padding = Padding.Empty;
			//Padding = new Padding(16);

			Font = SummerGUIWindow.CurrentContext.FontManager.DefaultFont;

			Format = new FontFormat (Alignment.Center, Alignment.Center, FontFormatFlags.None);
		}

		protected override void OnParentChanged ()
		{
			base.OnParentChanged ();
			//Font = this.ParentWindow.FontManager.DefaultFont;
		}

		public override SizeF PreferredSize (IGUIContext ctx, SizeF proposedSize)
		{
			SizeF sz = Font.Measure (Text).Inflate(Padding);
			this.MaxSize = sz;
			return sz;
		}

		public override void OnPaint (IGUIContext ctx, RectangleF bounds)
		{
			base.OnPaint (ctx, bounds);

			RectangleF paddingBounds = PaddingBounds;

			float y = paddingBounds.Top + (paddingBounds.Height / 2f);
			//ctx.DrawLine (Theme.Pens.Red, paddingBounds.Left, y, paddingBounds.Right, y);

			Point p = BoxAlignment.AlignBoxes (paddingBounds, bounds, Format, Font.Ascender, Font.Descender);
			y = p.Y;
			ctx.DrawLine (Theme.Pens.Red, paddingBounds.Left, y, paddingBounds.Right, y);

			ctx.DrawString (Text, Font, Style.ForeColorBrush, bounds, Format);
		}
	}
}

