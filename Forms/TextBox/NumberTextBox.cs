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
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Drawing;
using KS.Foundation;

namespace SummerGUI
{
	public class NumberTextBox : TextBoxContainer
	{		
		public event EventHandler<EventArgs> ValueChanged;
		public void OnValueChanged()
		{
			if (ValueChanged != null)
				ValueChanged (this, EventArgs.Empty);
		}

		public Button UpButton { get; private set; }
		public Button DownButton { get; private set; }

		public string StringFormat { get; set; }

		protected decimal m_Value;
		public decimal Value
		{
			get{
				return m_Value;
			}
			set {
				decimal oldVal = m_Value;
				m_Value = value;
				CheckMinMax ();
				if (m_Value != oldVal) {					
					UpdateText ();
					OnValueChanged ();
				}
			}
		}

		private void UpdateValue()
		{
			if (String.IsNullOrEmpty (TB.Text)) {
				m_Value = 0;
				return;
			}

			decimal val;
			if (Decimal.TryParse (TB.Text, out val))
				m_Value = val;
		}

		private void UpdateText()
		{
			TB.Text = m_Value.ToString (StringFormat);
			Invalidate ();
		}

		public NumberTextBox (string name)
			: base(name, Docking.Fill)
		{				
			TB.IsInputCharCallBack = IsInputChar;
			//TB.Format = FontFormat.DefaultSingleBaseLine;

			UpButton = new Button ("up", null, (char)FontAwesomeIcons.fa_angle_up);
			SetButtonStyles (UpButton);
			UpButton.IconOffsetY = -1;
			UpButton.Padding = new Padding (3.75f, 0, 2.25f, 0);
			UpButton.Fire += UpButton_Fire;
			UpButton.IsAutofire = true;
			UpButton.CanFocus = false;
			UpButton.CanSelect = false;
			AddChild (UpButton);

			DownButton = new Button ("down", null, (char)FontAwesomeIcons.fa_angle_down);
			SetButtonStyles (DownButton);
			DownButton.IconOffsetY = -1;
			DownButton.Padding = new Padding (3.75f, 0, 2.25f, 0);
			DownButton.Fire += DownButton_Fire;
			DownButton.IsAutofire = true;
			DownButton.CanFocus = false;
			DownButton.CanSelect = false;
			AddChild (DownButton);

			UpButton.MinSize = new SizeF (UpButton.MinSize.Width, (TB.MaxSize.Height - Border) / 2f);
			DownButton.MinSize = new SizeF (UpButton.MinSize.Width, (TB.MaxSize.Height - Border) / 2f);

			Increment = 1m;
			MinValue = 0;
			MaxValue = Decimal.MaxValue;
			UpdateText ();

				CanFocus = true;
		}

		public override void OnUpdateTheme (IGUIContext ctx)
		{			
			base.OnUpdateTheme (ctx);
			SetButtonStyles (UpButton);
			SetButtonStyles (DownButton);
		}

		private void SetButtonStyles(Button button)
		{
			button.Styles.ForEach (style => {				
				style.Border = 0;
				if ((style as GradientWidgetStyle) != null)
					(style as GradientWidgetStyle).ButtonStyle = ButtonStyles.Flat;
			});
		}			

		public decimal Increment { get; set; }
		public decimal MinValue { get; set; }
		public decimal MaxValue { get; set; }

		private void CheckMinMax()
		{
			m_Value = Math.Max (MinValue, Math.Min (MaxValue, m_Value));
		}

		public void DecrementValue()
		{
			UpdateValue ();
			Value -= Increment;
		}

		public void IncrementValue()
		{
			UpdateValue ();
			Value += Increment;
		}

		void UpButton_Fire (object sender, EventArgs e)
		{
			IncrementValue ();
		}

		void DownButton_Fire (object sender, EventArgs e)
		{
			DecrementValue ();
		}

		bool IsInputChar(char c) 
		{
			string decimalSeparator = Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator;
			if (!String.IsNullOrEmpty (decimalSeparator) && decimalSeparator [0] == c)
				return true;
			if (MinValue < 0 && c == '-')
				return true;
			return c.IsNumeric ();
		}

		public override void OnLayout (IGUIContext ctx, RectangleF bounds)
		{			
			if (IsLayoutSuspended)
				return;

			SetBounds (bounds);
			bounds = Bounds;

			float buttonWidth = UpButton.PreferredSize (ctx).Width;
			float halfHeight = bounds.Height / 2f;
			float buttonLeft = bounds.Right - buttonWidth;

			UpButton.SetBounds (new RectangleF (buttonLeft, bounds.Top, buttonWidth, halfHeight));
			DownButton.SetBounds (new RectangleF (buttonLeft, bounds.Top + halfHeight - 1, buttonWidth, bounds.Height - halfHeight));

			TB.OnLayout(ctx, new RectangleF(bounds.Left, bounds.Top, bounds.Width - buttonWidth, bounds.Height));
		}

		/***
		public override void Update (IGUIContext ctx, Rectangle bounds)
		{
			base.Update (ctx, bounds);

			if (Visible && Style != null) {				
				int x = UpButton.Left;
				ctx.DrawLine (Style.BorderColorPen, x, bounds.Top, x, bounds.Bottom);
			}
		}
		***/

		public override bool OnKeyDown (KeyboardKeyEventArgs e)
		{
			if (!TB.IsFocused)
				return false;

			switch (e.Key) {
			case Keys.Up:
				DecrementValue ();
				return true;
			case Keys.Down:
				IncrementValue ();
				return true;
			}

			if (base.OnKeyDown (e)) {
				UpdateValue ();
				return true;
			}

			return false;
		}



	}
}

