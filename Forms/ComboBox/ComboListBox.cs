using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using KS.Foundation;

namespace SummerGUI
{
	public class ComboListBoxTextWidgetStyle : WidgetStyle
	{
		public override void InitStyle ()
		{
			SetBackColor (Color.Empty);
			SetForeColor(Theme.Colors.Base02);
			SetBorderColor(Theme.Colors.Base01);
		}
	}

	public class ComboListBox : ComboBoxBase
	{		

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

		[DpiScalable]
		public Size TextMargin { get; set; }

		public ComboListBox (string name) 
			: base(name)
		{						
			InsertButton ();
			Button.Dock = Docking.Fill;			

			Font = FontManager.Manager.DefaultFont;
			TextMargin = new Size (6, 2);

			ItemHeight = Font.TextBoxHeight;			
			MaxSize = new SizeF (Int32.MaxValue, ItemHeight);			
		}

		protected override void OnScaleWidget (IGUIContext ctx, float absoluteScaleFactor, float relativeScaleFactor)
		{
			base.OnScaleWidget (ctx, absoluteScaleFactor, relativeScaleFactor);
			//ItemHeight = Font.TextBoxHeight;			
		}        

		/***
		public override void OnMouseDown (OpenTK.Input.MouseButtonEventArgs e)
		{
			ToggleDropDown ();
			base.OnMouseDown (e);
		}
			
		public override void OnClick (OpenTK.Input.MouseButtonEventArgs e)
		{			
			base.OnClick (e);
		}
		**/

		protected string m_Text = null;
		public override string Text {
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

		public override SizeF PreferredSize (IGUIContext ctx, SizeF proposedSize)
		{
			//return new SizeF(proposedSize.Width, ItemHeight);
			return new SizeF (proposedSize.Width, Font.TextBoxHeight);
		}        

		public override void Update (IGUIContext ctx)
		{
			if (Bounds.Width <= 0 || Bounds.Height <= 0)
				return;			

			using (var clip = new ClipBoundClip (ctx, Bounds, true)) {
				if (!clip.IsEmptyClip) {
					Button.Update (ctx);

					try {
						RectangleF  bounds = Bounds;
						bounds.Width -= Bounds.Height;
						this.OnPaint (ctx, bounds);
					} catch (Exception ex) {
						ex.LogError ();
					} finally {				
						//ctx.ResetClip ();
					}
				}
			}

			if (DropDownWindow != null && DropDownWindow.Visible)
				DropDownWindow.Update (ctx);
		}

		public override void DrawItem(IGUIContext ctx, RectangleF bounds, 
			ComboBoxItem item, IWidgetStyle style)
		{
			if (item == null)
				return;

			bounds.Inflate (-TextMargin.Width, 0);
			ctx.DrawString (item.Text, Font, style.ForeColorBrush, bounds, FontFormat.DefaultSingleLine);
		}
			
		public override void OnPaint (IGUIContext ctx, RectangleF bounds)
		{			
			base.OnPaint (ctx, bounds);
			DrawItem (ctx, bounds, SelectedItem, Button.Style);
		}        

		protected override void CleanupManagedResources ()
		{
			m_Font = null;
			base.CleanupManagedResources ();
		}
	}
}

