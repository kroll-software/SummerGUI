using System;
using System.Linq;
using System.Drawing;
using OpenTK.Input;
using KS.Foundation;

namespace SummerGUI
{
	public class ComboBoxButton : DefaultButton
	{
		public ComboBoxButton ()	
			: base ("DropDownButton", null, 
				(char)FontAwesomeIcons.fa_angle_down)
		{
			MaxSize = SizeMax;
			MinSize = SizeF.Empty;
			Dock = Docking.Right;
			//Padding = new Padding(9, 0, 9, 0);
			Padding = Padding.Empty;
			Margin = Padding.Empty;
			CanFocus = true;
		}
			
		public override void OnMouseDown (MouseButtonEventArgs e)
		{
			base.OnMouseDown (e);
			(Parent as ComboBoxBase).Do (p => p.ToggleDropDown ());
		}

		public override void OnClick (MouseButtonEventArgs e)
		{
			base.OnClick (e);
			if (e == null)
				(Parent as ComboBoxBase).Do (p => p.ToggleDropDown ());
		}

		protected override void OnParentChanged ()
		{
			ComboBoxBase cbo = Parent as ComboBoxBase;
			if (Parent != null && cbo == null)
				this.LogError ("Class ComboBoxButton should only be used for ComboBoxes.");

			base.OnParentChanged ();
		}
			
		public override void OnPaint (IGUIContext ctx, RectangleF bounds)
		{
			//base.OnPaint (ctx, bounds);

			if (Icon != 0 && IconFont != null) {				
				float h = bounds.Height;
				float w = bounds.Right - h;
				if (Dock == Docking.Fill && Text == null) {						
					ctx.DrawLine (Style.BorderColorPen, w, bounds.Top, w, bounds.Bottom - 0.5f);
				}

				RectangleF rt = new RectangleF (w, bounds.Top + TextOffsetY, h, h);

				if (IconColor != Color.Empty && Enabled) {
					using (Brush brush = new SolidBrush (IconColor)) {
						ctx.DrawString (Icon.ToString (), IconFont, brush, rt, FontFormat.DefaultIconFontFormatCenter);
					}
				} else {
					ctx.DrawString(Icon.ToString (), IconFont, Style.ForeColorBrush, rt, FontFormat.DefaultIconFontFormatCenter);
				}					
			}
		}
	}
}

