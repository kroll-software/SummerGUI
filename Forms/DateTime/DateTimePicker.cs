using System;
using System.Drawing;
using KS.Foundation;

namespace SummerGUI
{
	/// <summary>
	/// A date input comparable to the WinForms <c>DateTimePicker</c>:
	/// a masked text box (<c>dd.MM.yyyy</c>) on the left and a calendar
	/// icon button on the right. Clicking the button opens a
	/// <see cref="CalendarPopup"/> (a MonthCalendar) as an overlay —
	/// exactly like the ComboBox drop-down pattern already used elsewhere.
	/// The value can also be typed directly into the box.
	/// </summary>
	public class DateTimePicker : Container
	{
		public event EventHandler<EventArgs> ValueChanged;
		public void OnValueChanged ()
		{
			if (ValueChanged != null && !IsDisposed)
				ValueChanged (this, EventArgs.Empty);
		}

		protected BlockEditTextBox DateBox { get; private set; }
		public Button CalendarButton { get; private set; }
		protected CalendarPopup Popup { get; private set; }

		// ── eingebaute, optionale Checkbox („aktiv"-Anzeige, wie WinForms-DTP ShowCheckBox) ──
		public CheckBox EmbeddedCheckBox { get; private set; }

		public event EventHandler<EventArgs> CheckedChanged;
		public void OnCheckedChanged ()
		{
			ApplyCheckGate ();
			if (CheckedChanged != null && !IsDisposed)
				CheckedChanged (this, EventArgs.Empty);
		}

		/// <summary>Nach WinForms: ob eine Checkbox links im Feld eingebunden ist. Standard: false.
		/// Nur per Code schaltbar — die Checkbox bleibt dann permanent sichtbar.</summary>
		public bool ShowCheckBox
		{
			get { return EmbeddedCheckBox != null && EmbeddedCheckBox.Visible; }
			set
			{
				if (EmbeddedCheckBox == null || EmbeddedCheckBox.Visible == value)
					return;
				EmbeddedCheckBox.Visible = value;
				ApplyCheckGate ();
				ResetCachedLayout ();
				Invalidate ();
			}
		}

		// ── Gate: DateBox + Kalender sind aktiv NUR, wenn die eingebettete Checkbox (ein-)gewählt ist.
		//    „Checkbox ungewählt + Box/Kalender aktiv“ tritt damit nie auf. Die Checkbox-Glyphe
		//    selbst bleibt (bei Sichtbarkeit) enabled, damit der User sie zum Umschalten nutzen kann.
		private bool ActiveGate
		{
			get { return (EmbeddedCheckBox == null) || !EmbeddedCheckBox.Visible || EmbeddedCheckBox.Checked; }
		}
		private void ApplyCheckGate ()
		{
			bool g = ActiveGate;
			if (DateBox != null)
				DateBox.Enabled = g;
			if (CalendarButton != null)
				CalendarButton.Enabled = g;
		}

		/// <summary>Status der eingebauten Checkbox („aktiv/inaktiv").</summary>
		public bool Checked
		{
			get { return EmbeddedCheckBox != null && EmbeddedCheckBox.Checked; }
			set
			{
				if (EmbeddedCheckBox == null || EmbeddedCheckBox.Checked == value)
					return;
				EmbeddedCheckBox.Checked = value;
			}
		}

		protected DateTime m_Value = DateTime.Today;
		/// <summary>The selected date. Default: <see cref="DateTime.Today"/>.</summary>
		public DateTime Value
		{
			get { return m_Value; }
			set
			{
				value = value.Date;
				if (m_Value == value)
					return;
				m_Value = value;
				UpdateText ();
				OnValueChanged ();
			}
		}

		private const string Mask = "00.00.0000";
		private const char CalendarIcon = (char)0xf073; // FontAwesomeIcons.fa_calendar

