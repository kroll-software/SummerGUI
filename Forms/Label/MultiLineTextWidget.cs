using System;
using System.Drawing;
using System.Linq;
using KS.Foundation;

namespace SummerGUI
{
	public class MultiLineTextWidget : Widget
	{
		string m_Text;
		public virtual string Text 
		{ 
			get {
				return m_Text;
			}
			set {
				if (m_Text != value) {
					m_Text = value;
					OnTextChanged ();
				}
			}
		}

		protected virtual void OnTextChanged()
		{
			ResetCachedLayout ();
		}

		IGUIFont m_Font;
		public IGUIFont Font 
		{ 
			get {
				return m_Font;
			}
			set {
				if (m_Font != value) {
					m_Font = value;
					OnFontChanged ();
				}
			}
		}

		protected virtual void OnFontChanged()
		{
			ResetCachedLayout ();
		}

		FontFormat m_Format;
		bool formatUserSet;
		public FontFormat Format 
		{ 
			get {
				return m_Format;
			}
			set {
				if (m_Format != value) {
					m_Format = value;
					formatUserSet = true;
					OnFormatChanged ();
				}
			}
		}

		protected virtual void OnFormatChanged()
		{
			ResetCachedLayout ();
		}

		/***
		public MultiLineTextWidget (string name, string text = null)
			: this(name, text, new DefaultTextWidgetStyle())
		{
		}
		***/

		public MultiLineTextWidget (string name, string text = null, IWidgetStyle style = null)
			: base(name, Docking.Fill, style)
		{
			Text = text;
			this.SetFontByTag (CommonFontTags.Default);
			m_Format = FontFormat.DefaultMultiLine;
		}			

		public override SizeF PreferredSize (IGUIContext ctx, SizeF proposedSize)
		{
			if (CachedPreferredSize == SizeF.Empty) {
				if (Font == null || Text == null) {
					return base.PreferredSize (ctx, proposedSize);
				}
				else {
					//SizeF sz = Font.Measure (Text, proposedSize.Width - Padding.Width, FontFormat.DefaultMultiLine);

					if (!formatUserSet) {
						m_Format = new FontFormat (HAlign, VAlign, FontFormatFlags.WrapText);
					}

					SizeF sz = Font.Measure (Text, proposedSize.Width - Padding.Width, Format);

					CachedPreferredSize = new SizeF (sz.Width + Padding.Width, sz.Height + Padding.Height);
				}
			}
			return CachedPreferredSize;
		}

		public override void OnLayout (IGUIContext ctx, RectangleF bounds)
		{			
			if (IsLayoutSuspended)
				return;

			if (Dock != Docking.Fill) {
				this.SetBounds (bounds.Location, new SizeF (bounds.Width, PreferredSize (ctx, bounds.Size).Height));			
				base.OnLayout (ctx, Bounds);
			} else {
				base.OnLayout (ctx, bounds);
			}
		}
			
		public override void OnPaint (IGUIContext ctx, RectangleF bounds)
		{		
			bounds = bounds.Inflate(Padding);
			if (Font != null && Text != null)
				ctx.DrawString (Text, Font, Style.ForeColorBrush, bounds, Format);
		}

		protected override void CleanupManagedResources ()
		{
			m_Font = null;
			base.CleanupManagedResources ();
		}
	}
}

