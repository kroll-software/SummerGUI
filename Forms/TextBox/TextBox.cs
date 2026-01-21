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
using SummerGUI.Editor;

namespace SummerGUI
{		
	public class TextBox : Widget, ITextBox, ISupportsClipboard, ISupportsUndoRedo, ISupportsSelection
	{
		public static readonly FontFormat DefaultSingleLineFontFormat = new FontFormat (Alignment.Near, Alignment.Center, FontFormatFlags.None);

		public static readonly char DefaultPasswortChar = '●';

		public event EventHandler<EventArgs> TextChanged;
		public void OnTextChanged()
		{
			ResetCachedLayout ();
            TextChanged?.Invoke(this, EventArgs.Empty);
        }

        private string m_Text;
		public string Text 
		{ 
			get {
				return m_Text;
			}
			set {
				if (m_Text != value) {
					m_Text = value;
					OnTextChanged ();
				}
			}
		}

		private char m_PasswordChar;
		public char PasswordChar 
		{ 
			get {
				return m_PasswordChar;
			}
			set {
				// Fallback password character
				if (value > 31 && Font != null && !Font.ContainsChar(value))
					value = '*';
				m_PasswordChar = value;
			}
		}

		public virtual string DisplayText
		{
			get{
				if (Text == null)
					return String.Empty;
				if (m_PasswordChar > 31)
					return new String (m_PasswordChar, Text.Length);
				return Text;
			}
		}
			
		IGUIFont m_Font;
		public IGUIFont Font 
		{ 
			get {
				return m_Font;
			}
			set {
				if (m_Font != value) {
					m_Font = value;
					OnFontChanged ();
				}
			}
		}

		public virtual void OnFontChanged()
		{
			ResetCachedLayout ();
		}

		public bool ReadOnly { get; set; }
		public bool Modified { get; set; }
		public bool AllowTabKey { get; set; }
		public FontFormat Format { get; set; }			

		public bool HideSelection { get; set; }

		public TextBox (string name, string text = null, bool transparent = false)			
			: base (name, Docking.Fill, transparent ? new TransparentTextBoxWidgetStyle() as IWidgetStyle : new TextBoxWidgetStyle() as IWidgetStyle)
		{
			if (transparent) {
				Styles.SetStyle (new TransparentTextBoxActiveWidgetStyle (), WidgetStates.Active); 
				Styles.SetStyle (new TransparentTextBoxDisabledWidgetStyle (), WidgetStates.Disabled);
			} else {
				Styles.SetStyle (new TextBoxActiveWidgetStyle (), WidgetStates.Active);
				Styles.SetStyle (new TextBoxDisabledWidgetStyle (), WidgetStates.Disabled); 
			}

			UndoRedoManager = new UndoRedoStack (this, 100);

			Font = FontManager.Manager.DefaultFont;
			MaxSize = new SizeF (MaxSize.Width, Font.TextBoxHeight);
			Format = DefaultSingleLineFontFormat;
            			
			TextMargin = new Size (6, 2);

			CursorLineWidth = 1f;
			HideSelection = true;

			Text = text;
			if (Text != null)
				CursorPosition = Text.Length;

			AllowTabKey = true;
			CanFocus = true;
		}
			
		public override void Focus ()
		{			
			if (Text != null)
				CursorPosition = Text.Length;
			CursorOn = true;
			base.Focus ();
			//Selected = true;
			WidgetState = WidgetStates.Active;
		}

		public override void OnLostFocus ()
		{
			SelLength = 0;
			base.OnLostFocus ();
			//Selected = false;
		}
			
		public void TabInto()
		{	
			Focus ();
			SelectAll ();
		}

		public override bool OnSetupContextMenu (string widgetname)
		{						
			return true;
		}

		public override void OnResize ()
		{
			base.OnResize ();
			EnsureCursorVisible ();
		}						
			
		public override SizeF PreferredSize (IGUIContext ctx, SizeF proposedSize)
		{			
			if (Font == null)
				return base.PreferredSize (ctx, proposedSize);
			return new SizeF(proposedSize.Width, Font.TextBoxHeight);
		}		

		private float TextOffsetX = 0;
		bool CursorOn = false;
		bool CursorVisible {
			get{
				return IsFocused;
			}
		}

		int CursorPosition = 0;
		float CursorPosPix {
			get{				
				return Font.MeasureGlyphs (DisplayText.StrLeft (CursorPosition)).Width + TextOffsetX;
			}
		}
			
		private int m_SelStart = 0;
		public int SelStart 
		{ 
			get {
				return Math.Min (m_SelStart, CursorPosition);
			}
			set {
				m_SelStart = value;
			}
		}

