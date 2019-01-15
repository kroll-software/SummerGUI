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
using SummerGUI.Editor;
using SummerGUI.Scrolling;

namespace SummerGUI
{
	public class MultiLineTextEditWidgetStyle : WidgetStyle
	{
		public override void InitStyle ()
		{
			SetBackColor (Theme.Colors.Base02);
			SetForeColor (Theme.Colors.Base2);
			SetBorderColor (Color.Empty);
		}
	}

	public class MultiLineTextBox : ScrollableContainer, ITextBox, ISupportsFind, ISupportsClipboard, ISupportsSelection, ISupportsNonPrintingCharactersDisplay, ISupportsUndoRedo, ISupportsPersistency
    {				
		public MultiLineTextManager RowManager { get; private set; }
		public UndoRedoStack UndoRedoManager { get; private set; }

		public Pen CursorPen { get; private set; }

		public SpecialCharacterFlags TextOptions 
		{ 
			get {
				return RowManager.Flags;
			}
			set {
				if (RowManager.Flags != value) {
					RowManager.Flags = value;
				}
			}
		}			

		public bool WhiteSpaceVisible
		{
			get{
				return TextOptions.HasFlag(SpecialCharacterFlags.WhiteSpace);
			}
			set{				
				if (value != TextOptions.HasFlag (SpecialCharacterFlags.WhiteSpace)) {
					if (value)
						TextOptions |= SpecialCharacterFlags.WhiteSpace;
					else
						TextOptions &= ~SpecialCharacterFlags.WhiteSpace;
				}
			}
		}
			
		public bool LineBreaksVisible
		{
			get{
				return TextOptions.HasFlag(SpecialCharacterFlags.LineBreaks);
			}
			set{		
				if (value != TextOptions.HasFlag (SpecialCharacterFlags.LineBreaks)) {
					if (value)
						TextOptions |= SpecialCharacterFlags.LineBreaks;
					else
						TextOptions &= ~SpecialCharacterFlags.LineBreaks;
				}
			}
		}
			
		public bool EndOfTextVisible
		{
			get{
				return TextOptions.HasFlag(SpecialCharacterFlags.EndOfText);
			}
			set{			
				if (value != TextOptions.HasFlag (SpecialCharacterFlags.EndOfText)) {
					if (value)
						TextOptions |= SpecialCharacterFlags.EndOfText;
					else
						TextOptions &= ~SpecialCharacterFlags.EndOfText;
				}
			}
		}

		public CursorFlags CursorOptions { get; set; }

		public event EventHandler<EventArgs> TextChanged;
		public void OnTextChanged()
        {
            TextChanged?.Invoke(this, EventArgs.Empty);
        }

        public bool ReadOnly { get; set; }
		public bool Modified { get; set; }
		public bool AllowTabKey { get; set; }			

		public bool IsLoading { get; private set; }
		public override bool Enabled {
			get {
				return base.Enabled && !IsLoading;
			}
			set {
				base.Enabled = value;
			}
		}

		public string Text 
		{ 
			get {
				return RowManager.ToString();
			}
			set {
				IsLoading = true;
				OnEnabledChanged ();
				RowManager.LoadTextAsync (value, BreakWidth);
			}
		}

		private IGUIFont m_Font;
		public IGUIFont Font 
		{ 
			get{
				return m_Font;
			}
			set {
				if (m_Font != value) {
					m_Font = value;
					OnFontChanged ();
				}
			}
		}

		public void OnFontChanged()
		{
			ResetCachedLayout ();
		}						
			
		public override void Focus ()
		{						
			CursorOn = true;
			base.Focus ();
			RowManager.Active = true;
		}

		public void TabInto()
		{	
			Focus ();
			SelectAll ();
		}

		public bool HideSelection { get; set; }

		public override void OnLostFocus ()
		{
			if (HideSelection)
				SelLength = 0;
			RowManager.Active = false;
			base.OnLostFocus ();
		}

		[DpiScalable]
		public float CursorLineWidth 
		{ 
			get{
				return CursorPen.Width;
			}
			set {
				CursorPen.Width = value;
			}
		}

        public bool BreakWidthRulerVisible { get; set; }

