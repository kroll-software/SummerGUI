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
	public class UrlTextWidgetStyle : WidgetStyle
	{
		public override void InitStyle ()
		{
			SetBackColor (Color.Empty);
			SetForeColor (Theme.Colors.Blue);
			SetBorderColor (Color.Empty);
		}
	}

	public class UrlTextWidgetPressedStyle : WidgetStyle
	{
		public override void InitStyle ()
		{
			SetBackColor (Color.Empty);
			SetForeColor (Theme.Colors.Magenta);
			SetBorderColor (Color.Empty);
		}
	}
		
	public class UrlTextWidget : TextWidget
	{				
		public UrlTextWidget (string name, string url)
			: base (name, Docking.Fill, new UrlTextWidgetStyle(), null, null)
		{
			Text = url;
			m_Url = url;
			Format = new FontFormat (Alignment.Near, Alignment.Center, 
				FontFormatFlags.Elipsis);

			Styles.SetStyle (new DisabledTextWidgetStyle (), WidgetStates.Disabled);
			Styles.SetStyle (new UrlTextWidgetPressedStyle (), WidgetStates.Pressed);
			CanFocus = true;
		}

		private string m_Url = null;
		public string Url 
		{ 
			get{
				return m_Url;
			}
			set {
				if (m_Url != value) {
					m_Url = value;
					OnUrlChanged ();
					if (string.IsNullOrEmpty (Text))
						Text = value;
				}
			}
		}

		protected virtual void OnUrlChanged()
		{
			// Nö..
			//ResetCachedSizes ();
		}

		public override string Text {
			get {
				return base.Text;
			}
			set {
				if (base.Text != value) {
					base.Text = value;
					if (String.IsNullOrEmpty (m_Url))
						Url = value;
				}
			}
		}

		public void OpenURL()
		{
			if (!String.IsNullOrEmpty(Url))
			{
				try
				{
					Process.Start(Url);
				}
				catch (Exception ex)
				{
					ex.LogError ();

					// ToDo()
					//ErrMsgBox(ex.Message);
				}
			}
		}

		public override void OnClick (OpenTK.Input.MouseButtonEventArgs e)
		{
			base.OnClick (e);
			if (IsVisibleEnabled) {
				OpenURL ();
			}
		}			

		public override bool OnKeyDown (OpenTK.Input.KeyboardKeyEventArgs e)
		{
			if (base.OnKeyDown (e))
				return true;

			if (!IsFocused)
				return false;

			if (e.Key == OpenTK.Input.Key.Enter) {
				OnClick (null);
				return true;
			}

			return false;
		}

		public override void OnMouseEnter (IGUIContext ctx)
		{			
			if (Enabled)
				Format = new FontFormat (Alignment.Near, Alignment.Center, 
					FontFormatFlags.Elipsis | FontFormatFlags.Underline);
			else
				Format = new FontFormat (Alignment.Near, Alignment.Center, 
					FontFormatFlags.Elipsis);
			base.OnMouseEnter (ctx);
		}

		public override void OnMouseLeave (IGUIContext ctx)
		{
			Format = new FontFormat (Alignment.Near, Alignment.Center, 
				FontFormatFlags.Elipsis);
			base.OnMouseLeave (ctx);
		}
	}
}

