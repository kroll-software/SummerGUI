using System;
using System.Drawing;

namespace SummerGUI
{
	public class TextBoxWidgetStyle : WidgetStyle
	{
		public override void InitStyle ()
		{
			SetBackColor (Theme.Colors.White);
			SetForeColor (Theme.Colors.Base03);
			SetBorderColor (Theme.Colors.Base01);
		}
	}

	public class TextBoxActiveWidgetStyle : WidgetStyle
	{
		public override void InitStyle ()
		{			
			SetBackColor (Theme.Colors.White);
			SetForeColor (Theme.Colors.Base03);
			SetBorderColor (Theme.Colors.Base01);
		}
	}

	public class TextBoxDisabledWidgetStyle : WidgetStyle
	{
		public override void InitStyle ()
		{
			SetBackColor (Theme.Colors.White);
			SetForeColor (Theme.Colors.Base0);
			SetBorderColor (Theme.Colors.Base0);
		}
	}


	//********************** FORM BOX *************************


	public class TextBoxFormWidgetStyle : WidgetStyle
	{
		public override void InitStyle ()
		{
			SetBackColor (Theme.Colors.White);
			SetForeColor (Theme.Colors.Base03);
			SetBorderColor (Color.Empty);
		}
	}

	public class TextBoxFormDisabledWidgetStyle : WidgetStyle
	{
		public override void InitStyle ()
		{
			SetBackColor (Theme.Colors.White);
			SetForeColor (Theme.Colors.Base0);
			SetBorderColor (Color.Empty);
		}
	}


	// *************** TRANSPARENT STYLES **************












	public class TransparentTextBoxWidgetStyle : WidgetStyle
	{
		public override void InitStyle ()
		{
			SetBackColor (Color.Empty);
			SetForeColor (Theme.Colors.Base03);
			SetBorderColor (Color.Empty);
		}
	}

	public class TransparentTextBoxActiveWidgetStyle : WidgetStyle
	{
		public override void InitStyle ()
		{
			//SetBackColor (Color.FromArgb(64, NcsColors.Yellow));
			SetBackColor (Color.Empty);
			SetForeColor (Theme.Colors.Base03);
			SetBorderColor (Color.Empty);
		}
	}

	public class TransparentTextBoxDisabledWidgetStyle : WidgetStyle
	{
		public override void InitStyle ()
		{
			SetBackColor (Color.Empty);
			SetForeColor (Theme.Colors.Base0);
			SetBorderColor (Color.Empty);
		}
	}

}

