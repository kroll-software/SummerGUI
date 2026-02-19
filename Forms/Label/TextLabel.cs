using System;

namespace SummerGUI
{
	public class TextLabel : TextWidget
	{
		public override string Text {
			get {
				if (String.IsNullOrEmpty (base.Text))
					return Name;
				return base.Text;
			}
			set {
				base.Text = value.ToUpper();
			}
		}

		public float OffsetY { get; set; }

		public TextLabel (string name, string text = null)
			: base (name, Docking.Fill, new DefaultFormLabelWidgetStyle(), text, FontManager.Manager.DefaultFont)
		{
			Styles.SetStyle (new DisabledFormLabelWidgetStyle (), WidgetStates.Disabled);
			Format = FontFormat.DefaultSingleLine;			
			this.SetFontByTag(CommonFontTags.Small);
			Text = Text.ToUpper ();
			Margin = Padding.Empty;

			//Padding = Padding.Empty;
			Padding = new Padding (4, 0, 3, 0);
		}

		protected override void OnParentChanged ()
		{
			base.OnParentChanged ();

			TableLayoutContainer tlc = Parent as TableLayoutContainer;
			if (tlc != null)
				OffsetY = tlc.CellPadding.Height / 2;
		}

		public override void OnLayout (IGUIContext ctx, System.Drawing.RectangleF bounds)
		{
			bounds.Offset (0, OffsetY);
			base.OnLayout (ctx, bounds);
		}		

		public override void OnPaint (IGUIContext ctx, System.Drawing.RectangleF bounds)
		{
			base.OnPaint (ctx, bounds);
		}
	}
}