		public int SelLength { get; set; }

		public string SelectedText 
		{
			get{
				if (SelLength <= 0)
					return String.Empty;
				return DisplayText.StrMid (SelStart + 1, SelLength);
			}
		}

		private SizeF m_TextMargin;
		[DpiScalable]
		public SizeF TextMargin 
		{ 
			get {
				return m_TextMargin;
			}
			set {
				if (m_TextMargin != value) {
					m_TextMargin = value;
					OnTextMarginChanged ();
				}
			}
		}

		protected virtual void OnTextMarginChanged()
		{
			ResetCachedLayout ();
		}


		[DpiScalable]
		public float CursorLineWidth { get; set; }

		public override void OnPaint (IGUIContext ctx, RectangleF bounds)
		{
			base.OnPaint (ctx, bounds);
			bounds.Inflate (-TextMargin.Width, -TextMargin.Height);

			if (!String.IsNullOrEmpty (Text)) {
				ctx.DrawSelectedString(DisplayText, Font, SelStart, SelLength, bounds, TextOffsetX, Format,
					Style.ForeColorBrush.Color, Theme.Colors.HighLightBlue, Theme.Colors.White);
			}

			if (CursorOn && CursorVisible) {
				Style.ForeColorPen.Width = CursorLineWidth;
				float cp = Math.Max(3f, CursorPosPix + bounds.Left);
				ctx.DrawLine (Style.ForeColorPen, cp, bounds.Top + 1f, cp, bounds.Bottom - 2f);
			}
		}

		public void InsertChar(int pos, char value)
		{
			InsertRange (pos, value.ToString ());
		}

		public void InsertRange(string value)
		{
			InsertRange (CursorPosition, value);
		}

		public virtual void InsertRange(int pos, string value)
		{
			if (ReadOnly)
				return;

			if (Text == null) {
				Text = value;
				return;
			}

			if (pos < 0)
				Text = value + Text;
			else if (pos >= Text.Length)
				Text += value;
			else
				Text = Text.Insert (pos, value);

			Modified = true;
		}

		public virtual void DeleteChar(int pos)
		{
			if (ReadOnly)
				return;
			
			if (Text == null || pos < 0 || pos >= Text.Length)
				return;
			Text = Text.StrLeft (pos) + Text.StrMid (pos + 2);
			Modified = true;
		}

		public void DeleteRange(int pos, int len)
		{
			if (ReadOnly)
				return;

			if (Text == null || len == 0 || pos < 0 || pos >= Text.Length)
				return;
			Text = Text.StrLeft (pos) + Text.StrMid (pos + len + 1);
			Modified = true;
		}

		public void DeleteSelection()
		{
			if (SelLength > 0 && !ReadOnly) {
				CursorPosition = SelStart;	// swaps when necessary
				DeleteRange (SelStart, SelLength);
			}
		}

		void StartSelection()
		{
			if (SelLength == 0)
				m_SelStart = CursorPosition;
		}

		void SetSelection()
		{
			SelLength = Math.Abs(CursorPosition - m_SelStart);
		}

		void ResetSelection()
		{			
			SelLength = 0;
			m_SelStart = CursorPosition;
		}

		protected virtual void MovePrevChar()
		{
			if (CursorPosition > 0)
				CursorPosition--;

			if (CursorPosition <= 0)
				TextOffsetX = 0;
		}

		protected virtual void MoveNextChar()
		{
			if (Text != null && CursorPosition < Text.Length)
				CursorPosition++;
		}			

		protected virtual void MovePrevWord()
		{
			if (String.IsNullOrEmpty(Text))
				return;

			if (CursorPosition >= Text.Length)
				CursorPosition = Text.Length - 1;

			if (CursorPosition > 1 && Text[CursorPosition - 1] == ' ')
				CursorPosition--;

			while (CursorPosition > 0 && Text[CursorPosition] == ' ')
				CursorPosition--;

			while (CursorPosition > 0  && Text[CursorPosition] != ' ')
				CursorPosition--;

			if (CursorPosition > 0 && Text[CursorPosition] == ' ')
				CursorPosition++;

			if (CursorPosition <= 0)
				TextOffsetX = 0;
		}

		protected virtual void MoveNextWord()
		{			
			if (String.IsNullOrEmpty(Text))
				return;

			if (CursorPosition >= Text.Length)
				CursorPosition = Text.Length - 1;

			while (CursorPosition < Text.Length && Text[CursorPosition] != ' ')
				CursorPosition++;

			while (CursorPosition < Text.Length && Text[CursorPosition] == ' ')
				CursorPosition++;
		}

