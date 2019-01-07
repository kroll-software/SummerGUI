using System;
using System.Drawing;

namespace SummerGUI
{
	public class AutoScrollButtonStyle : WidgetStyle
	{
		public override void InitStyle ()
		{
			base.InitStyle ();
			SetForeColor (Theme.Colors.Base2);
			//SetForeColor(Theme.Colors.Cyan);
			SetBackColor (Color.FromArgb(128, Theme.Colors.Base01));
			SetBorderColor (Color.Empty);
		}
	}

	public class AutoScrollButtonHoverStyle : WidgetStyle
	{
		public override void InitStyle ()
		{
			base.InitStyle ();
			SetForeColor (Theme.Colors.Base3);
			SetBackColor (Color.FromArgb(200, Theme.Colors.Base00));
			SetBorderColor (Color.Empty);
		}
	}

	public class AutoScrollButtonPressedStyle : WidgetStyle
	{
		public override void InitStyle ()
		{
			base.InitStyle ();
			SetBackColor (Color.FromArgb(180, Theme.Colors.Blue));
			SetForeColor (Theme.Colors.Base2);
			SetBorderColor (Color.Empty);
		}
	}

	public class AutoScrollButtonDisabledStyle : WidgetStyle
	{
		public override void InitStyle ()
		{
			base.InitStyle ();
			SetBackColor (Color.FromArgb(180, Theme.Colors.Base0));
			SetForeColor (Theme.Colors.Base00);
			SetBorderColor (Color.Empty);
		}
	}

	public class AutoScrollButton : Button
	{
		public bool ScrollOnMouseHover { get; set; }

		public override bool CanFocus {
			get {
				return false;
			}
			set {
				base.CanFocus = value;
			}
		}

		public AutoScrollButton (string name, Docking dock, char icon)
			: base (name, String.Empty, icon, new AutoScrollButtonStyle())
		{				
			Dock = dock;

			this.Styles.SetStyle (new AutoScrollButtonHoverStyle (), WidgetStates.Hover);
			this.Styles.SetStyle (new AutoScrollButtonPressedStyle (), WidgetStates.Pressed);
			this.Styles.SetStyle (new AutoScrollButtonDisabledStyle (), WidgetStates.Disabled);


			this.ZIndex = 1000;
			Margin = Padding.Empty;
			Padding = new Padding (12, 8, 12, 7);
			HAlign = Alignment.Center;
			IsAutofire = true;

			//Visible = false;
		}
	}

	public class VerticalTopAutoScrollButton : AutoScrollButton
	{
		public VerticalTopAutoScrollButton()
			: base("VerticalTopAutoScrollButton", Docking.Top, (char)FontAwesomeIcons.fa_chevron_up)
		{

		}
	}

	public class VerticalBottomAutoScrollButton : AutoScrollButton
	{
		public VerticalBottomAutoScrollButton()
			: base("VerticalBottomAutoScrollButton", Docking.Bottom, (char)FontAwesomeIcons.fa_chevron_down)
		{			
		}
	}
}