		public DateTimePicker (string name)
			: base (name, Docking.Fill, new TextBoxWidgetStyle())
		{
			Styles.SetStyle (new TextBoxActiveWidgetStyle (), WidgetStates.Active);
			Styles.SetStyle (new TextBoxDisabledWidgetStyle (), WidgetStates.Disabled);

			DateBox = new BlockEditTextBox (
				"datebox",
				new BlockEditTextBox.Block [] {
					new BlockEditTextBox.Block { Len = 2, Max = 31, Value = m_Value.Day },
					new BlockEditTextBox.Block { Len = 2, Max = 12, Value = m_Value.Month },
					new BlockEditTextBox.Block { Len = 4, Max = 9999, Value = m_Value.Year },
				},
				'.' ) { Dock = Docking.Fill };
			DateBox.ValueChanged += DateBox_TextChanged;
			AddChild (DateBox);

			CalendarButton = new Button ("calendar", null, CalendarIcon);
			CalendarButton.Padding = new Padding (3, 0, 3, 0);
			CalendarButton.IsAutofire = true;
			CalendarButton.CanFocus = true;
			CalendarButton.CanSelect = false;
			CalendarButton.Fire += CalendarButton_Fire;
			AddChild (CalendarButton);

			EmbeddedCheckBox = new CheckBox ("embeddedcheckbox", "");
			EmbeddedCheckBox.CanFocus = false;   // keine Fokus-Übernahme, nur visuelle/klickbare Anzeige
			EmbeddedCheckBox.Visible = false;
			EmbeddedCheckBox.CheckedChanged += delegate { OnCheckedChanged (); };
			// Klick in die Datums-TextBox -> automatisch aktivieren (Checkbox anhaken)
			DateBox.Click += delegate { if (EmbeddedCheckBox != null && EmbeddedCheckBox.Visible) Checked = true; };
			AddChild (EmbeddedCheckBox);

			ApplyCheckGate ();
			CanFocus = true;
			UpdateText ();
		}

		// ── value ⇄ text ─────────────────────────────────────────────────

		private void UpdateText ()
		{
			if (DateBox == null)
				return;
			// digits only — the '.' separators are rendered by the BlockEditTextBox itself.
			DateBox.SetValue (m_Value.ToString ("ddMMyyyy"));
			Invalidate ();
		}

		private void DateBox_TextChanged (object sender, EventArgs e)
		{
			UpdateValueFromText ();
		}

		private void UpdateValueFromText ()
		{
			// Digits = padded block values, e.g. "17122026".
			string digits = DateBox.Digits;
			DateTime val;
			if (digits.Length == 8 && DateTime.TryParseExact (digits, "ddMMyyyy", System.Globalization.CultureInfo.InvariantCulture,
					System.Globalization.DateTimeStyles.None, out val))
			{
				DateTime old = m_Value;
				m_Value = val.Date;
				if (m_Value != old)
					OnValueChanged ();
			}
		}

		private static bool IsInputChar (char c)
		{
			return c.IsNumeric () || c == '.';
		}

		// ── popup ────────────────────────────────────────────────────────

		public bool IsPopupOpen
		{
			get { return Popup != null && !Popup.IsDisposed; }
		}

		public void TogglePopup ()
		{
			if (IsPopupOpen)
				ClosePopup ();
			else
				OpenPopup ();
		}

		protected virtual void OpenPopup ()
		{
			if (IsPopupOpen)
				return;

			Popup = CreatePopup ();
			AddChild (Popup);
			// AddChild auto-registers it as an overlay at the root (ZIndex >= 10000),
			// so clicking elsewhere on the root closes it via IOverlayWidget.Closing.
			Popup.Closing += delegate
			{
				ClosePopup ();
				if (Enabled)
					DateBox.Focus ();
			};

			MonthCalendar cal = Popup.Calendar;
			cal.CurrentDate = m_Value;
			cal.SelectionChanged += delegate
			{
				if (cal.CurrentDate != DateTime.MinValue)
					Value = cal.CurrentDate;
				ClosePopup ();
				if (Enabled)
					DateBox.Focus ();
			};

			Popup.Focus ();
		}

		protected virtual void ClosePopup ()
		{
			if (!IsPopupOpen)
				return;

			Popup.Visible = false;
			RemoveChild (Popup);
			Popup.Dispose ();
			Popup = null;
			Invalidate ();
		}

