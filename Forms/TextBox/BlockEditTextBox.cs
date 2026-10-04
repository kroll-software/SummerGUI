using System;
using System.Text;
using System.Drawing;
using OpenTK.Input;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using KS.Foundation;

namespace SummerGUI
{
	/// <summary>
	/// A block-wise numeric input comparable to the WinForms <c>DateTimePicker</c> / <c>TimePicker</c>.
	/// The input is split into fixed-width <see cref="Block">blocks</span> separated by a literal
	/// (e.g. <c>17.12.2026</c> → <c>17</c>, <c>12</c>, <c>2026</c>; <c>14:30</c> → <c>14</c>, <c>30</c>).
	/// Behaviour (matches WinForms):
	/// <list type="bullet">
	/// <item>On focus / click the <b>entire block</b> is highlighted; <b>no caret, no underscores</b>.</item>
	/// <item>Digit keys retype the block left-to-right; the first digit is right-aligned in the
	/// block, each further digit appends; a digit that would exceed the block's range is swallowed.</item>
	/// <item>Left/Right move the active block; Up/Down step the active block by 1 (wrapping 0..Max).</item>
	/// <item>Backspace/Delete are no-ops; invalid chars are swallowed.</item>
	/// <item>On commit (block switch / lost focus) the block is padded with leading zeros.</item>
	/// </list>
	/// The rendered selection uses the existing <see cref="TextBox"/> selection pipeline
	/// (<c>ctx.DrawSelectedString</c>), so the highlight looks exactly like a standard text selection.
	/// </summary>
	public class BlockEditTextBox : TextBox
	{
		/// <summary>One fixed-width numeric block. <see cref="Len"/> is the char count, <see cref="Max"/> the upper bound (inclusive; lower bound is 0).</summary>
		public class Block
		{
			public int Len = 2;
			public int Max = 99;
			/// <summary>The committed value (0..Max).</summary>
			public int Value;
			/// <summary>Digits typed during the current edit session, left to right.
			/// Length is the number of positions filled (<see cref="Len"/> on commit).</summary>
			public string Typed = string.Empty;
			/// <summary>True while this block is the one being edited (highlighted).</summary>
			public bool Editing;
		}

		Block[] m_Blocks;
		char m_Sep;
		int m_Active = 0;

		// active-block character span within the display text (for the selection highlight)
		int m_ActStart;
		int m_ActLen;

		/// <summary>Fires whenever a block's value changes (typing, stepping, committing).</summary>
		public event EventHandler<EventArgs> ValueChanged;
		void OnValueChanged ()
		{
			if (ValueChanged != null && !IsDisposed)
				ValueChanged (this, EventArgs.Empty);
		}

		public BlockEditTextBox (string name, Block[] blocks, char separator)
			: base (name)
		{
			m_Blocks = blocks ?? new Block[0];
			m_Sep = separator;
			if (m_Blocks.Length > 0)
				m_Active = 0;
			CanFocus = true;
			SelectBlock (0);
		}

		// ── display text (no caret, no prompt chars) ─────────────────────

		public override string DisplayText
		{
			get { return BuildDisplay (); }
		}

		string BuildDisplay ()
		{
			if (m_Blocks == null || m_Blocks.Length == 0)
				return string.Empty;
			var sb = new StringBuilder ();
			for (int i = 0; i < m_Blocks.Length; i++)
			{
				var b = m_Blocks[i];
				// While a block is being edited, the typed digits are shown right-aligned with a
				// leading-space pad (WinForms: first typed digit right-aligned).  A committed block
				// is always zero-padded left, so a value of 9 renders "09" — never "90".
				bool editing = b.Editing && b.Typed.Length > 0;
				string s = editing ? b.Typed : b.Value.ToString ();
				sb.Append (s.PadLeft (b.Len, editing ? ' ' : '0'));
				if (i < m_Blocks.Length - 1)
					sb.Append (m_Sep);
			}
			return sb.ToString ();
		}

		/// <summary>Rebuild the display and the active-block selection span, then repaint.</summary>
		void Refresh ()
		{
			if (m_Blocks == null || m_Blocks.Length == 0)
			{
				m_ActStart = 0;
				m_ActLen = 0;
				Invalidate ();
				return;
			}
			int start = 0;
			for (int i = 0; i < m_Blocks.Length; i++)
			{
				int len = m_Blocks[i].Len;
				if (i == m_Active)
				{
					m_ActStart = start;
					m_ActLen = len;
				}
				start += len;
				if (i < m_Blocks.Length - 1)
					start += 1;
			}
			// keep the base's selection accessors consistent with our block
			CaretIndex = m_ActStart + m_ActLen;
			ResetCachedLayout ();
			Invalidate ();
		}

