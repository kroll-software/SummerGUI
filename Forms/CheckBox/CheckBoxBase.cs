using System;
using System.Drawing;
using System.Text;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Input;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using KS.Foundation;

namespace SummerGUI
{
	public abstract class CheckBoxBase : TextWidget
	{
		public event EventHandler<EventArgs> CheckedChanged;
		public void OnCheckedChanged()
		{
			if (CheckedChanged != null)
				CheckedChanged (this, EventArgs.Empty);
		}

		protected bool m_Checked;
		public virtual bool Checked
		{
			get{
				return m_Checked;
			}
			set{
				if (m_Checked != value) {
					m_Checked = value;
					Invalidate ();
					OnCheckedChanged ();
				}
			}
		}			

		int m_Indent;
		public int Indent 
		{ 
			get {
				return m_Indent;
			}
			set {
				if (m_Indent != value) {
					m_Indent = value;
					OnIndentChanged ();
				}
			}
		}

		protected virtual void OnIndentChanged()
		{
			ResetCachedLayout ();
		}

		protected char[] CheckChars;
		protected float IconOffsetY;

		protected CheckBoxBase (string name, string text)
			: base (name, Docking.Fill, new DefaultTextWidgetStyle(), text, SummerGUIWindow.CurrentContext.FontManager.DefaultFont)
		{			
			IconFont = SummerGUIWindow.CurrentContext.FontManager.SmallIcons;
			//Margin = new Padding (3);
			Margin = Padding.Empty;
			//Padding = new Padding (8, 4, 4, 4);
			Padding = new Padding (8, 0, 4, 0);
			IconOffsetY = 0.25f;

			Styles.SetStyle (new DisabledTextWidgetStyle (), WidgetStates.Disabled);
			Styles.SetStyle (new ActiveTextWidgetStyle (), WidgetStates.Active);
			CanFocus = true;
		}			
			
		public override bool OnKeyDown (KeyboardKeyEventArgs e)
		{
			base.OnKeyDown (e);

			if (!IsFocused)
				return false;

			switch (e.Key) {
			case Key.Space:
				OnClick (null);
				return true;
			}

			return false;
		}
			
		public override SizeF PreferredSize (IGUIContext ctx, SizeF proposedSize)
		{
			if (CachedPreferredSize == System.Drawing.Size.Empty) {
				if (!String.IsNullOrEmpty (Text)) {
					SizeF sz = Font.Measure (Text);
					float iconWidth = Font.CaptionHeight * (Indent + 1);
					CachedPreferredSize = new SizeF (sz.Width + iconWidth + Padding.Width, Font.TextBoxHeight);
				}
			}
			return CachedPreferredSize;
		}

		public override void OnPaint (IGUIContext ctx, RectangleF bounds)
		{
			//base.OnPaint (ctx, bounds);
			//bounds = bounds.Inflate (Padding);
			bounds = new RectangleF(bounds.Left + Padding.Left, bounds.Top, bounds.Width - Padding.Width, bounds.Height);

			float iconWidth = 0;
			if (IconFont != null) {
				char icon = CheckChars [Checked.ToInt ()];
				RectangleF iconBounds = bounds;
				float cp = Font.CaptionHeight;
				iconBounds.Offset (cp * Indent, IconOffsetY);
				ctx.DrawString (icon.ToString (), IconFont, Style.ForeColorBrush, iconBounds, FontFormat.DefaultSingleBaseLine);
				iconWidth = cp * (Indent + 1);
			}

			ctx.DrawString (Text, Font, Style.ForeColorBrush, new RectangleF (
				bounds.Left + iconWidth,
				bounds.Top,
				bounds.Width - iconWidth,
				bounds.Height), 
				FontFormat.DefaultSingleLine);
		}
	}
}