		public MultiLineTextBox (string name) : this(name, new MultiLineTextEditWidgetStyle()) {}
		public MultiLineTextBox (string name, IWidgetStyle style)
			: base (name, Docking.Fill, style)
		{
			m_Font = SummerGUIWindow.CurrentContext.FontManager.MonoFont;
			RowManager = new MultiLineTextManager(this, SpecialCharacterFlags.All);
			UndoRedoManager = new UndoRedoStack (this, 100);
			CursorOptions = CursorFlags.Default;
			CanFocus = true;
			HideSelection = true;
			AutoScroll = true;
			ScrollBars = ScrollBars.Vertical;
			VScrollBar.SetColorScheme (ScrollBarColorSchemes.Dark);

			Padding = new Padding (Font.Height, Font.LineHeight / 2);
			CursorPen = new Pen(Theme.Colors.White, 1.5f);

			BreakWidthRulerVisible = true;

			RowManager.LoadingCompleted += delegate {
                RowManager.MoveHome();
                ScrollOffsetX = 0;
                ScrollOffsetY = 0;
                IsLoading = false;
				OnEnabledChanged ();

                RowManager.Paragraphs.UpdateCompleted += delegate {
					try {
						EnsureCurrentRowVisible ();
						Invalidate(1);
					} catch (Exception ex) {
						ex.LogError();
					}
				};
			};				
		}

		protected override void OnScaleWidget (IGUIContext ctx, float absoluteScaleFactor, float relativeScaleFactor)
		{			
			RowManager.OnScalingChanged (BreakWidth);
			base.OnScaleWidget (ctx, absoluteScaleFactor, relativeScaleFactor);
		}

		public override void OnUpdateTheme (IGUIContext ctx)
		{
			base.OnUpdateTheme (ctx);
			SelectionBrush = new SolidBrush (Color.FromArgb(212, Theme.Colors.Magenta));
		}

		protected override void OnRootChanged ()
		{
			base.OnRootChanged ();
			SelectionBrush = new SolidBrush (Color.FromArgb(212, Theme.Colors.Magenta));
		}

		public override SizeF PreferredSize (IGUIContext ctx, SizeF proposedSize)
		{
			return new SizeF (proposedSize.Width, Math.Max (proposedSize.Height, RowManager.Height + Padding.Height));
		}

		// *** Scrolling ***

		protected override void SetUpScrollbars (IGUIContext ctx)
		{	
			DocumentSize = new SizeF (RowManager.Width + Padding.Width, RowManager.Height + Padding.Height);
			base.SetUpScrollbars (ctx);

			if (VScrollBar != null)
				VScrollBar.SmallChange = RowManager.LineHeight;
			if (HScrollBar != null)
				HScrollBar.SmallChange = ScrollBar.ScrollBarWidth;			
		}


		private void EnsureCurrentParagraphVisible()
		{
			// not yet tested..

			if (IsMouseMoving)
				return;

			try {
				Paragraph para = RowManager.CurrentParagraph;

				if (!IsMouseOrKeyDown && para != null && para.Height < Height - Padding.Height) {
					int lineHeight = RowManager.LineHeight;
					float halfRowHeight = lineHeight / 2f;

					float paragraphTop = Top + (para.LineOffset * lineHeight) + ScrollOffsetY + Padding.Top;
					float paragraphBottom = paragraphTop + ((para.LineCount + 1) * lineHeight);

					// Ensure Paragraph Visible
					if (paragraphTop < 0) {
						VScrollBar.Value = para.Top;
					} else if (paragraphBottom > Height - halfRowHeight) {				
						VScrollBar.Value = para.Bottom - Height + Padding.Height;				
					}
				}
			} catch (Exception ex) {
				ex.LogError ();
			}
		}
			