		protected virtual void MoveHome()
		{
			CursorPosition = 0;
			TextOffsetX = 0;
		}

		protected virtual void MoveEnd()
		{
			if (Text == null)
				CursorPosition = 0;
			else
				CursorPosition = Text.Length;
		}			

		protected virtual void EnsureCursorVisible()
		{
			string text = DisplayText;
			if (String.IsNullOrEmpty (text) || Bounds.IsEmpty) {
				TextOffsetX = 0;		
				return;
			}

			// Text length left from Cursor
			float lenLeft = Font.MeasureGlyphs (text, 0, CursorPosition).Width;
			// add text length right from Cursor
			float totalLength = lenLeft + Font.MeasureGlyphs (text, CursorPosition).Width;

			float contentWidth = Bounds.Width - (TextMargin.Width * 2f);
			float cursorX = lenLeft + TextOffsetX;

			// Case 1: Cursor > right border
			if (cursorX > contentWidth) {
				//this.LogDebug ("Case 1: Cursor > right border");
				TextOffsetX = contentWidth - lenLeft;
			}

			// Case 2: Cursor < left border
			else if (cursorX < TextMargin.Width) {
				//this.LogDebug ("Case 2: Cursor < left border");
				TextOffsetX = -lenLeft;
			}

			// Case 3: prevent space at right (after delete)
			if (TextOffsetX < 0 && totalLength + TextOffsetX < contentWidth) {
				//this.LogDebug ("Case 3: space at right");
				TextOffsetX = contentWidth - totalLength;
			}

			// Case 4: prevent space at left (rounding errors, fail safe)
			if (TextOffsetX > 0)
				TextOffsetX = 0;
		}
			
		void SetSelection(bool shiftPressed)
		{
			if (shiftPressed) {				
				SetSelection ();
			} else {
				ResetSelection ();
			}
		}

		public override bool OnKeyDown (KeyboardKeyEventArgs e)
		{
			if (ModifierKeys.AltPressed || !IsFocused)
				return false;

			//base.OnKeyDown (e);

			switch (e.Key) {
			case Keys.LeftShift:
			case Keys.RightShift:
			case Keys.LeftControl:
			case Keys.RightControl:
			case Keys.LeftAlt:
			case Keys.RightAlt:
				return true;
				
			case Keys.Left:
				if (e.Control)
					MovePrevWord ();
				else
					MovePrevChar ();
				SetSelection (e.Shift);
				break;
			case Keys.Right:
				if (e.Control)
					MoveNextWord ();
				else
					MoveNextChar ();
				SetSelection (e.Shift);
				break;
			case Keys.Home:
				MoveHome ();
				SetSelection (e.Shift);
				break;
			case Keys.End:
				MoveEnd ();
				SetSelection (e.Shift);
				break;
			case Keys.PageUp:
			case Keys.PageDown:
				SetSelection (e.Shift);
				return true;
			case Keys.Backspace:
				if (SelLength > 0) {
					//this.SetUndoDelete (CursorPosition, 0);
					Delete ();
				} else if (CursorPosition > 0) {					
					UndoRedoManager.Do (new UndoRedoBackspaceMemento{
						ScrollOffset = ScrollOffset,
						SelStart = SelStart,
						SelLength = SelLength,
						SelectedText = SelectedText,
						Position = CursorPosition - 1,
						Data = Text.StrMid(CursorPosition, 1),
					});
					DeleteChar (--CursorPosition);
				}
				ResetSelection ();
				break;
			case Keys.C:
				if (e.Control)
					Copy ();
				break;
			case Keys.V:
				if (e.Control)
					Paste ();
				break;
			case Keys.X:
				if (e.Control)
					Cut ();
				break;
			case Keys.Delete:
				if (e.Shift)
					Cut ();
				else
					Delete ();
				break;
			case Keys.Insert:
				if (e.Control)
					Copy ();
				else if (e.Shift)
					Paste ();
				break;
			case Keys.A:
				if (e.Control)
					SelectAll ();				
				break;
			case Keys.Y:	// OpenTK sends an Y for a Z
				if (e.Control)
					Undo ();				
				break;
			case Keys.Z: // OpenTK sends an Z for a Y
				if (e.Control)
					Redo ();				
				break;
			case Keys.Escape:
				if (HideSelection)
					SelectNone ();
				return false;
			case Keys.Enter:
				return false;			
			case Keys.Tab:
				return false;
				//if (!AllowTabKey)
				//	return false;
				//InsertString (CursorPosition++, new String (' ', 4));
				//CursorPosition += 3;
				//break;	
			case Keys.F10:
				return false;
			default:
				//this.LogDebug ("OnKeyDown not handled: {0}", e.Key.ToString ());
				//return false;
				break;
			}
						
			EnsureCursorVisible ();
			CursorOn = true;
			Invalidate ();
			return true;
		}
			
