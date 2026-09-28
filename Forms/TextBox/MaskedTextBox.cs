using System;
using System.Text;
using System.Diagnostics;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Input;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using KS.Foundation;

namespace SummerGUI
{
	/// <summary>
	/// A text box that restricts input based on a <see cref="Mask"/> string — comparable to the
	/// WinForms <c>MaskedTextBox</c>, but implemented on top of SummerGUI's <see cref="TextBox"/> so all
	/// basic functionality (Caret, Selection, Clipboard, Cursor, Scrolling, …) is inherited unchanged.
	/// </summary>
	/// <remarks>
	/// <b>Mask format</b> — one character per position in the input field:
	/// <list type="bullet">
	/// <item><description><c>0</c> — digit <b>required</b></description></item>
	/// <item><description><c>9</c> — digit <b>optional</b></description></item>
	/// <item><description><c>#</c> — digit or sign (<c>+/-</c>) <b>optional</b></description></item>
	/// <item><description><c>L</c> — letter <b>required</b></description></item>
	/// <item><description><c>?</c> — letter <b>optional</b></description></item>
	/// <item><description><c>A</c> — alphanumerical, converted to <b>upper</b>case</description></item>
	/// <item><description><c>a</c> — alphanumerical, converted to <b>lower</b>case</description></item>
	/// <item><description><c>*</c> — any printable character</description></item>
	/// <item><description>any other character — <b>literal</b> (pre-filled, not deletable)</description></item>
	/// </list>
	/// Unfilled placeholder positions show <see cref="PromptChar"/>.
	/// <para>
	/// Reading values: <see cref="Text"/> holds the full display (literals + values + prompt chars).
	/// The raw user input (placeholders only, in mask order) is available via <see cref="MaskedInput"/>.
	/// </para>
	/// </remarks>
	public class MaskedTextBox : TextBox
	{
		// ── mask state ──────────────────────────────────────────────────────────
		// m_MaskState[i] holds the currently displayed character for mask-position i:
		//   literal   → the literal
		//   filled    → the user-typed character
		//   empty     → PromptChar
		char[] m_MaskState;

		/// <summary>
		/// The mask template (<see cref="m_MaskStates"/> one char per position).
		/// If empty, the box behaves exactly like a plain <see cref="TextBox"/>.
		/// </summary>
		public string Mask
		{
			get { return m_Mask; }
			set
			{
				if (value == m_Mask) return;
				m_Mask = value ?? string.Empty;
				ApplyMask(null);
				ResetCachedLayout();
				Invalidate();
			}
		}
		private string m_Mask = "";

		/// <summary>
		/// The character shown in positions that are not yet filled (default: <c>'</c> — same prompt character WinForms uses).
		/// </summary>
		public char PromptChar
		{
			get { return m_PromptChar; }
			set
			{
				if (m_PromptChar == value) return;
				m_PromptChar = value;
				if (m_MaskState != null)
				{
					for (int i = 0; i < m_MaskState.Length; i++)
						if (IsPlaceholder(m_Mask[i]) && m_MaskState[i] != m_PromptChar && IsEmpty(i))
							m_MaskState[i] = value;
					SetTextFromMaskState();
					Invalidate();
				}
			}
		}
		private char m_PromptChar = '_';

		/// <summary><c>true</c> when all <b>required</b> mask positions are filled (optional positions are ignored).</summary>
		public bool MaskCompleted
		{
			get
			{
				if (m_MaskState == null) return true;
				for (int i = 0; i < m_MaskState.Length; i++)
					if (IsRequired(m_Mask[i]) && IsEmpty(i))
						return false;
				return true;
			}
		}

		// ── constructors ─────────────────────────────────────────────────────────

		/// <summary>Creates a <see cref="MaskedTextBox"/> without a mask (behaves like a <see cref="TextBox"/>).</summary>
		public MaskedTextBox(string name)
				: base(name)
		{
		}

		/// <summary>Creates a <see cref="MaskedTextBox"/> with the given <paramref name="mask"/>, pre-filled with <paramref name="text"/> (raw value).</summary>
		public MaskedTextBox(string name, string mask, string text = null)
				: base(name)
		{
			m_Mask = mask ?? string.Empty;
			ApplyMask(text);
		}

		// ── value access ─────────────────────────────────────────────────────────

		/// <summary>
		/// The raw user input: only filled placeholder values, in mask order (literals and prompt chars are stripped).
		/// Example: mask "00-00" with display "12-34" → <c>"1234"</c>.
		/// </summary>
		public string MaskedInput
		{
			get
			{
				if (m_MaskState == null)
					return string.Empty;
				var sb = new StringBuilder();
				for (int i = 0; i < m_MaskState.Length; i++)
					if (IsPlaceholder(m_Mask[i]) && !IsEmpty(i))
						sb.Append(m_MaskState[i]);
				return sb.ToString();
			}
		}