		public void EnsureCurrentRowVisible ()
		{
			if (IsMouseMoving)
				return;

			//Console.WriteLine ("ScrollOffsetY: {0}, Cursor.Bottom: {1}", ScrollOffsetY, RowManager.CursorRectangle.Bottom);

			try {	
				Paragraph para = RowManager.CurrentParagraph;
				if (para == null)
					return;

				int lineHeight = RowManager.LineHeight;
				float halfRowHeight = lineHeight / 2f;
				int lineIndex = para.LineOffset + para.LineFromPosition(RowManager.CursorPosition);

				float rowTop = (lineIndex * lineHeight) + ScrollOffsetY + Padding.Top;
				float rowBottom = rowTop + lineHeight;

				// Ensure Current Row Visible
				if (rowTop < 0) {						
					VScrollBar.Value = (int)((lineIndex * lineHeight) + Padding.Top - halfRowHeight + 0.5f);
				}
				else if (rowBottom > Height - halfRowHeight) {						
					float newVal = (int)((lineIndex * lineHeight) + Padding.Top + lineHeight - Height + halfRowHeight + 0.5f);
					VScrollBar.Value = newVal;
					if (VScrollBar.Value < newVal) {
						SetupDocumentSize();
						VScrollBar.Value = newVal;
					}
				}
			} catch (Exception ex) {
				ex.LogError ();
			}				
		}


		public override bool OnSetupContextMenu (string widgetname)
		{						
			//this.SetContextMenu ("Edit");
			//return base.OnSetupContextMenu (widgetname);
			return true;
		}

		bool CursorOn = false;
		bool CursorVisible {
			get{
				//return Enabled && Focused;
				return IsFocused;
			}
		}			
			
		private int m_SelStart = 0;
		public int SelStart 
		{ 
			get {
				if (m_SelStart == 0)
					return 0;
				return Math.Min (m_SelStart, RowManager.AbsCursorPosition);
			}
			set {
				m_SelStart = value;
			}
		}

		public int SelLength { get; set; }

		public string SelectedText 
		{
			get {
				if (SelLength == 0)
					return null;
				return RowManager.GetCharRange (SelStart, SelLength);
			}
		}			

		void StartSelection()
		{				
			if (SelLength == 0)
				m_SelStart = RowManager.AbsCursorPosition;
		}

		void SetSelection()
		{		
			SelLength = Math.Abs(RowManager.AbsCursorPosition - m_SelStart);
		}

		void ResetSelection()
		{			
			SelLength = 0;
			m_SelStart = RowManager.AbsCursorPosition;
		}

		void SetSelection(bool shiftPressed)
		{
			if (shiftPressed) {				
				SetSelection ();
			} else {
				ResetSelection ();
			}
		}

		public void ScrollLineUp()
		{
			if (VScrollBar != null && VScrollBar.IsVisibleEnabled) {
				VScrollBar.Value -= VScrollBar.SmallChange;
			}
		}

		public void ScrollLineDown()
		{
			if (VScrollBar != null && VScrollBar.IsVisibleEnabled) {
				VScrollBar.Value += VScrollBar.SmallChange;
			}
		}

		protected void SetupDocumentSize()
		{
			if (VScrollBar != null) {
				VScrollBar.SetUp (VScrollBar.Height, RowManager.Height + Padding.Height, RowManager.LineHeight);
			}
		}
			