		/// <summary>Creates the popup with a fresh calendar — overridable to customise fonts/colors.</summary>
		protected virtual CalendarPopup CreatePopup ()
		{
			MonthCalendar calendar = new MonthCalendar ("popupcalendar",
				FontManager.Manager.FontByTag (CommonFontTags.Default),
				FontManager.Manager.FontByTag (CommonFontTags.Bold));
			return new CalendarPopup (calendar);
		}

		protected virtual void CalendarButton_Fire (object sender, EventArgs e)
		{
			TogglePopup ();
		}

		// ── enabled / focus ──────────────────────────────────────────────

		public override bool Enabled
		{
			get { return base.Enabled; }
			set
			{
				base.Enabled = value;
				// Die Checkbox-Glyphe folgt nur dem Picker-Level (bleibt bei Sichtbarkeit klickbar —
				// das ist ihre Aufgabe: Box/Kalender umschalten). DateBox + Kalender folgen dem Gate.
				if (EmbeddedCheckBox != null)
					EmbeddedCheckBox.Enabled = value;
				ApplyCheckGate ();
			}
		}

		public override void Focus ()
		{			
			DateBox.Focus ();
		}

		// ── sizing: let the text box decide the natural row height ───────

		public override SizeF PreferredSize (IGUIContext ctx, SizeF proposedSize)
		{
			if (DateBox == null)
				return base.PreferredSize (ctx, proposedSize);
			SizeF sz = DateBox.PreferredSize (ctx, proposedSize);
			if (ShowCheckBox && EmbeddedCheckBox != null) {
				float cbw = Math.Max (EmbeddedCheckBox.PreferredSize (ctx).Width, sz.Height);
				sz.Width += cbw;
			}
			return sz;
		}

		// ── layout ───────────────────────────────────────────────────────

		public override void OnLayout (IGUIContext ctx, RectangleF bounds)
		{
			if (IsLayoutSuspended)
				return;

			SetBounds (bounds);

			float height = bounds.Height;
			float buttonwidth = Math.Max (height, DateBox.PreferredSize (ctx).Height);

			// keep the button roughly square
			buttonwidth = Math.Max (buttonwidth, height);

			// optionale Checkbox-Zone links
			float leftEdge = bounds.Left;
			if (ShowCheckBox && EmbeddedCheckBox != null) {
				float cbw = Math.Max (EmbeddedCheckBox.PreferredSize (ctx).Width, height);
				EmbeddedCheckBox.SetBounds (new RectangleF (bounds.Left, bounds.Top, cbw, height));
				leftEdge += cbw;
			}

			float boxWidth = bounds.Right - buttonwidth - leftEdge;
			if (boxWidth < 0f)
				boxWidth = 0f;

			CalendarButton.SetBounds (new RectangleF (bounds.Right - buttonwidth, bounds.Top, buttonwidth, height));
			DateBox.OnLayout (ctx, new RectangleF (leftEdge, bounds.Top, boxWidth, height));

			// popup: anchor near the calendar button, clamped into the visible window.
			// CalendarPopup.SetBounds clamps the width to a compact month-grid size.
			if (IsPopupOpen && ParentWindow != null)
			{
				float pw = 300f;
				float ph = pw * 1.2f;
				float parentW = ParentWindow.Width;
				float parentH = ParentWindow.Height;

				// anchor under the calendar button (picker right edge), not the table cell
				// left edge; clamp so the full popup stays inside the window.
				float pl = bounds.Right - pw;
				if (pl < 4f)
					pl = 4f;
				if (pl + pw > parentW - 4f)
					pl = Math.Max (4f, parentW - pw - 4f);

				float pt = bounds.Bottom + 2f;
				if (pt + ph > parentH - 4f && bounds.Top >= ph + 4f)
					pt = bounds.Top - ph - 2f; // no room below → open above

				RectangleF pBounds = new RectangleF (pl, pt, pw, ph);
				Popup.SetBounds (pBounds);
				Popup.OnLayout (ctx, pBounds);
			}
		}
	}
}
