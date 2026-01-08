using System;

namespace SummerGUI
{
	public class CaptionLabel : TextWidget
	{
		public override string Text {
			get {
				if (String.IsNullOrEmpty (base.Text))
					return Name;
				return base.Text;
			}
			set {
				base.Text = value;
			}
		}

		public CaptionLabel (string name, string text = null)
			: base (name, Docking.Top, new DefaultTextWidgetStyle(), text, FontManager.Manager.DefaultFont)
		{
			Styles.SetStyle (new DisabledTextWidgetStyle (), WidgetStates.Disabled);
			Format = FontFormat.DefaultSingleLineCentered;
			//Format = FontFormat.DefaultMultiLineCentered;

			m_Margin = Padding.Empty;
			//m_Padding = new Padding (5, 0, 3, 0);
			m_Padding = new Padding (6);

			this.SetFontByTag(CommonFontTags.Status);
		}
	}
}