		public override bool OnKeyDown (KeyboardKeyEventArgs e)
		{
			if (ModifierKeys.AltPressed || !IsFocused)
				return false;

			//Console.WriteLine (e.Key);

			//if (base.OnKeyDown (e))
			//	return false;

			switch (e.Key) {
			case Key.ShiftLeft:
			case Key.ShiftRight:
				//if (SelLength == 0)
				//	SelStart = RowManager.AbsCursorPosition;
				return true;

			case Key.Left:
				if (e.Control)
					RowManager.MovePrevWord ();
				else
					RowManager.MovePrevChar ();
				SetSelection (e.Shift);
				break;
			case Key.Right:
				if (e.Control)
					RowManager.MoveNextWord ();
				else
					RowManager.MoveNextChar ();
				SetSelection (e.Shift);
				break;
			case Key.Up:
				if (e.Control) {
					ScrollLineUp ();
					return true;
				} else {
					RowManager.MovePrevLine ();
					SetSelection (e.Shift);
				}
				break;
			case Key.Down:
				if (e.Control) {
					ScrollLineDown ();
					return true;
				} else {
					RowManager.MoveNextLine ();
					SetSelection (e.Shift);
				}
				break;
			case Key.Home:
				if (e.Control)
					RowManager.MoveHome ();
				else
					RowManager.MoveParagraphHome ();
				SetSelection (e.Shift);
				break;
			case Key.End:
				if (e.Control)
					RowManager.MoveEnd ();
				else
					RowManager.MoveParagraphEnd ();
				SetSelection (e.Shift);
				break;
			case Key.PageUp:
				if (e.Control)
					return false;
				RowManager.MovePageUp ((int)((Height - Padding.Height) / RowManager.LineHeight));
				SetSelection (e.Shift);
				break;
			case Key.PageDown:
				if (e.Control)
					return false;
				RowManager.MovePageDown ((int)((Height - Padding.Height) / RowManager.LineHeight));
				SetSelection (e.Shift);
				break;
			case Key.BackSpace:				
				if (SelLength > 0) {					
					//this.SetUndoDelete (RowManager.AbsCursorPosition, 0);
					Delete ();
				} else {
					int pos = RowManager.AbsCursorPosition;
					UndoRedoManager.Do (new UndoRedoBackspaceMemento{
						ScrollOffset = ScrollOffset,
						SelStart = SelStart,
						SelLength = SelLength,
						SelectedText = SelectedText,
						Position = pos - 1,
						Data = RowManager.GetCharRange(pos - 1, 1),
					});
					RowManager.DeletePrevChar ();
				}
				ResetSelection ();
				SetupDocumentSize ();
				break;
			case Key.Enter:
                SetUndoInsert("\n");
                if (SelLength > 0)
                {
                    DeleteSelection();
                    SelLength = 0;
                }
				RowManager.InsertLineBreak ();
				SetupDocumentSize ();
                break;
			case Key.C:
				if (e.Control)
					Copy ();
				break;
			case Key.V:
				if (e.Control)
					Paste ();
				break;
			case Key.X:
				if (e.Control)
					Cut ();
				break;
			case Key.Delete:
				if (e.Shift)
					Cut ();
				else
					Delete ();
				SetupDocumentSize ();
				break;
			case Key.Insert:
				if (e.Control)
					Copy ();
				else if (e.Shift)
					Paste ();
				break;
			case Key.A:
				if (e.Control)
					SelectAll ();
				break;
			case Key.F:
				if (e.Control)
					Find ();
				break;
			case Key.Y:	// OpenTK sends an Y for a Z
				if (e.Control)
					Undo ();				
				break;
			case Key.Z: // OpenTK sends an Z for a Y
				if (e.Control)
					Redo ();				
				break;
			case Key.Escape:
				if (HideSelection)
					SelectNone ();
				return false;			
				/***
			case Key.Tab:
				if (!AllowTabKey)
					return false;
				InsertString (CursorPosition++, new String (' ', 4));
				CursorPosition += 3;
				break;
			***/
			default:
				//this.LogDebug ("OnKeyDown not handled: {0}", e.Key.ToString ());
				// F10
				//return false;
				break;
			}
						
			CursorOn = true;
			EnsureCurrentRowVisible ();
			Invalidate ();
			return true;
		}
			
		public override bool OnHeartBeat ()
		{
			if (!IsMouseOrKeyDown) {
				CursorOn = !CursorOn;
				return true;
			}
			return false;
		}						

		protected virtual float BreakWidth
		{
			get{		
				return Math.Max(20, Width - Padding.Width - VScrollBarWidth);
			}
		}			

		public override void OnResize ()
		{			
			RowManager.OnResize (BreakWidth);
			base.OnResize ();
		}

		protected virtual bool IsDefaultInputChar(char c)
		{
			return (int)c > 31;
		}

		public Func<char, bool> IsInputCharCallBack { get; set; }

		public bool IsInputChar(char c) 
		{
			if (IsInputCharCallBack != null)
				return IsInputCharCallBack (c);
			return IsDefaultInputChar (c);
		}

		void SetUndoInsert(string data)
		{
			UndoRedoManager.Do (new UndoRedoInsertMemento{
				ScrollOffset = ScrollOffset,
				SelStart = SelStart,
				SelLength = SelLength,
				SelectedText = SelectedText,
				Position = RowManager.AbsCursorPosition,
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
				Data = RowManager.GetCharRange(pos, delLen),
			});
		}

		public override bool OnKeyPress (KeyPressEventArgs e)
		{
			if (Enabled && !ReadOnly && IsInputChar (e.KeyChar)) {
				SetUndoInsert (e.KeyChar.ToString ());
				if (SelLength > 0) {
					DeleteSelection ();
					SelLength = 0;
				}
				RowManager.InsertChar(e.KeyChar);
                SelStart = RowManager.CursorPosition;

                EnsureCurrentRowVisible ();
				CursorOn = true;
				Invalidate ();
				return true;
			}

			return base.OnKeyPress (e);
		}

