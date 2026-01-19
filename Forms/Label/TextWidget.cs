using System;
using System.Diagnostics;
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
	public class TextWidget : Widget
	{				
		public TextWidget (string name) : this(name, Docking.None, new DefaultTextWidgetStyle(), null, null) {}		

		public TextWidget (string name, Docking dock, IWidgetStyle style, string text, IGUIFont font)
			: base(name, dock, style)
		{
			this.m_Padding = new Padding (3);

			this.HAlign = Alignment.Near;
			this.VAlign = Alignment.Center;

			if (text == null)
				m_Text = String.Empty;
			else
				m_Text = text;

			if (font == null)
				m_Font = FontManager.Manager.DefaultFont;
			else
				m_Font = font;

			m_IconFont = FontManager.Manager.SmallIcons;
			Format = FontFormat.DefaultSingleLineCentered;
			CanFocus = false;
		}
			
		protected string m_Text = String.Empty;
		public virtual string Text
		{
			get{
				return m_Text;
			}
			set {
				if (m_Text != value) {
					m_Text = value;
					OnTextChanged ();
				}
			}
		}			

		public virtual void OnTextChanged()
		{
			ResetCachedLayout ();
			Invalidate (1);
		}			
			
		protected char m_Icon = (char)0;
		public virtual char Icon
		{
			get{
				return m_Icon;
			}
			set {
				if (m_Icon != value) {
					m_Icon = value;
					OnIconChanged ();
				}
			}
		}

		public virtual void OnIconChanged()
		{
			ResetCachedLayout ();
			Invalidate ();
		}
			
		private IGUIFont m_Font;
		public IGUIFont Font
		{
			get{				
				return m_Font;
			}
			set{
				if (m_Font != value) {
					m_Font = value;
					OnFontChanged ();
				}
			}
		}

		public virtual void OnFontChanged()
		{
			ResetCachedLayout ();
		}

		IGUIFont m_IconFont;
		public IGUIFont IconFont
		{
			get{			
				return m_IconFont;
			}
			set{
				if (m_IconFont != value) {
					m_IconFont = value;
					OnIconFontChanged();
				}
			}
		}

		public virtual void OnIconFontChanged()
		{
			ResetCachedLayout ();
		}

		protected override void OnParentChanged ()
		{
			if (!IsDisposed) {
				if (Parent != null) {
					if (Font == null && ReflectionUtils.HasProperty (Parent.GetType (), "Font"))
						Font = ReflectionUtils.GetPropertyValue (Parent.GetType (), "Font") as IGUIFont;

					if (IconFont == null && ReflectionUtils.HasProperty (Parent.GetType (), "IconFont"))
						IconFont = ReflectionUtils.GetPropertyValue (Parent.GetType (), "IconFont") as IGUIFont;
				}

				if (Font == null)
					this.SetFontByTag (CommonFontTags.Default);

				if (IconFont == null)
					this.SetIconFontByTag (CommonFontTags.SmallIcons);
			}

			base.OnParentChanged ();
		}

		public override SizeF PreferredSize (IGUIContext ctx, SizeF proposedSize)
		{
			if (CachedPreferredSize == SizeF.Empty) {

				float w = 0;
				float h = 0;

				if (IconFont != null && Icon != 0) {
					SizeF sz = IconFont.Measure (Icon.ToString ());
					h = sz.Height;
					w += sz.Width * 1.5f;
				}

				if (Font != null && !String.IsNullOrEmpty (Text)) {
					if (Format.HasFlag (FontFormatFlags.WrapText) && proposedSize.Width - w > 0) {
						SizeF sz = Font.Measure (Text, proposedSize.Width - w - Padding.Width, Format);
						h = Math.Max (h, sz.Height);
						w += sz.Width + 4;
					} else {
						SizeF sz = Font.Measure (Text);
						h = Math.Max (h, sz.Height);
						w += sz.Width;
					}
				}					

				CachedPreferredSize = new SizeF (w + Padding.Width, h + Padding.Height);
			}				

			return CachedPreferredSize;
		}

		public FontFormat Format { get; set; }

		public override void OnPaint (IGUIContext ctx, RectangleF bounds)
		{		
			bounds = PaddingBounds;

			if (IconFont != null && Icon != 0) {				
				RectangleF iconRect = bounds;				
				SizeF iconWidth;
				if (String.IsNullOrEmpty (Text)) {					
					iconWidth = ctx.DrawString (Icon.ToString(), IconFont, Style.ForeColorBrush, iconRect, FontFormat.DefaultIconFontFormatCenter);
				}
				else {
					iconWidth = ctx.DrawString (Icon.ToString(), IconFont, Style.ForeColorBrush, iconRect, FontFormat.DefaultIconFontFormatLeft);
					float offset = iconWidth.Width * 1.5f;
					bounds.X += offset;
					bounds.Width -= offset;
				}
			}

			if (Font != null && !String.IsNullOrEmpty(Text)) {				
				Debug.WriteLine($"Bounds: {bounds}");
				if (ctx.DrawString (Text, Font, Style.ForeColorBrush, bounds, Format).Width > bounds.Width && !Format.HasFlag(FontFormatFlags.WrapText))
					Tooltip = Text;
				else
					Tooltip = null;
			}
		}

		protected override void CleanupManagedResources ()
		{
			m_Font = null;
			m_IconFont = null;
			base.CleanupManagedResources ();
		}
	}
}

