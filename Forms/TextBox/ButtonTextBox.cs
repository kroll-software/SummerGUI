using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Input;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System.Drawing;
using KS.Foundation;

namespace SummerGUI
{
	public class ButtonTextBox : TextBoxContainer
	{
		public Button Button { get; private set; }
		public bool AutoEnableButton { get; set; }

		int m_FixedButtonWidth;
		[DpiScalable]
		public int FixedButtonWidth 
		{ 
			get {
				return m_FixedButtonWidth;
			}
			set {
				if (m_FixedButtonWidth != value) {
					m_FixedButtonWidth = value;
					OnFixedButtonWidthChanged ();
				}
			}
		}

		protected virtual void OnFixedButtonWidthChanged()
		{
			ResetCachedLayout ();
		}

		public ButtonTextBox (string name, char icon = (char)0, string buttonText = null, ColorContexts colorContext = ColorContexts.Default)
			: base (name)
		{
			Button = new Button ("button", buttonText, colorContext);
			Button.IsMenu = false;
			Button.Icon = icon;
			Button.Padding = new Padding (8, 0, 8, 0);
			Button.MaxSize = TB.MaxSize;
			Button.MinSize = SizeF.Empty;

			AddChild (Button);

			TB.TextChanged += TB_TextChanged;
			AutoEnableButton = true;
			Button.Enabled = false;

			Button.CanFocus = true;
			CanFocus = true;
		}

		void TB_TextChanged (object sender, EventArgs e)
		{
			if (AutoEnableButton)
				Button.Enabled = !String.IsNullOrEmpty (Text);
		}						

		public override void Focus ()
		{
			TB.Focus ();
		}

		public void TabInto()
		{
			TB.TabInto ();
		}

		public override void OnLayout (IGUIContext ctx, RectangleF bounds)
		{			
			if (IsLayoutSuspended)
				return;
			SetBounds (bounds);

			if (TB.Font != null) {				
				float height = TB.Font.TextBoxHeight;
				Button.SetBounds (new RectangleF (bounds.Right - height - 1, bounds.Top.Ceil (), height + 1, (int)height));
				TB.OnLayout (ctx, new RectangleF(bounds.Left, bounds.Top, bounds.Width - height, height));
			}				
		}			
	}
}