		public override void OnKeyUp (KeyboardKeyEventArgs e)
		{
			base.OnKeyUp (e);
			RowManager.Paragraphs.OnKeyUp ();
		}

		// *** Mouse ***

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
				RowManager.SetCursorPosition (e.X - Left - ScrollOffsetX - Padding.Left, e.Y - Top - ScrollOffsetY - Padding.Top);
				m_SelStart = RowManager.AbsCursorPosition;
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
			StopOutOfBoundsSelection ();
		}			

		public override void OnMouseMove (MouseMoveEventArgs e)
		{				
			base.OnMouseMove (e);
			if (IsMouseMoving) {				
				if (e.Y > Bottom - 2)
					StartOutOfBoundsSelection (1);
				else if (e.Y < Top + 2)
					StartOutOfBoundsSelection (-1);
				else {
					Invalidate (1);
					StopOutOfBoundsSelection ();
					EnsureCurrentRowVisible ();				
					RowManager.SetCursorPosition (e.X - Left - ScrollOffsetX - Padding.Left, e.Y - Top - ScrollOffsetY - Padding.Top);
					SetSelection ();
				}
			}
		}

		long lastDoubleClickTicks;
		public override void OnDoubleClick (MouseButtonEventArgs e)
		{
			base.OnDoubleClick (e);

			Paragraph para = RowManager.CurrentParagraph;
			if (para == null)
				return;
			if (Environment.TickCount - lastDoubleClickTicks < 500) {
				// Select Paragraph
				SelStart = para.PositionOffset;
				SelLength = para.Length - 1;
				RowManager.SetCursorAbsPosition(para.PositionOffset + para.Length - 1);
			} else {
				// Select Word
				RowManager.MovePrevWord();
				SelStart = para.PositionOffset + RowManager.CursorPosition;
				RowManager.MoveEndOfWord ();
				SelLength = (para.PositionOffset + RowManager.CursorPosition) - SelStart;

				Invalidate ();
				lastDoubleClickTicks = Environment.TickCount;
			}
		}

		public float ScrollOffsetX 
		{ 
			get {
				if (HScrollBar == null)
					return 0;					
				return -HScrollBar.Value;
			}
			set {
				if (HScrollBar != null)
					HScrollBar.Value = -value;
			}
		}
		public float ScrollOffsetY 
		{ 
			get {
				if (VScrollBar == null)
					return 0;					
				return -VScrollBar.Value;
			}
			set {
				if (VScrollBar != null)
					VScrollBar.Value = -value;
			}
		}

		public float VerticalScrollOffset {
			get {
				return Top + ScrollOffsetY;
			}
		}

		protected TaskTimer OutOfBoundsSelectionTimer { get; private set; }
		protected int OutOfBondsDirection;
		protected void StartOutOfBoundsSelection(int direction)
		{			
			if (VScrollBar == null || !VScrollBar.Visible)
				return;

			OutOfBondsDirection = direction;
			if (OutOfBoundsSelectionTimer == null) {
				OutOfBoundsSelectionTimer = new TaskTimer (50, () => {										
					if (OutOfBondsDirection < 0) {
						RowManager.SetCursorPosition (0, 0 - ScrollOffsetY - Padding.Top - RowManager.LineHeight);
						SetSelection ();
					} else if (OutOfBondsDirection > 0) {
						RowManager.SetCursorPosition (0, Height  - ScrollOffsetY + Padding.Height + RowManager.LineHeight + RowManager.LineHeight);
						SetSelection ();
					}
												
					EnsureCurrentRowVisible ();
					VScrollBar.Value += OutOfBondsDirection * VScrollBar.SmallChange;
				}, 500);
				OutOfBoundsSelectionTimer.Start ();
			} else if (!OutOfBoundsSelectionTimer.Enabled) {
				OutOfBoundsSelectionTimer.Start ();
			}
		}

		protected void StopOutOfBoundsSelection()
		{
			if (OutOfBoundsSelectionTimer != null) {
				OutOfBoundsSelectionTimer.Stop ();	
				OutOfBoundsSelectionTimer.Enabled = false;
			}
		}

