using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using KS.Foundation;

namespace SummerGUI
{
	public class SplitButton : Button
	{
		public SplitButton (string name, string text) : this(name, text, (char)0, ColorContexts.Default) {}
		public SplitButton (string name, string text, ColorContexts colorContext) : this (name, text, (char)0, colorContext) {}
		public SplitButton (string name, string text, char icon = (char)0, ColorContexts colorContext = ColorContexts.Default)
			: this(name, text, icon, new ButtonWidgetStyle(colorContext))
		{			
			Styles.SetStyle (new ButtonHoverWidgetStyle (colorContext), WidgetStates.Hover);
			Styles.SetStyle (new ButtonPressedWidgetStyle (colorContext), WidgetStates.Pressed);
			Styles.SetStyle (new ButtonDisabledWidgetStyle (colorContext), WidgetStates.Disabled);

			m_ColorContext = colorContext;
		}	

		public SplitButton (string name, string text, char icon, IWidgetStyle style)
			: base(name, text, icon, style)
		{
			Padding = new Padding (4, 4, 18, 4);
		}

		public override SizeF PreferredSize (IGUIContext ctx, SizeF proposedSize)
		{
			SizeF sz = base.PreferredSize (ctx, proposedSize);
			SizeF szplus = new SizeF(sz.Width + (18f * ScaleFactor) + Padding.Top + Padding.Top, sz.Height);
			//MinSize = szplus;
			return szplus;
		}

		public override void OnPaint (IGUIContext ctx, RectangleF bounds)
		{
			base.OnPaint (ctx, bounds);

			if (IconFont == null)
				return;

			RectangleF iconBounds = new RectangleF (bounds.Left + Padding.Left, 
				bounds.Top + Padding.Top,
				bounds.Width - Padding.Left - Padding.Top, 
				bounds.Height - Padding.Top - Padding.Bottom);

			SizeF fw = ctx.DrawString (((char)FontAwesomeIcons.fa_caret_down).ToString (), IconFont, 
				Style.ForeColorBrush, iconBounds, FontFormat.DefaultIconFontFormatFar);

			float x = bounds.Right - Padding.Top - fw.Width - Padding.Top;
			ctx.DrawLine (Theme.Pens.Base03, x, bounds.Top + Padding.Top, x, bounds.Bottom - Padding.Bottom);
			ctx.DrawLine (Theme.Pens.Base01, x + 1, bounds.Top + Padding.Top, x + 1, bounds.Bottom - Padding.Bottom);
		}
	}
}