		/// <summary>
		/// Pre-fills the mask with the given raw value (walked character by character into the placeholder positions, order preserved).
		/// Excess characters are ignored.
		/// </summary>
		public void SetValue(string rawValue)
		{
			if (m_MaskState == null)
			{
				Text = rawValue ?? string.Empty;
				return;
			}
			ResetMaskState();
			if (!string.IsNullOrEmpty(rawValue))
			{
				int ri = 0;
				for (int i = 0; i < m_MaskState.Length && ri < rawValue.Length; i++)
				{
					if (!IsPlaceholder(m_Mask[i]))
						continue;
					char c = rawValue[ri++];
					if (CanAcceptAt(i, c))
					{
						ConvertCase(i, ref c);
						m_MaskState[i] = c;
					}
				}
			}
			SetTextFromMaskState();
			CaretIndex = FirstEditablePosition();
			SelLength = 0;
			Invalidate();
		}

		/// <summary>Clears all user-entered values — the box returns to its initial (all-prompt) state.</summary>
		public void ClearMask()
		{
			if (m_MaskState == null) return;
			ResetMaskState();
			SetTextFromMaskState();
			SelectNone();
			Invalidate();
		}

		// ── helpers ─────────────────────────────────────────────────────────────

		private bool IsEmpty(int i)
		{
			return i >= 0 && i < m_MaskState.Length && IsPlaceholder(m_Mask[i]) && m_MaskState[i] == m_PromptChar;
		}

		private void ResetMaskState()
		{
			for (int i = 0; i < m_Mask.Length; i++)
				m_MaskState[i] = IsPlaceholder(m_Mask[i]) ? m_PromptChar : m_Mask[i];
		}

		/// <summary>Initialises the mask state array and pushes it into <see cref="TextBox.Text"/>.</summary>
		private void ApplyMask(string rawValue)
		{
			if (string.IsNullOrEmpty(m_Mask))
			{
				m_MaskState = null;
				return;
			}
			m_MaskState = new char[m_Mask.Length];
			ResetMaskState();

			if (!string.IsNullOrEmpty(rawValue))
			{
				int ri = 0;
				for (int i = 0; i < m_MaskState.Length && ri < rawValue.Length; i++)
				{
					if (!IsPlaceholder(m_Mask[i]))
						continue;
					char c = rawValue[ri++];
					if (CanAcceptAt(i, c))
					{
						ConvertCase(i, ref c);
						m_MaskState[i] = c;
					}
				}
			}

			SetTextFromMaskState();
			CaretIndex = FirstEditablePosition();
			SelLength = 0;
			CursorOn = true;
		}

		private int FirstEditablePosition()
		{
			if (m_MaskState == null) return 0;
			for (int i = 0; i < m_MaskState.Length; i++)
				if (IsEmpty(i))
					return i;
			return m_MaskState.Length;
		}

		#region Character validation
		/// <summary>A mask character the user must type exactly as shown (everything that is not a placeholder).</summary>
		private static bool IsLiteral(char c)
		{
			switch (c)
			{
				case '0': case '9': case '#': case 'L': case '?':
				case 'A': case 'a': case '*':
					return false;
			}
			return true;
		}

		private static bool IsPlaceholder(char c) { return !IsLiteral(c); }

		private static bool IsRequired(char c)
		{
			switch (c)
			{
				case '0': case 'L': case 'A': case 'a': case '*':
					return true;
			}
			return false;
		}

		private bool CanAcceptAt(int i, char c)
		{
			switch (char.ToUpperInvariant(m_Mask[i]))
			{
				case '0':
					return char.IsDigit(c);
				case '9':
				case '#':
					return char.IsDigit(c) || c == '+' || c == '-';
				case 'L':
				case '?':
					return char.IsLetter(c);
				case 'A':
				case 'a':
					return char.IsLetterOrDigit(c);
				case '*':
					return (int)c > 31;
			}
			return false;
		}

		private void ConvertCase(int i, ref char c)
		{
			if (m_Mask[i] == 'A') c = char.ToUpperInvariant(c);
			else if (m_Mask[i] == 'a') c = char.ToLowerInvariant(c);
		}
		#endregion

		/// <summary>Pushes the current mask state into <see cref="TextBox.Text"/> (only when it actually changed).</summary>
		private void SetTextFromMaskState()
		{
			string s = new string(m_MaskState);
			if (Text != s)
				Text = s;
		}

		// ── rendering ───────────────────────────────────────────────────────────

		/// <summary>
		/// The displayed text in a masked box is the full mask state: literal, user value and prompt character.
		/// </summary>
		public override string DisplayText
		{
			get { return m_MaskState != null ? new string(m_MaskState) : (Text ?? string.Empty); }
		}

		// ── input handling ──────────────────────────────────────────────────────