		public override void OnLayout (IGUIContext ctx, RectangleF bounds)
		{						
			if (IsLayoutSuspended)				
				return;

			//VScrollBar.SetUp (VScrollBar.Height, RowManager.Height + Padding.Height, RowManager.LineHeight);
			//EnsureCurrentRowVisible ();
			base.OnLayout (ctx, bounds);
		}
			
		readonly Lazy<RectangleDrawingBuffer> painter = new Lazy<RectangleDrawingBuffer> (() => new RectangleDrawingBuffer (SummerGUIWindow.CurrentContext), false);
		public Brush SelectionBrush { get; set; }

		public override void Update (IGUIContext ctx)
		{
			// this sets RowManager.FirstParagraphOnScreen
			// to a common value for the following OnPaintBackground and OnPaint
			// so that all paintings are guaranteed synchronized and thus flicker-free

			// An important point about such implementations:
			// * The "manager" calculates everything in it's own absolute (0/0) metrics.
			// * The "presenter" (this class) finally adds all kinds of "Offsets" (Paddings, Scrolling, ..)
			// otherwise you'd have to deal with strange feedback effects
			// which can make life very very hard.

			RowManager.OnLayout(-ScrollOffsetY);
			base.Update (ctx);
		}

		public override void OnPaintBackground (IGUIContext ctx, RectangleF bounds)
		{
			base.OnPaintBackground (ctx, bounds);

			try {
				if (BreakWidthRulerVisible && Padding.Right > 0) {
					int breakWidth = (int)(bounds.Left + Padding.Left + BreakWidth);
					ctx.DrawLine (Theme.Pens.Base01, breakWidth, bounds.Top, breakWidth, bounds.Bottom);
				}

				painter.Value.Clear ();

				if (SelLength <= 0)
					return;

				int Start = SelStart;
				int End = Start + SelLength;
				int paraIndexEnd = RowManager.FindParagraphIndexByPosition(End);

				int firstParagraphIndex = RowManager.FirstParagraphOnScreen;
				if (firstParagraphIndex > paraIndexEnd)
					return;

				int rowHeight = RowManager.LineHeight;
				float rowBorder = 1f * ScaleFactor;

				Paragraph paraStart;
				ParagraphPosition ppStart;

				int paraIndex = RowManager.FindParagraphIndexByPosition(Start);

				int lineOffset = 0;

				if (paraIndex >= firstParagraphIndex) {
					paraStart = RowManager.Paragraphs [paraIndex];
					ppStart = paraStart.PositionAtIndex (Start - paraStart.PositionOffset);
					lineOffset = ppStart.LineIndex;
				} else {
					paraIndex = firstParagraphIndex;
					paraStart = RowManager.Paragraphs [paraIndex];
					lineOffset = firstParagraphIndex - paraStart.LineOffset;
					ppStart = paraStart.PositionAtIndex (0);
				}

				Paragraph paraEnd = RowManager.Paragraphs[paraIndexEnd];
				ParagraphPosition ppEnd = paraEnd.PositionAtIndex (End - paraEnd.PositionOffset);

				int absline = paraStart.LineOffset + ppStart.LineIndex;
				int absEndLine = paraEnd.LineOffset + ppEnd.LineIndex;

				float xStart = ppStart.ColumnStart + bounds.Left + Padding.Left;
				float yStart = bounds.Top + Padding.Top + (absline * rowHeight) + ScrollOffsetY;
				float xEnd = 0;

				if (absline == absEndLine) {
					xEnd = ppEnd.ColumnStart + bounds.Left + Padding.Left;
					painter.Value.AddRectangle (SelectionBrush, xStart, yStart, xEnd - xStart, rowHeight - rowBorder);

					// >>> Paint end exit ,,
					painter.Value.Flush ();
					return;
				}

				float startWidth = ppStart.ColumnStart;

				while (absline <= absEndLine) {
					foreach (float w in paraStart.LineWidths ().Skip(lineOffset)) {						
						if (absline++ == absEndLine || yStart > bounds.Bottom) {						
							startWidth = ppEnd.ColumnStart;
							painter.Value.AddRectangle (SelectionBrush, xStart, yStart, startWidth, rowHeight - rowBorder);

							// >>> Paint end exit ,,
							painter.Value.Flush ();
							return;
						}					

						if (yStart + rowHeight > bounds.Top && yStart < bounds.Bottom) {
							painter.Value.AddRectangle (SelectionBrush, xStart, yStart, w - startWidth, rowHeight - rowBorder);
						}
						yStart += rowHeight;
						startWidth = 0;
						xStart = bounds.Left + Padding.Left;
					}

					paraIndex++;
					if (paraIndex >= RowManager.Paragraphs.Count)
						break;
					paraStart = RowManager.Paragraphs[paraIndex];
					lineOffset = 0;
				}	
			} catch (Exception ex) {
				ex.LogWarning ();
			}				
		}