		// ── rendering: background + selection highlight + text, NO CURET ────────────────

		public override void OnPaint (IGUIContext ctx, RectangleF bounds)
		{
			// Background is painted by Widget.Update() → OnPaintBackground (via the TextBoxWidgetStyle).
			// Here we draw the selection highlight + glyphs, but deliberately NOT the caret.
			// The blue block highlight is only shown while the widget has the focus.
			bounds.Inflate (-TextMargin.Width, -TextMargin.Height);

			string text = DisplayText;
			int selStart = IsFocused ? m_ActStart : 0;
			int selLen = IsFocused ? m_ActLen : 0;
			if (!string.IsNullOrEmpty (text))
			{
				ctx.DrawSelectedString (text, Font, selStart, selLen, bounds, TextOffsetX, Format,
					Style.ForeColorBrush.Color, Theme.Colors.HighLightBlue, Theme.Colors.White);
			}
			// no caret is drawn in block-edit mode
		}

		// ── selection / block navigation ────────────────────────────────

		public int ActiveIndex { get { return m_Active; } }
		public Block Active { get { return m_Blocks [m_Active]; } }

		void SelectBlock (int i)
		{
			if (m_Blocks == null || m_Blocks.Length == 0)
				return;
			CommitActive ();
			i = Math.Max (0, Math.Min (i, m_Blocks.Length - 1));
			m_Active = i;
			var b = m_Blocks[i];
			b.Editing = true;
			b.Typed = string.Empty;   // show current value until the user types
			Refresh ();
		}

		void CommitActive ()
		{
			if (m_Blocks == null || m_Active < 0)
				return;
			var b = m_Blocks[m_Active];
			if (b.Editing && b.Typed.Length > 0)
				b.Value = int.Parse (b.Typed);
			b.Editing = false;
			b.Typed = string.Empty;
		}

		/// <summary>Steps the active block by <paramref name="delta"/> (wraps within 0..Max).</summary>
		public void Step (int delta)
		{
			var b = Active;
			int base2 = (b.Editing && b.Typed.Length > 0) ? int.Parse (b.Typed) : b.Value;
			int nv = base2 + delta;
			if (nv < 0) nv = b.Max;
			if (nv > b.Max) nv = 0;
			b.Value = nv;
			b.Editing = true;
			b.Typed = string.Empty;   // display the stepped value, still highlighted
			Refresh ();
			OnValueChanged ();
		}

		// ── value in/out ─────────────────────────────────────────────────

		/// <summary>Sets the value from a digit string (filled block by block, left to right).
		/// The currently active block is KEPT — re-setting the value (e.g. the picker
		/// round-tripping after a step) must not move the highlight to block 0.</summary>
		public void SetValue (string digits)
		{
			if (m_Blocks == null || m_Blocks.Length == 0)
				return;
			digits = FilterTextAssignment (digits ?? string.Empty);   // Emojis would silently shift the block mapping.
			CommitAll ();
			int ri = 0;
			for (int i = 0; i < m_Blocks.Length; i++)
			{
				var b = m_Blocks[i];
				string s = string.Empty;
				for (int k = 0; k < b.Len && ri < digits.Length; k++)
					s += digits[ri++];
				int v;
				b.Value = int.TryParse (s, out v) ? v : b.Value;
			}
			// resume editing on the SAME active block (fresh start, highlighted)
			int ai = Math.Max (0, Math.Min (m_Active, m_Blocks.Length - 1));
			m_Active = ai;
			var ab = m_Blocks[ai];
			ab.Editing = true;
			ab.Typed = string.Empty;
			Refresh ();
		}

		void CommitAll ()
		{
			if (m_Blocks == null)
				return;
			foreach (var b in m_Blocks)
			{
				if (b.Editing && b.Typed.Length > 0)
					b.Value = int.Parse (b.Typed);
				b.Editing = false;
				b.Typed = string.Empty;
			}
		}