		/// <summary>Only mask-valid characters are accepted; invalid characters are rejected.
		/// (The base input path is replaced only when a mask is active — otherwise <see cref="TextBox.OnKeyPress"/> runs untouched.)</summary>
		public override bool OnKeyPress(KeyPressEventArgs e)
		{
			if (m_MaskState == null || ReadOnly)
				return base.OnKeyPress(e);

			char c = e.KeyChar;

			int pos = CaretIndex;

			// Caret at (or past) the end: everything filled — nothing to insert.
			if (pos >= m_Mask.Length)
			{
				CursorOn = true;
				return true;
			}

			// At a literal: only accepting the exact (already prefilled) character advances the caret.
			if (IsLiteral(m_Mask[pos]))
			{
				if (m_Mask[pos] == c)
				{
					CaretIndex = pos + 1;
					EnsureCursorVisible();
					CursorOn = true;
					Invalidate();
				}
				else
					CursorOn = true;
				return true;
			}

			// At a placeholder: validate, insert, advance.
			if (!CanAcceptAt(pos, c))
			{
				CursorOn = true;
				return true;
			}

			// If a selection is active, only overwrite from the caret when the caret is the selection start.
			if (SelLength > 0 && CaretIndex != SelStart)
			{
				DeleteSelection();
				SelLength = 0;
			}

			ConvertCase(pos, ref c);
			m_MaskState[pos] = c;
			SetTextFromMaskState();
			CaretIndex = pos + 1;
			SelStart = CaretIndex;
			EnsureCursorVisible();
			CursorOn = true;
			Invalidate();
			return true;
		}

		/// <summary>Mask-aware Backspace / Delete: a filled placeholder can be cleared back to its prompt character;
		/// prefilled literals are untouched. All other navigation and clipboard keys fall through to <see cref="TextBox.OnKeyDown"/>.</summary>
		public override bool OnKeyDown(KeyboardKeyEventArgs e)
		{
			if (m_MaskState == null || ReadOnly)
				return base.OnKeyDown(e);

			if (e.Key == Keys.Backspace && !e.Control)
			{
				if (SelLength > 0)
				{
					// collapse the selection; the caret ends up at its start
					CaretIndex = SelStart;
					SelLength = 0;
					EnsureCursorVisible();
					Invalidate();
					return true;
				}

				// Backspace does NOT delete; it moves the caret one edit position to
				// the left (the previous placeholder, skipping literals), so the
				// user can then overtype that position.
				int p = CaretIndex - 1;
				while (p >= 0 && IsLiteral(m_Mask[p]))
					p--;
				if (p >= 0)
				{
					CaretIndex = p;
					SelStart = CaretIndex;
					EnsureCursorVisible();
				}
				Invalidate();
				return true;
			}

			if (e.Key == Keys.Delete && !e.Control && !e.Shift)
			{
				int p = CaretIndex;
				if (p >= m_Mask.Length || IsLiteral(m_Mask[p]))
					return true;
				m_MaskState[p] = m_PromptChar;
				SetTextFromMaskState();
				Invalidate();
				return true;
			}

			return base.OnKeyDown(e);
		}

		/// <summary>Pastes content filtered by the mask (only characters that match the placeholder at the caret position are inserted).</summary>
		public override void Paste()
		{
			if (m_MaskState == null)
			{
				base.Paste();
				return;
			}
			if (!CanPaste)
				return;

			string content = Root?.CTX.GlWindow.ClipboardString;
			if (string.IsNullOrEmpty(content))
				return;

			int pos = CaretIndex;
			foreach (char c in content)
			{
				if (pos >= m_Mask.Length)
					break;
				if (!IsPlaceholder(m_Mask[pos]))
					continue; // literals are not part of the paste target
				if (CanAcceptAt(pos, c))
				{
					char cc = c;
					ConvertCase(pos, ref cc);
					m_MaskState[pos] = cc;
					pos++;
				}
			}
			SetTextFromMaskState();
			CaretIndex = pos;
			SelLength = 0;
			EnsureCursorVisible();
			Modified = true;
			Invalidate();
		}

		// ── navigation (skip prefilled literals) ────────────────────────────────

		protected override void MoveNextChar()
		{
			if (m_MaskState == null)
			{
				base.MoveNextChar();
				return;
			}

			int pos = CaretIndex;
			while (pos < m_Mask.Length)
			{
				if (IsPlaceholder(m_Mask[pos]))
					break; // reached an editable position
				pos++;
			}
			CaretIndex = Math.Min(pos, m_Mask.Length);
			if (CaretIndex <= 0)
				TextOffsetX = 0;
			EnsureCursorVisible();
		}

		protected override void MovePrevChar()
		{
			if (m_MaskState == null)
			{
				base.MovePrevChar();
				return;
			}

			int pos = CaretIndex;
			while (pos > 0)
			{
				pos--;
				if (IsPlaceholder(m_Mask[pos]))
					break;
			}
			CaretIndex = pos;
			if (CaretIndex <= 0)
				TextOffsetX = 0;
			EnsureCursorVisible();
		}
	}
}