		public override void OnPaint (IGUIContext ctx, RectangleF bounds)
		{
			base.OnPaint (ctx, bounds);

			bounds.Offset (Padding.Left, Padding.Top);

			int lineHeight = RowManager.LineHeight;
			int offsetY = (int)ScrollOffsetY;

			int rowIndex = RowManager.FirstParagraphOnScreen;
			IGUIFont font = RowManager.Font;

			try {
				while (rowIndex < RowManager.Paragraphs.Count) {					
					Paragraph para = RowManager.Paragraphs[rowIndex];
					Rectangle rowBounds = new Rectangle((int)bounds.Left + (int)ScrollOffsetX, 
						(int)(bounds.Top + para.Top + offsetY),
						(int)bounds.Width, 
						lineHeight);

					var glyphLines = para.ToGlyphs();

					foreach (var line in glyphLines) {
						if (rowBounds.Bottom > bounds.Top) {							
							font.Begin(ctx);
							font.PrintTextLine (line.Select(g => g.Glyph).ToArray(), rowBounds, Style.ForeColorBrush.Color);
							font.End();
						}
						rowBounds.Offset(0, lineHeight);
						if (rowBounds.Top > bounds.Bottom)
							break;
					}

					if (rowBounds.Top > bounds.Bottom)
						break;

					rowIndex++;
				}

				if (CursorOn && CursorVisible) {					
					RectangleF CursorRectangle = RowManager.CalcCursorRectangle();
					int x = Math.Max((int)bounds.Left + 1, (int)(CursorRectangle.X + bounds.Left + ScrollOffsetX + 0.5f));
					float y1 = CursorRectangle.Top + bounds.Top + ScrollOffsetY + 2;
					float y2 = y1 + CursorRectangle.Height - 4;
					ctx.DrawLine (CursorPen, x, y1, x, y2);
				}	
			} catch (Exception ex) {
				ex.LogError ();
			}
		}


        // ISupportsPersistency
        public virtual bool CanOpen { get; set; }
        public virtual bool CanClose { get; set; }
        public virtual bool CanSave { get; set; }
        public virtual bool CanSaveAs { get; set; }

        public virtual bool CanNew
        {
            get
            {
                return RowManager.HasText;
            }
        }


        // *** Clipboard ***

        public void Cut()
		{
			if (!CanCut)
				return;

			int pos = RowManager.AbsCursorPosition;
			this.SetUndoDelete (pos, 0);

			string content = RowManager.GetCharRange(SelStart, SelLength);
			DeleteSelection ();
			PlatformExtensions.SetClipboardText (content);
			ResetSelection ();
			Invalidate ();
		}

		public void Copy()
		{
			if (!CanCopy)
				return;
			string content = RowManager.GetCharRange(SelStart, SelLength);
			PlatformExtensions.SetClipboardText (content);
			Modified = true;
		}

		public void Paste()
		{
			if (!CanPaste)
				return;

			string content = PlatformExtensions.GetClipboardText ();

			// Important: filter valid characters
			if (content != null) {
				content = new string (content.Where (IsInputChar).ToArray ());
			}

			if (!String.IsNullOrEmpty (content)) {
				SetUndoInsert (content);
				if (SelLength > 0)
					DeleteSelection ();
				SelLength = 0;
				RowManager.InsertRange (PlatformExtensions.GetClipboardText ());
				EnsureCurrentRowVisible ();
				ResetSelection ();
				Modified = true;
				Invalidate ();
			}
		}