		/// <summary>The concatenated padded block values (e.g. "17122026" or "1430").</summary>
		public string Digits
		{
			get
			{
				if (m_Blocks == null)
					return string.Empty;
				var sb = new StringBuilder ();
				foreach (var b in m_Blocks)
				{
					string d = (b.Editing && b.Typed.Length > 0) ? b.Typed : b.Value.ToString ();
					sb.Append (d.PadLeft (b.Len, '0'));
				}
				return sb.ToString ();
			}
		}

		// ── input handling ───────────────────────────────────────────────

		protected override bool AcceptChar(char c)
		{
			if (IsEmojiChar(c))
				return false;
			return base.AcceptChar(c);
		}

		protected override string FilterTextAssignment(string value)
		{
			var sb = new System.Text.StringBuilder(value.Length);
			foreach (char c in value)
				if (!IsEmojiChar(c))
					sb.Append(c);
			return sb.ToString();
		}

		public override bool OnKeyPress (KeyPressEventArgs e)
		{
			if (!IsFocused || m_Blocks == null)
				return base.OnKeyPress (e);

			char c = e.KeyChar;
			if (char.IsDigit (c))
			{
				var b = m_Blocks[m_Active];
				if (!b.Editing)
				{
					b.Editing = true;
					b.Typed = string.Empty;
				}

				string typed = b.Typed;
				// Fill left-to-right, appending new digits (standard decimal entry).
				string cand = typed + c;
				if (cand.Length > b.Len)
					cand = cand.Substring (cand.Length - b.Len);

				int val;
				if (!int.TryParse (cand, out val) || val > b.Max || val < 0)
					return true;   // swallow: would make the block invalid

				b.Typed = cand;
				if (cand.Length == b.Len)
				{
					// block complete → commit and advance (WinForms retype flow)
					b.Value = val;
					if (m_Active < m_Blocks.Length - 1)
						SelectBlock (m_Active + 1);
					else
						Refresh ();
				}
				else
				{
					Refresh ();
				}
				OnValueChanged ();
				return true;
			}

			// swallow all non-digit input (letters, '.', ':' etc. are already shown as separators)
			return true;
		}

		public override bool OnKeyDown (KeyboardKeyEventArgs e)
		{
			if (ModifierKeys.AltPressed || !IsFocused || m_Blocks == null)
				return base.OnKeyDown (e);

			switch (e.Key)
			{
			case Keys.Left:
				SelectBlock (m_Active - 1);
				return true;
			case Keys.Right:
				SelectBlock (m_Active + 1);
				return true;
			case Keys.Up:
				Step (1);
				return true;
			case Keys.Down:
				Step (-1);
				return true;
			case Keys.Backspace:
			case Keys.Delete:
			case Keys.Tab:
			case Keys.Enter:
				return true;   // no-op / consume (no backspace-delete in block edit)
			case Keys.Home:
				SelectBlock (0);
				return true;
			case Keys.End:
				SelectBlock (m_Blocks.Length - 1);
				return true;
			}

			return base.OnKeyDown (e);
		}

		// ── focus / mouse ────────────────────────────────────────────────

		public override void Focus ()
		{
			base.Focus ();
			SelectBlock (0);
		}

		public override void OnGotFocus ()
		{
			base.OnGotFocus ();
			Refresh ();   // show the active-block highlight now that we have focus
		}

		public override void OnLostFocus ()
		{
			CommitAll ();
			Refresh ();   // drop the highlight
			OnValueChanged ();
			base.OnLostFocus ();
		}

		public override void OnMouseDown (MouseButtonEventArgs e)
		{
			base.OnMouseDown (e);
			// the block under the cursor becomes the active one (whole block highlighted)
			if (m_Blocks != null && m_Blocks.Length > 0 && Font != null)
			{
				float local = e.X - Bounds.Left;
				int idx = m_Blocks.Length - 1;
				int cidx = 0;
				float x = 0f;
				for (int i = 0; i < m_Blocks.Length; i++)
				{
					int len = m_Blocks[i].Len;
					float w = Font.MeasureGlyphs (DisplayText, cidx, len).Width;
					if (local < x + w)
					{
						idx = i;
						break;
					}
					x += w;
					cidx += len;
					if (i < m_Blocks.Length - 1)
						x += Font.MeasureGlyphs (m_Sep.ToString ()).Width;
				}
				if (idx != m_Active)
				{
					SelectBlock (idx);
					Invalidate ();
				}
			}
		}
	}
}
