using System;
using System.Drawing;
using OpenTK.Input;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using KS.Foundation;

namespace SummerGUI
{
	/// <summary>
	/// A time input comparable to the WinForms <c>TimePicker</c>: a masked text box
	/// (<c>hh:mm</c>) plus one spin button pair (up/down) on the right — exactly like
	/// the spinner in <see cref="NumberTextBox"/>. Up/down nudges the minute by 1
	/// (wrapping the hour when 59 → 00). No overlay.
	/// </summary>
	public class TimePicker : Container
	{
		public event EventHandler<EventArgs> ValueChanged;
		public void OnValueChanged ()
		{
			if (ValueChanged != null && !IsDisposed)
				ValueChanged (this, EventArgs.Empty);
		}

		protected BlockEditTextBox TimeBox { get; private set; }

		public Button UpButton { get; private set; }
		public Button DownButton { get; private set; }

		/// <summary>Minute step for the spin buttons / arrow keys (default 1).</summary>
		public int SpinStep { get; set; } = 1;

		protected DateTime m_Value = DateTime.Today;
		/// <summary>The current time. Default: the current time of day.</summary>
		public DateTime Value
		{
			get { return m_Value; }
			set
			{
				if (m_Value == value)
					return;
				m_Value = value;
				UpdateText ();
				OnValueChanged ();
			}
		}

		private const string Mask = "00:00";

		private void SetButtonStyles (Button button)
		{
			button.Styles.ForEach (style =>
			{
				style.Border = 0;
				if ((style as GradientWidgetStyle) != null)
					(style as GradientWidgetStyle).ButtonStyle = ButtonStyles.Flat;
			});
		}

		public TimePicker (string name)
			: base (name, Docking.Fill, new TextBoxWidgetStyle())
		{
			Styles.SetStyle (new TextBoxActiveWidgetStyle (), WidgetStates.Active);
			Styles.SetStyle (new TextBoxDisabledWidgetStyle (), WidgetStates.Disabled);

			TimeBox = new BlockEditTextBox (
				"timebox",
				new BlockEditTextBox.Block [] {
					new BlockEditTextBox.Block { Len = 2, Max = 23, Value = m_Value.Hour },
					new BlockEditTextBox.Block { Len = 2, Max = 59, Value = m_Value.Minute },
				},
				':' ) { Dock = Docking.Fill };
			TimeBox.ValueChanged += TimeBox_TextChanged;
			AddChild (TimeBox);

			UpButton = new Button ("timeup", null, (char)FontAwesomeIcons.fa_angle_up);
			SetButtonStyles (UpButton);
			UpButton.IconOffsetY = -1;
			UpButton.Padding = new Padding (3.75f, 0, 2.25f, 0);
			UpButton.IsAutofire = true;
			UpButton.CanFocus = false;
			UpButton.CanSelect = false;
			UpButton.Fire += delegate { TimeBox.Step (SpinStep); };
			AddChild (UpButton);

			DownButton = new Button ("timedown", null, (char)FontAwesomeIcons.fa_angle_down);
			SetButtonStyles (DownButton);
			DownButton.IconOffsetY = -1;
			DownButton.Padding = new Padding (3.75f, 0, 2.25f, 0);
			DownButton.IsAutofire = true;
			DownButton.CanFocus = false;
			DownButton.CanSelect = false;
			DownButton.Fire += delegate { TimeBox.Step (-SpinStep); };
			AddChild (DownButton);

			CanFocus = true;
			UpdateText ();
		}

		public override void OnUpdateTheme (IGUIContext ctx)
		{
			base.OnUpdateTheme (ctx);
			SetButtonStyles (UpButton);
			SetButtonStyles (DownButton);
		}

		/// <summary>Increments (positive) or decrements (negative) the time by N minutes (wraps within the day).</summary>
		public void Spin (int deltaMinutes)
		{
			if (!Enabled)
				return;

			Value = m_Value.AddMinutes (deltaMinutes);
		}