		public void DeleteSelection()
		{
			if (CanDelete) {
				RowManager.DeleteRange (SelStart, SelLength);
				RowManager.SetCursorAbsPosition (SelStart);
				ResetSelection ();
				Modified = true;
			}
		}

		public void Delete()
		{
			if (!CanDelete)
				return;

			int pos = RowManager.AbsCursorPosition;
			if (SelLength > 0) {
				this.SetUndoDelete (pos, 0);
				DeleteSelection ();
			} else {
				this.SetUndoDelete (pos, 1);
				RowManager.DeleteCurrentChar ();
			}
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
				return IsVisibleEnabled && !ReadOnly && PlatformExtensions.IsClipboardTextAvailable();
			}
		}

		public virtual bool CanDelete
		{
			get{
				return IsVisibleEnabled && !ReadOnly && !RowManager.IsEmpty;
			}
		}
			
		public void DeleteRange(int pos, int len)
		{
			if (ReadOnly)
				return;
			RowManager.DeleteRange (pos, len);
		}						

		// Selection

		public virtual bool CanSelectAll
		{
			get{
				//return IsVisibleEnabled && !String.IsNullOrEmpty(Text) && SelectionManager.Distance < Text.Length;
				return IsVisibleEnabled && !RowManager.IsEmpty;
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
				return IsVisibleEnabled && SelLength > 0;
			}
		}

		public virtual void SelectAll()
		{
			if (!CanSelectAll)
				return;
			RowManager.MoveHome ();
			ResetSelection ();
			RowManager.MoveEnd ();
			SetSelection ();
			EnsureCurrentRowVisible ();
			Update (true);
		}

		public virtual void SelectNone()
		{
			if (!CanSelectNone)
				return;			
			SelLength = 0;
			Invalidate (1);
		}

		public virtual void InvertSelection()
		{
			if (!CanInvertSelection)
				return;
		}

		// *** ISupportsFind ***

		//int LastFindIndex = -1;
		//string LastFindString;
		public bool CanFind
		{
			get{
				return IsVisibleEnabled && !RowManager.IsEmpty;
			}
		}

		public bool CanFindNext
		{
			get{

				return IsVisibleEnabled && false;
				//return LastFindString != null && !RowManager.IsEmpty;
			}
		}

		public bool CanFindPrevious
		{
			get{

				return IsVisibleEnabled && false;
				//return LastFindString != null && !RowManager.IsEmpty;
			}
		}

		public void FindText(string searchstring, FindDirections direction)
		{
			ParentWindow.ShowInfo ("The search function is under construction.");
		}

		public void Find()
		{
			if (Parent as TextEditorEnsemble != null) {
				(Parent as TextEditorEnsemble).Find ();
			} else {
				// ToDo:
			}
		}

		public void FindNext()
		{
		}

		public void FindPrevious()
		{
		}

		// ****** ISupportsUndoRedo

		public bool CanUndo
		{
			get{

				return IsVisibleEnabled && !ReadOnly && UndoRedoManager.CanUndo;
			}
		}

		public bool CanRedo
		{
			get{

				return IsVisibleEnabled && !ReadOnly && UndoRedoManager.CanRedo;
			}
		}

		public void Undo()
		{
			UndoRedoManager.Undo ();
		}

		public void Redo()
		{
			UndoRedoManager.Redo ();
		}

		// ITextBox Implementation for UndoRedo Stack

		public PointF ScrollOffset
		{
			get{
				return new PointF (ScrollOffsetX, ScrollOffsetY);
			}
			set{
				ScrollOffsetX = value.X;
				ScrollOffsetY = value.Y;
			}
		}			
		public void SetCursorPosition (int pos)
		{
			RowManager.SetCursorAbsPosition (pos);
		}
		public void InsertRange (string text)
		{
			RowManager.InsertRange (text);
		}

		protected override void CleanupUnmanagedResources ()
		{			

			if (OutOfBoundsSelectionTimer != null) {
				StopOutOfBoundsSelection ();
				OutOfBoundsSelectionTimer.Dispose ();
			}
				
			if (painter.IsValueCreated) {
				painter.Value.Clear ();
				painter.Value.Dispose ();
			}
				
			UndoRedoManager.Dispose ();
			RowManager.Dispose ();
			base.CleanupUnmanagedResources ();
		}
	}
}

