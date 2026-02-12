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
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using KS.Foundation;
using System.Drawing;

namespace SummerGUI
{
	public class CheckedListBox : ListBox
	{

		public event EventHandler<EventArgs> CheckedChanged;
		public void OnCheckedChanged()
		{
			if (CheckedChanged != null && !IsDisposed)
				CheckedChanged (this, EventArgs.Empty);			
		}

		IGUIFont m_IconFont;
		public IGUIFont IconFont
		{ 
			get {
				return m_IconFont;
			}
			set {
				if (m_IconFont != value) {
					m_IconFont = value;
					OnFontChanged ();
				}
			}
		}
		
		[DpiScalable]
		public Size IconMargin { get; set; }


		public CheckedListBox (string name)
			: base (name)
		{
			TextMargin = new Size(25, 0);
			IconMargin = new Size(6, 1);
			MultiSelect = false;			
			m_IconFont = FontManager.Manager.SmallIcons;			
		}

        public override void DrawItem(IGUIContext ctx, RectangleF bounds, ListBoxItem item, IWidgetStyle style)
        {
			base.DrawItem(ctx, bounds, item, style);
						
			if (IconFont != null) {
				char icon = item.Checked ? (char)FontAwesomeIcons.fa_check_square_o : (char)FontAwesomeIcons.fa_square_o;			
				RectangleF iconBounds = bounds;
				float cp = Font.CaptionHeight;
				iconBounds.Offset (IconMargin.Width, IconMargin.Height);
				ctx.DrawString (icon.ToString(), IconFont, style.ForeColorBrush, iconBounds, FontFormat.DefaultIconFontFormatLeft);				
			}
        }

		protected void ToggleChecked(int idx)
		{
			if (idx < 0 || idx >= Items.Count)
				return;
			ListBoxItem item = Items[idx];
			item.Checked = !item.Checked;
			OnCheckedChanged();
			Invalidate();
		}

		public override bool OnKeyDown (KeyboardKeyEventArgs e)
		{			
			if (!IsFocused)			
				return false;

			switch (e.Key) {
			case Keys.Space:
				ToggleChecked(SelectedIndex);
				return true;
			}

			return base.OnKeyDown (e);
		}

        public override void OnClick(MouseButtonEventArgs e)
        {
            base.OnClick(e);
			
			float x = e.X - Left;
			if (x > IconMargin.Width && x < TextMargin.Width)
			{
				int idx = (int)((e.Y - Bounds.Top + VScrollBar.Value) / ItemHeight);
				ToggleChecked(idx);
			}
		}
	}
}