		// ── value ⇄ text ─────────────────────────────────────────────────

		private void UpdateText ()
		{
			if (TimeBox == null)
				return;
			// digits only — the mask's literal ':' separator is rendered by the
			// MaskedTextBox itself (SetValue walks placeholder positions in order).
			TimeBox.SetValue (m_Value.ToString ("HHmm"));
			Invalidate ();
		}

		private void TimeBox_TextChanged (object sender, EventArgs e)
		{
			int hour, minute;
			if (!TryParseTime (TimeBox.Digits, out hour, out minute))
				return;

			if (hour < 0 || hour > 23 || minute < 0 || minute > 59)
				return;

			DateTime old = m_Value;
			DateTime nv = new DateTime (old.Year, old.Month, old.Day, hour, minute, 0);

			if (nv != old)
			{
				// Set the value directly — do NOT round-trip through UpdateText()/SetValue():
				// that would re-push the digits into the box and, before the fix, moved the
				// active-block highlight back to the hour (block 0).  Same pattern as DateTimePicker.
				m_Value = nv;
				OnValueChanged ();
			}
		}

		private static bool TryParseTime (string text, out int hour, out int minute)
		{
			hour = 0;
			minute = 0;
			if (string.IsNullOrEmpty (text))
				return false;

			string digits = string.Empty;
			for (int i = 0; i < text.Length; i++)
				if (char.IsDigit (text [i]))
					digits += text [i];

			if (digits.Length < 2)
				return false;

			int ok;
			if (!int.TryParse (digits.Substring (0, 2), out ok))
				return false;
			hour = ok;
			if (digits.Length >= 4)
			{
				if (!int.TryParse (digits.Substring (2, 2), out ok))
					return false;
				minute = ok;
			}
			return true;
		}

		private static bool IsInputChar (char c)
		{
			return c.IsNumeric () || c == ':';
		}

		// ── enabled / focus ──────────────────────────────────────────────

		public override bool Enabled
		{
			get { return base.Enabled; }
			set
			{
				base.Enabled = value;
				if (TimeBox != null)
					TimeBox.Enabled = value;
				if (UpButton != null)
					UpButton.Enabled = value;
				if (DownButton != null)
					DownButton.Enabled = value;
			}
		}

		public override void Focus ()
		{
			TimeBox.Focus ();
		}

		// ── sizing: let the text box decide the natural row height ───────

		public override SizeF PreferredSize (IGUIContext ctx, SizeF proposedSize)
		{
			if (TimeBox == null)
				return base.PreferredSize (ctx, proposedSize);
			return TimeBox.PreferredSize (ctx, proposedSize);
		}

		// ── layout: [ textbox ][ up / down ] ──────────────────────────────

		public override void OnLayout (IGUIContext ctx, RectangleF bounds)
		{
			if (IsLayoutSuspended)
				return;

			SetBounds (bounds);
			bounds = Bounds;

			// Exactly like NumberTextBox: button column as wide as the button's natural
			// width, split top/bottom at mid-height with NO white gap between them,
			// the text box takes the remaining width.
			float buttonWidth = UpButton.PreferredSize (ctx).Width;
			float halfHeight = bounds.Height / 2f;
			float buttonLeft = bounds.Right - buttonWidth;

			UpButton.SetBounds (new RectangleF (buttonLeft, bounds.Top, buttonWidth, halfHeight));
			DownButton.SetBounds (new RectangleF (buttonLeft, bounds.Top + halfHeight - 1, buttonWidth, halfHeight));

			TimeBox.OnLayout (ctx, new RectangleF (bounds.Left, bounds.Top, bounds.Width - buttonWidth, bounds.Height));
		}

		public override bool OnKeyDown (KeyboardKeyEventArgs e)
		{
			// Up/Down are handled by BlockEditTextBox itself (steps the active block).
			return base.OnKeyDown (e);
		}
	}
}