		public override bool OnHeartBeat ()
		{
			if (!IsVisibleEnabled || !IsFocused)
			{
				CursorOn = false;
				return false;
			}
			
			if (!IsMouseOrKeyDown) {
				CursorOn = !CursorOn;
				return true;
			}
			return false;
		}

		protected virtual bool IsDefaultInputChar(char c)
		{
			return (int)c > 31;
		}

		public Func<char, bool> IsInputCharCallBack { get; set; }

		public bool IsInputChar(char c) 
		{
			if (ModifierKeys.AltPressed)
				return false;
			if (IsInputCharCallBack != null)
				return IsInputCharCallBack (c);
			return IsDefaultInputChar (c);
		}
			
		public override bool OnKeyPress (KeyPressEventArgs e)
		{
			if (Enabled && !ReadOnly && IsInputChar (e.KeyChar)) {
                SetUndoInsert (e.KeyChar.ToString ());
				if (SelLength > 0) {
					DeleteSelection ();
					SelLength = 0;
				}
				InsertChar(CursorPosition++, e.KeyChar);
                SelStart = CursorPosition;

                EnsureCursorVisible ();
				CursorOn = true;
				Invalidate ();
				return true;
			}

			return base.OnKeyPress (e);
		}
			

		public override void OnMouseEnter (IGUIContext ctx)
		{			
			if (Visible && Enabled)
				Cursor = Cursors.Text;
			else
				Cursor = Cursors.Default;
			base.OnMouseEnter (ctx);
		}

		protected bool IsMouseMoving { get; private set; }

		public override void OnMouseDown (MouseButtonEventArgs e)
		{	
			base.OnMouseDown (e);

			if (e.Button == MouseButton.Left) {				
				CursorPosition = Font.CharPos (DisplayText, e.X - Bounds.Left - TextOffsetX - TextMargin.Width);
				m_SelStart = CursorPosition;
				if (!ModifierKeys.ShiftPressed)
					SelLength = 0;
				IsMouseMoving = true;
				CursorOn = true;
			}

			Invalidate ();
		}

		public override void OnMouseUp (MouseButtonEventArgs e)
		{
			IsMouseMoving = false;
			base.OnMouseUp (e);
		}


		// ToDo: Mouse-Selection with autoscroll / timer

		public override void OnMouseMove (MouseMoveEventArgs e)
		{
			base.OnMouseMove (e);

			if (IsMouseMoving) {				
				CursorPosition = Font.CharPos (DisplayText, e.X - Bounds.Left - TextOffsetX - TextMargin.Width);
				SetSelection ();
				//EnsureCursorVisible ();
				Invalidate ();
			}
		}

		long lastDoubleClickTicks;
		public override void OnDoubleClick (MouseButtonEventArgs e)
		{
			base.OnDoubleClick (e);
			if (Environment.TickCount - lastDoubleClickTicks < 500) {
				SelectAll ();
			} else {
				// Select Word
				int pos = Font.CharPos (DisplayText, e.X - Bounds.Left - TextOffsetX - TextMargin.Width);
                if (pos < Text.Length)
                {
    				int start = pos;
    				while (start >= 0 && Text [start] != ' ')
    					start--;
    				int end = pos;
    				while (end < Text.Length && Text [end] != ' ')
    					end++;
    				SelStart = start + 1;
    				SelLength = Math.Max(0, end - start - 1);
    				CursorPosition = SelStart + SelLength;
                }

                lastDoubleClickTicks = Environment.TickCount;
				Invalidate ();
			}
		}

		// *** ISupportsClipboard Implementationn ***

		// ToDo:

		public void Cut()
		{
			if (!CanCut)
				return;
			this.SetUndoDelete (CursorPosition, 0);
			try{
				Root.CTX.GlWindow.ClipboardString = SelectedText;
			}
			catch{}
			Text = Text.Remove (SelStart, SelLength);
			ResetSelection ();
			Modified = true;
			Invalidate ();
		}

		public void Copy()
		{
			if (!CanCopy)
				return;
			try{
				Root.CTX.GlWindow.ClipboardString = SelectedText;
			}
			catch{}
			Modified = true;
		}

