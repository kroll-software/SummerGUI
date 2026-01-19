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
	public class ComboBox : ComboBoxBase
	{
		protected TextBox TB { get; private set; }

		[DpiScalable]
		public Size TextMargin { get; set; }

		public ComboBox (string name) 
			: base(name)
		{
			TB = new TextBox ("TextBox", null, true);
			TB.Dock = Docking.Fill;
			Children.Add (TB);

			InsertButton ();

			//TB.ReadOnly = true;
			TextMargin = new Size (6, 2);
			ItemHeight = TB.Font.TextBoxHeight;

			//Styles = new WidgetStyleProvider (new TextBoxWidgetStyle ());
			Styles.Clear();
			Styles.SetStyle (new TextBoxWidgetStyle (), WidgetStates.Default);
			Styles.SetStyle (new TextBoxActiveWidgetStyle (), WidgetStates.Active);
			Styles.SetStyle(new TextBoxDisabledWidgetStyle(), WidgetStates.Disabled);
		}			

		protected override void OnScaleWidget (IGUIContext ctx, float absoluteScaleFactor, float relativeScaleFactor)
		{
			base.OnScaleWidget (ctx, absoluteScaleFactor, relativeScaleFactor);
			ItemHeight = TB.Font.TextBoxHeight;
		}
			
		public override void OnLayout (IGUIContext ctx, RectangleF bounds)
		{
			if (IsLayoutSuspended)
				return;			
			base.OnLayout (ctx, bounds);
			if (TB.Font != null) {
				float height = TB.Font.TextBoxHeight;
				Button.SetBounds (new RectangleF (bounds.Right - height - 1, bounds.Top, height, height));
			}
		}

		public bool ReadOnly { 
			get {
				if (TB == null)
					return false;
				return TB.ReadOnly;
			}
			set {
				if (TB != null)
					TB.ReadOnly = value;
			}
		}

		public override SizeF PreferredSize (IGUIContext ctx, SizeF proposedSize)
		{
			return TB.PreferredSize(ctx, proposedSize);
		}

		public override string Text {
			get {
				return TB.Text;
			}
			set {
				if (TB.Text != value) {
					TB.Text = value;
					OnTextChanged ();
				}
			}
		}

		public override void Focus ()
		{
			TB.TabInto ();
		}

		public override void DrawItem(IGUIContext ctx, RectangleF bounds, 
			ComboBoxItem item, IWidgetStyle style)
		{
			if (item == null)
				return;
			// ToDo: DPI Scaling
			bounds.Inflate (-TextMargin.Width, 0);
			ctx.DrawString (item.Text, TB.Font, style.ForeColorBrush, bounds, FontFormat.DefaultSingleLine);
		}
	}
}