		public void Paste()
		{
			if (!CanPaste)
				return;				
			
			string content = Root?.CTX.GlWindow.ClipboardString;
			if (String.IsNullOrEmpty (content))
				return;

			// Important: filter valid characters
			if (content != null) {
				content = new string (content.Where (IsInputChar).ToArray ());
			}

			if (!String.IsNullOrEmpty (content)) {
				SetUndoInsert (content);
				if (SelLength > 0)
					DeleteSelection ();
				SelLength = 0;
				if (Text == null || (CursorPosition >= Text.Length))
					Text += content;
				else
					Text = Text.Insert (CursorPosition.Clamp(0, Text.Length - 1), content);
				
				CursorPosition += content.Length;
				ResetSelection ();
				EnsureCursorVisible ();
				Modified = true;
				Invalidate ();
			}
		}			

		public void Delete()
		{
			if (!CanDelete)
				return;

			if (SelLength > 0) {
				this.SetUndoDelete (CursorPosition, 0);
				DeleteSelection ();
			} else {
				this.SetUndoDelete (CursorPosition, 1);
				DeleteChar (CursorPosition);
			}
			ResetSelection ();
		}

		public virtual bool CanCut
		{
			get{
				return IsVisibleEnabled && !ReadOnly && SelLength > 0;
			}
		}

		public virtual bool CanCopy
		{
			get{				
				return IsVisibleEnabled && SelLength > 0;
			}
		}

		public virtual bool CanPaste
		{
			get{
				return IsVisibleEnabled && !ReadOnly && Root != null;
			}
		}

		public virtual bool CanDelete
		{
			get{
				return IsVisibleEnabled && !ReadOnly && Text != null && (CursorPosition < Text.Length || SelLength > 0);
			}
		}

		// *** ISupportsUndo Implementationn ***

		// ToDo:

		public virtual bool CanUndo
		{
			get{
				return IsVisibleEnabled && !ReadOnly && UndoRedoManager.CanUndo;
			}
		}

		public virtual bool CanRedo
		{
			get{
				return IsVisibleEnabled && !ReadOnly && UndoRedoManager.CanRedo;
			}
		}

		public virtual void Undo()
		{
			UndoRedoManager.Undo ();
		}

		public virtual void Redo()
		{
			UndoRedoManager.Redo ();
		}

		// Selection

		public virtual bool CanSelectAll
		{
			get{
				return IsVisibleEnabled && !String.IsNullOrEmpty(Text) && SelLength < Text.Length;
			}
		}

		public virtual bool CanSelectNone
		{
			get{
				return IsVisibleEnabled && SelLength > 0;
			}
		}

		public virtual bool CanInvertSelection
		{
			get{
				return false;
				//return IsVisibleEnabled && SelLength > 0;
			}
		}			
			
		public virtual void SelectAll()
		{
			if (!CanSelectAll)
				return;			
			CursorPosition = Text.Length;
			SelStart = 0;
			SelLength = Text.Length;
			EnsureCursorVisible ();
			Invalidate ();
		}

		public virtual void SelectNone()
		{
			if (!CanSelectNone)
				return;			
			SelLength = 0;
			Invalidate ();
		}
			

		public virtual void InvertSelection()
		{
			if (!CanInvertSelection)
				return;			
		}

		// ITextBox Implementation for Undo/Redo

		public UndoRedoStack UndoRedoManager { get; private set; }

		void SetUndoInsert(string data)
		{
			UndoRedoManager.Do (new UndoRedoInsertMemento{
				ScrollOffset = ScrollOffset,
				SelStart = SelStart,
				SelLength = SelLength,
				SelectedText = SelectedText,
				Position = CursorPosition,
				Data = data,
			});
		}

		void SetUndoDelete(int pos, int delLen)
		{
			UndoRedoManager.Do (new UndoRedoDeleteMemento{
				ScrollOffset = ScrollOffset,
				SelStart = SelStart,
				SelLength = SelLength,
				SelectedText = SelectedText,
				Position = pos,
				Data = Text.StrMid(pos + 1, delLen),
			});
		}

		public PointF ScrollOffset
		{
			get{
				return new PointF (TextOffsetX, 0);
			}
			set{
				TextOffsetX = value.X;
			}
		}

		/***
		public void DeleteRange (int start, int len)
		{
			DeleteString (start, len);
		}
		***/
		public void SetCursorPosition (int pos)
		{
			CursorPosition = pos;
		}
		/***
		public void InsertRange (string text)
		{
			InsertString (CursorPosition, text);
		}
		***/

		protected override void CleanupManagedResources ()
		{			
			UndoRedoManager.Dispose ();
			base.CleanupManagedResources ();
		}			
	}
}

