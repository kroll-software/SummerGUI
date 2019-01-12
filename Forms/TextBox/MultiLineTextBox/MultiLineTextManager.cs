using System;
using System.Text;
using System.Diagnostics;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KS.Foundation;

namespace SummerGUI.Editor
{
	public class ParagraphList : BinarySortedList<Paragraph>
	//public class ParagraphList : List<Paragraph>
	//public class ParagraphList : BalancedOrderStatisticTree<Paragraph>
	{		
		public event EventHandler<EventArgs> UpdateCompleted;

		//private object SyncObject = new object ();
		IAsyncResult AsyncResult;
		CancellationTokenSource TokenSource;

		IAsyncResult AsyncScalingWordWrapResult;
		CancellationTokenSource TokenScalingWordWrapSource;

		public int LineHeight { get; set; }
		public int LineCount { get; set; }
		public float BreakWidth { get; private set; }
		public int Length { get; set; }

		public bool RequiresFullRefresh { get; private set; }
		int refreshLastIndex = 0;

		public void OnKeyUp()
		{
			if (RequiresFullRefresh) {
				OnUpdateAsync (refreshLastIndex, BreakWidth);
			}
		}

		public ParagraphList(int lineHeight, float breakWidth) : base()
		{
			LineHeight = lineHeight;
			BreakWidth = breakWidth;
		}

		public void CancelAll()
		{
			if (TokenSource != null && !TokenSource.IsCancellationRequested)
				TokenSource.Cancel ();
			if (TokenScalingWordWrapSource != null && !TokenScalingWordWrapSource.IsCancellationRequested)
				TokenScalingWordWrapSource.Cancel ();
		}

		public new void Clear()
		{
			CancelAll();
			base.Clear();
		}

		public float Height 
		{ 
			get {
				return LineCount * LineHeight;
			}
		}

		public float Width { get; private set; }

		/***
		public void AddLast(Paragraph p)
		{
			Add (p);
		}
		***/

		public new void Add(Paragraph p)
		{
			if (p == null)
				return;
			//p.Height = p.LineCount * LineHeight;
			base.AddLast (p);
			//base.Add (p);
		}
			
		public override void OnInsert (Paragraph elem)
		{
			elem.Height = elem.LineCount * LineHeight;
			base.OnInsert (elem);
		}

		public void OnUpdateCompleted()
		{
			
			if (UpdateCompleted != null)
				UpdateCompleted(this, EventArgs.Empty);
		}			
			
		float RepeatAsyncUpdateWidth = 0;
		static int count = 0;

		public void OnUpdateBreakWidthAsync(float breakWidth)
		{	
			if (Math.Abs (RepeatAsyncUpdateWidth - breakWidth) < 2)
				return;			

			RepeatAsyncUpdateWidth = breakWidth;

			if (AsyncScalingWordWrapResult != null && !(AsyncScalingWordWrapResult.IsCompleted || AsyncScalingWordWrapResult.CompletedSynchronously)) {
				if (TokenScalingWordWrapSource != null && !TokenScalingWordWrapSource.IsCancellationRequested)
					TokenScalingWordWrapSource.Cancel ();				
			}				

			this.LogDebug ("Word-Break Job #{0} started.", count++);

			try {				
				TokenScalingWordWrapSource = new CancellationTokenSource ();
				AsyncScalingWordWrapResult = Task.Factory.StartNew (() => OnUpdate(0, RepeatAsyncUpdateWidth), 
					TokenScalingWordWrapSource.Token)
					.ContinueWith((t) => {																
						if (!t.IsCanceled)
							OnUpdateCompleted();
						AsyncScalingWordWrapResult = null;
					});
			} catch (Exception ex) {
				ex.LogError ();
			}
		}
			
		public void OnRefreshGlyphsAsync(IGUIFont font, SpecialCharacterFlags flags, int lineHeight, float breakWidth)
		{
			if (AsyncScalingWordWrapResult != null && !(AsyncScalingWordWrapResult.IsCompleted || AsyncScalingWordWrapResult.CompletedSynchronously)) {
				if (TokenScalingWordWrapSource != null && !TokenScalingWordWrapSource.IsCancellationRequested)
					TokenScalingWordWrapSource.Cancel ();				
			}

			LineHeight = lineHeight;
			RepeatAsyncUpdateWidth = breakWidth;

			this.LogDebug ("Word-Break Job #{0} started (scaling).", count++);

			try {				
				TokenScalingWordWrapSource = new CancellationTokenSource ();
				AsyncScalingWordWrapResult = Task.Factory.StartNew (() => this.ForEach(p => p.RefreshGlyphs(font, flags)), 
					TokenScalingWordWrapSource.Token)
					.ContinueWith((t) => {						
						if (!t.IsCanceled)
							OnUpdate(0, RepeatAsyncUpdateWidth, true);
					})
					.ContinueWith((t) => {												
						if (!t.IsCanceled)
							OnUpdateCompleted();
						AsyncScalingWordWrapResult = null;
					});
			} catch (Exception ex) {
				ex.LogError ();
			}
		}

		public void OnUpdateAsync(int startIndex, float breakWidth)
		{			
			if (AsyncResult != null && !(AsyncResult.IsCompleted || AsyncResult.CompletedSynchronously)) {								
			}				

			try {				
				TokenSource = new CancellationTokenSource ();
				AsyncResult = Task.Factory.StartNew (() => OnUpdate (startIndex, breakWidth), TokenSource.Token)					
					.ContinueWith((t) => {							
						if (!t.IsCanceled)
							OnUpdateCompleted();
						AsyncResult = null;	
					});
			} catch (Exception ex) {
				ex.LogError ();
			}
		}

		public void OnUpdate(int startIndex = 0)
		{
			OnUpdate (startIndex, BreakWidth);
		}

		public void OnUpdate(int startIndex, float breakwidth, bool forceAll = false, int maxRows = 0)
		{	
			//this.RWLock.EnterWriteLock ();
			try {				
				bool doWordWrap = breakwidth != BreakWidth || forceAll;
				BreakWidth = breakwidth;

				float top = 0;
				int lineCount = 0;
				int positionOffset = 0;
				float width = 0;

				startIndex = Math.Max(0, startIndex - 2);
				if (startIndex >= Count)
					return;

				if (startIndex > 0) {
					Paragraph p = this [startIndex - 1];
					top = p.Bottom;
					lineCount = p.LineOffset + p.LineCount;
					positionOffset = p.PositionOffset + p.Length;
				}

				bool fastExitFlag = false;
				for (int i = startIndex; i < Count; i++) {
					Paragraph p = this [i];

					// condition for fast exit..
					if (!forceAll && !doWordWrap && !p.NeedsWordWrap && i > startIndex + 5 && p.Index == i && p.Top == top && p.PositionOffset == positionOffset) {
						fastExitFlag = true;
						break;
					}

					p.Index = i;
					p.Top = top;
					p.LineOffset = lineCount;
					p.PositionOffset = positionOffset;

					p.NeedsWordWrap |= doWordWrap;
					if (p.NeedsWordWrap)
						p.WordWrap (BreakWidth);

					int lc = p.LineCount;
					p.Height = lc * LineHeight;
					width = Math.Max(width, p.Width);
					// prepare next
					top += p.Height;
					lineCount += lc;
					positionOffset += p.Length;

					if (maxRows > 0 && i - startIndex > maxRows) {
						refreshLastIndex = i;
						RequiresFullRefresh = true;
						fastExitFlag = true;
						break;
					}
				}					

				if (fastExitFlag) {
					//LineCount = lineCount;	
					Width = Math.Max(Width, width);
				} else {
					LineCount = lineCount;
					if (startIndex == 0)
						Width = width;
					Length = positionOffset;
				}					

				//if (UpdateCompleted != null)
				//	UpdateCompleted(this, EventArgs.Empty);
			} catch (Exception ex) {
				ex.LogError ();
			}
			finally {
				//this.RWLock.ExitWriteLock ();
			}
		}
	}


	public class MultiLineTextManager : DisposableObject
	{				
		public event EventHandler<EventArgs> LoadingCompleted;

		// *** Options ***

		// *** Configuration ***

		public IGUIFont Font { get; private set; }
		public bool Active { get; set; }

		// *** Data ***

		private ParagraphList m_Paragraphs;
		public ParagraphList Paragraphs 
		{ 
			get {
				return m_Paragraphs;
			}
		}

		// *** Metrics ***

		public int LineHeight { get; private set; }
		public int LineCount 
		{ 
			get {
				return Paragraphs.LineCount;
			}
		} 
			
		//public int CurrentLineIndex { get; private set; }

		// Offsets
		public float BreakWidth { get; private set; }

		// Sizes

		public float Height
		{
			get{
				return Paragraphs.Height;
			}
		}

		public float Width
		{
			get{
				return Paragraphs.Width;
			}
		}

		// Bounds

		public RectangleF LineBounds(int lineIndex)
		{
			float top = (lineIndex * LineHeight);
			return new RectangleF (0, top, 1, LineHeight);
		}

		// *** Positioning ***

		public int CurrentParagraphIndex { get; private set; }
		public int CursorPosition { get; private set; }

		public int AbsCursorPosition
		{
			get{
				return CursorPosition + CurrentParagraph.PositionOffset;
			}
		}

		public Paragraph CurrentParagraph 
		{ 
			get {
				if (Paragraphs == null || Paragraphs.Count == 0)
					Paragraphs.AddLast (new Paragraph(0, this.BreakWidth));
				CurrentParagraphIndex = CurrentParagraphIndex.Clamp (0, Paragraphs.Count - 1);
				return Paragraphs [CurrentParagraphIndex];				
			}
		}
			
		public bool IsStartOfLine
		{
			get{
				return CursorPosition == 0;
			}
		}

		public bool IsEndOfLine
		{
			get{
				Paragraph p = CurrentParagraph;
				return p == null || CursorPosition == p.Length;
			}
		}

		public bool IsLastParagraph
		{
			get{
				Paragraph para = CurrentParagraph;
				return para == null || CurrentParagraphIndex == Paragraphs.Count - 1;
			}
		}

		public bool IsStartOfText
		{
			get{
				return CurrentParagraphIndex == 0 && CursorPosition == 0;
			}
		}

		public bool IsEndOfText
		{
			get{
				return IsLastParagraph && IsEndOfLine;
			}
		}			

		public bool IsEmpty
		{
			get{
				return IsStartOfText && IsEndOfText;
			}
		}			

		public int FindParagraphIndexOnScreen(float y)
		{
			if (y < 3 || Paragraphs.Count < 2)
				return 0;

			Paragraphs.RWLock.EnterReadLock ();
			try {
				int a = 0;
				int b = Paragraphs.Count;
				while (true)
				{
					if (a == b)
						return (a - 1).Clamp(0, Paragraphs.Count - 1);

					int mid = a + ((b - a) / 2);                    
					switch (Paragraphs[mid].Top.CompareTo(y))
					{
					case -1:
						a = mid + 1;
						break;

					case 1:
						b = mid;
						break;

					case 0:						
						return (mid).Clamp(0, Paragraphs.Count - 1);
					}
				}
			} catch (Exception ex) {
				ex.LogError ("FindParagraphIndexOnScreen");
				return 0;
			}
			finally {
				Paragraphs.RWLock.ExitReadLock ();
			}				
		}

		public int FindParagraphIndexByPosition(int position)
		{
			if (position < 1 || Paragraphs.Count < 2)
				return 0;

			Paragraphs.RWLock.EnterReadLock ();
			try {
				int a = 0;
				int b = Paragraphs.Count;
				while (true)
				{
					if (a == b)
						return (a - 1).Clamp(0, Paragraphs.Count - 1);

					int mid = a + ((b - a) / 2);                    
					switch (Paragraphs[mid].PositionOffset.CompareTo(position))
					{
					case -1:
						a = mid + 1;
						break;

					case 1:
						b = mid;
						break;

					case 0:						
						return (mid).Clamp(0, Paragraphs.Count - 1);
					}
				}
			} catch (Exception ex) {
				ex.LogError ("FindParagraphIndexForAbsolutePosition");
				return 0;
			}
			finally {
				Paragraphs.RWLock.ExitReadLock ();
			}				
		}
			
		public RectangleF CalcCursorRectangle()
		{						
			Paragraph para = CurrentParagraph;
			int x = CursorPosition;
			ParagraphPosition pos = para.PositionAtIndex (x);
			float top = para.LineOffset * LineHeight;
			float y = top + (pos.LineIndex * LineHeight);
			return new RectangleF (pos.ColumnStart, y, pos.ColumnWidth, LineHeight);
		}
			
		public void SetCursorPosition(float x, float y)
		{				
			int line = 0;
			CurrentParagraphIndex = FindParagraphIndexOnScreen (y);
			Paragraph para = CurrentParagraph;
			if (para.LineCount > 0)				
				line = (int)((y - para.Top - 1) / LineHeight);
			ParagraphPosition pos = para.PositionAtLineWidth (x, line);
			CursorPosition = pos.Position;
			ResetCursorColoumns ();
		}			

		public void LogPosition()
		{
			//Console.WriteLine ("ParagraphIndex: {0} CursorPosition: {1}", CurrentParagraphIndex, CursorPosition);
			//Console.WriteLine ("Line: {0}, CursorPosition: {1}", CurrentParagraph.LineFromPosition(CursorPosition), CursorPosition);
		}

		public int FirstParagraphOnScreen { get; private set; }
		public void OnLayout(float y)
		{			
			FirstParagraphOnScreen = FindParagraphIndexOnScreen(y);
			LogPosition ();
		}

		public void OnResize(float breakWidth)
		{
			//this.LogDebug ("OnResize: {0}", breakWidth);
			BreakWidth = breakWidth;
			Paragraphs.OnUpdateBreakWidthAsync (breakWidth);
		}
			
		public void InsertChar(char c)
		{			
			switch (c) {
			case '\r':
				break;
			case '\n':
				InsertLineBreak ();
				break;
			default:
				int startParaIndex = CurrentParagraphIndex;
				Paragraph para = CurrentParagraph;
				if (para.InsertChar (CursorPosition++, c, Font, Flags)) {
					//MoveNextChar ();
					//para.WordWrap (BreakWidth);
					Paragraphs.OnUpdate (startParaIndex, BreakWidth, false, 250);
					ResetCursorColoumns ();
				}
				break;
			}
		}

		public void InsertLineBreak()
		{			
			if (CursorPosition >= CurrentParagraph.Length - 1) {
				if (CurrentParagraphIndex >= Paragraphs.Count - 1) {
					// most common case: Writing at the edge
					Paragraphs.AddLast (new Paragraph (Paragraphs.Count, BreakWidth, String.Empty, Font, Flags));
					MoveNextChar ();
				} else {					
					MoveNextChar ();
					Paragraph current = CurrentParagraph;
					Paragraph para = new Paragraph (CurrentParagraphIndex, BreakWidth, "\n", Font, Flags);
					para.LineOffset = current.LineOffset;
					para.Height = LineHeight;
					Paragraphs.Insert (CurrentParagraphIndex, para);
					//current = CurrentParagraph;
				}
			} else if (CursorPosition == 0) {				
				Paragraph current = CurrentParagraph;
				Paragraph para = new Paragraph (CurrentParagraphIndex, BreakWidth, "\n", Font, Flags);
				para.LineOffset = current.LineOffset;
				para.Height = LineHeight;
				para.Top = current.Top;
				Paragraphs.Insert (CurrentParagraphIndex, para);
				CurrentParagraphIndex++;
			} else {
				Paragraph para = new Paragraph(CurrentParagraphIndex + 1, BreakWidth);
				Paragraph cp = CurrentParagraph;
				GlyphList.Node np = cp.Glyphs.Head;
				for (int i = 0; i < CursorPosition - 1; i++)
					np = np.Next;			
				GlyphList.Node npTail = np;

				np = np.Next;
				while (np != null) {
					para.Glyphs.AddLast (np.Value);
					np = np.Next;
				}
				para.NeedsWordWrap = true;

				// cut np..
				npTail.Next = null;
				cp.Glyphs.ResetCurrent ();
				cp.AppendChar ('\n', Font, Flags);
				Paragraphs.Insert (++CurrentParagraphIndex, para);
			}
				
			CurrentParagraphIndex = CurrentParagraphIndex.Clamp(0, Paragraphs.Count - 1);
			CursorPosition = 0;
			ResetCursorColoumns ();
			//Paragraphs.OnUpdateAsync (CurrentParagraphIndex - 1, BreakWidth);
			Paragraphs.OnUpdate (CurrentParagraphIndex - 1, BreakWidth, false, 250);
		}
			
		public void InsertRange(string range)
		{
			if (String.IsNullOrEmpty (range))
				return;

			Paragraph para = CurrentParagraph;
			ResetCursorColoumns ();

			//string[] lines = range.SplitLines ();
			string[] lines = range.Split(new char[]{'\n'}, StringSplitOptions.None);
			if (lines.Length == 1) {
				lines[0].ForEach(c => para.InsertChar(CursorPosition++, c, Font, Flags));
				//para.WordWrap (BreakWidth);
				Paragraphs.OnUpdate (CurrentParagraphIndex, BreakWidth);
				ResetCursorColoumns ();
			} else {
				int startParagraphIndex = CurrentParagraphIndex;
				string paraString = para.ToString ();
				string startingText = paraString.StrLeft (CursorPosition);
				string pendingText = paraString.StrMid (CursorPosition + 1);

				//Console.WriteLine ("ParaString: {0}, Len: {1}", paraString, paraString.Length);
				//Console.WriteLine ("StartingText: {0}, Len: {1}", startingText, startingText.Length);
				//Console.WriteLine ("PendingText: {0}, Len: {1}", pendingText, pendingText.Length);

				para.Glyphs.Clear ();
				para.ParseString (startingText + lines[0], Font, Flags);

				for (int i = 1; i < lines.Length; i++) {
					string line = lines [i];

					CurrentParagraphIndex++;
					para = new Paragraph (CurrentParagraphIndex, BreakWidth);
					if (i == lines.Length - 1) {
						para.ParseString (line + pendingText, Font, Flags);
						CursorPosition = line.Length;
					} else {
						para.ParseString (line, Font, Flags);
					}
					Paragraphs.Insert (CurrentParagraphIndex, para);
				}					

				/***
				//if (!String.IsNullOrEmpty(pendingText)) {
				if (pendingText != null && pendingText.Length > 1) {
					CurrentParagraphIndex++;
					para = new Paragraph (CurrentParagraphIndex, BreakWidth);					
					para.ParseString (pendingText, Font, Flags);
					Paragraphs.Insert (CurrentParagraphIndex, para);
					CursorPosition = 0;
				}
				***/

				//CurrentParagraphIndex = (CurrentParagraphIndex++).Clamp(0, Paragraphs.Count - 1);
				//CursorPosition = para.Length - 1;

				Paragraphs.OnUpdate (startParagraphIndex, BreakWidth);
				ResetCursorColoumns ();
			}
		}

		public void DeleteCurrentChar()
		{
			if (CurrentParagraph.Length == 1) {
				// Delete current line
				if (CurrentParagraphIndex < Paragraphs.Count - 1)
					Paragraphs.RemoveAt (CurrentParagraphIndex);				 
			}
			else if (CursorPosition == CurrentParagraph.Length - 1) {
				if (CurrentParagraphIndex < Paragraphs.Count - 1) {
					// merge with next line
					Paragraph cp = CurrentParagraph;
					Paragraph np = Paragraphs [CurrentParagraphIndex + 1];
					cp.Glyphs.RemoveLast ();
					foreach (GlyphChar g in np.Glyphs)
						cp.Glyphs.AddLast (g);
					cp.NeedsWordWrap = true;
					cp.WordWrap (BreakWidth);
					Paragraphs.Remove (np);
				}
			}
			else {
				// remove current character from current position
				CurrentParagraph.RemoveChar (CursorPosition);
			}
				
			//Paragraphs.OnUpdateAsync (CurrentParagraphIndex, BreakWidth);
			Paragraphs.OnUpdate (CurrentParagraphIndex, BreakWidth, false, 250);
			CurrentParagraphIndex = CurrentParagraphIndex.Clamp(0, Paragraphs.Count - 1);
		}

		public void DeletePrevChar()
		{			
			if (CursorPosition == 0 && CurrentParagraphIndex == 0)
				return;
			if (CursorPosition > 0)
				MovePrevChar ();
			else if (CurrentParagraphIndex > 0) {
				CurrentParagraphIndex--;
				CursorPosition = CurrentParagraph.Length - 1;
			}				
			DeleteCurrentChar ();
		}
			
		public void DeleteRange(int start, int len)
		{
			if (len <= 0) {
				this.LogWarning ("DeleteRange: len <= 0, nothing deleted.");
				return;
			}

			ResetCursorColoumns ();

			int end = start + len;
			int startParaIndex = FindParagraphIndexByPosition (start);
			int index = startParaIndex;
			while (len > 0 && index < Paragraphs.Count) {
				Paragraph para = Paragraphs [index];
				if (para.PositionOffset < end) {
					int count;
					if (start - para.PositionOffset > 0 || len < para.Length) {
						count = para.RemoveRange (Math.Max(0, start - para.PositionOffset), len, Font, Flags);
					} else {						
						count = para.Length;
						para.Glyphs.Clear ();
					}
						
					if (count < 0)
						break;
					len -= count;
				}
				if (para.Glyphs.Count == 0)
					Paragraphs.RemoveAt (index);
				else
					index++;
			}
			Paragraphs.OnUpdate (startParaIndex, BreakWidth);
		}

		public string GetCharRange(int start, int len)
		{
			if (len <= 0) {
				//this.LogWarning ("GetCharRange: len <= 0 requested.");
				return String.Empty;
			}

			StringBuilder sb = new StringBuilder (len);
			int end = start + len;
			int startParaIndex = FindParagraphIndexByPosition (start);
			int index = startParaIndex;
			while (len > 0 && index < Paragraphs.Count) {
				Paragraph para = Paragraphs [index];
				if (para.PositionOffset < end) {
					int count = 0;
					if (start - para.PositionOffset > 0 || len < para.Length) {
						para.ToString ().StrMid (Math.Max(0, start - para.PositionOffset) + 1, len).Do (s => {
							count = s.Length;
							sb.Append (s);	
						});
					} else {						
						para.ToString (sb);
						count = para.Length;
					}

					if (count < 0)
						break;
					len -= count;
				}
				index++;
			}

			return sb.ToString ();
		}

		public void SetCursorAbsPosition(int pos)
		{
			int idx = FindParagraphIndexByPosition (pos);
			Paragraph para = Paragraphs [idx];
			if (pos >= para.PositionOffset) {
				CurrentParagraphIndex = idx;
				CursorPosition = pos - para.PositionOffset;
			}
			ResetCursorColoumns ();
		}

		public SpecialCharacterFlags Flags { get; set; }
		public MultiLineTextBox Owner { get; private set; }

		public MultiLineTextManager(MultiLineTextBox owner, SpecialCharacterFlags flags)
		{
			Owner = owner;
			Font = Owner.Font;
			Flags = flags;
			LineHeight = (int)Font.LineHeight;

			m_Paragraphs = new ParagraphList (LineHeight, BreakWidth);

			Paragraph para = new Paragraph (0, BreakWidth, String.Empty, Font, Flags);
			Paragraphs.AddLast (para);
		}

		public override string ToString ()
		{
			StringBuilder sb = new StringBuilder ();
			for (int i = 0; i < Paragraphs.Count; i++) {
				//sb.Append (Paragraphs[i].ToString());
				Paragraphs[i].ToString(sb);
			}
			return sb.ToString ();
		}

        public int Length
        {
            get
            {
                return Paragraphs.Sum(p => p.Length);
            }
        }

        public bool HasText
        {
            get
            {
                return !IsEmpty;
                //return Paragraphs.Any(p => p.Length > 1);
            }
        }

		public void OnScalingChanged(float breakWidth)
		{
			BreakWidth = breakWidth;
			LineHeight = (int)Font.LineHeight;
			Paragraphs.OnRefreshGlyphsAsync (Font, Flags, LineHeight, BreakWidth);
		}

		public void OnFlagsChanged()
		{
			Paragraphs.OnRefreshGlyphsAsync (Font, Flags, LineHeight, BreakWidth);
		}


		// *** MOVEMENT ***

		// *** Column memory ***
		// Fine editors will remember the user's previously selected column

		public int CurrentColumn { get; private set; }
		public bool CurrentColumnEOL { get; private set; }
		public bool CurrentColumnBOL { get; private set; }

		public void ResetCursorColoumns()
		{
			CurrentColumnBOL = false;
			CurrentColumnEOL = false;
			CurrentColumn = CurrentParagraph.FastPositionAtIndex(CursorPosition);
		}

		private int GetCursorColoumn()
		{
			if (CurrentColumnBOL)
				return 0;
			else if (CurrentColumnEOL)
				return CurrentParagraph.Length - 1;
			else
				return CurrentColumn;
		}

		// >>> Movement

		public virtual void MovePrevParagraph()
		{
			if (CurrentParagraphIndex > 0)
				CurrentParagraphIndex--;
			else
				CurrentParagraphIndex = 0;			
		}			

		public virtual void MoveNextParagraph()
		{
			if (CurrentParagraphIndex < Paragraphs.Count - 1) {
				CurrentParagraphIndex++;
			} else {
				CurrentParagraphIndex = Paragraphs.Count - 1;
			}
		}

		public virtual void MovePageUp(int numLines)
		{
			if (numLines <= 0 || Paragraphs.Count == 0)
				return;

			Paragraph para = CurrentParagraph;
			int line = para.LineFromPosition (CursorPosition);
			if (line >= numLines) {
				CursorPosition = para.PositionAtLineIndex (GetCursorColoumn (), line - numLines);
				return;
			} else {
				numLines -= line;
			}

			if (CurrentParagraphIndex <= 0) {
				CurrentParagraphIndex = 0;
				CursorPosition = 0;
				return;
			}

			while (numLines > 0) {				
				MovePrevParagraph ();
				para = CurrentParagraph;
				if (numLines > para.LineCount)
					numLines -= para.LineCount;
				else {					
					CursorPosition = para.PositionAtLineIndex (GetCursorColoumn (), para.LineCount - numLines);
					return;
				}
			}
		}

		public virtual void MovePageDown(int numLines)
		{
			if (numLines <= 0 || Paragraphs.Count == 0)
				return;

			Paragraph para = CurrentParagraph;
			int line = para.LineFromPosition (CursorPosition);
			if (para.LineCount - line > numLines) {				
				CursorPosition = para.PositionAtLineIndex (GetCursorColoumn (), line + numLines);
				return;
			} else {
				numLines -= para.LineCount - line - 1;
			}
				
			if (CurrentParagraphIndex >= Paragraphs.Count - 1) {
				CurrentParagraphIndex = Paragraphs.Count - 1;
				CursorPosition = CurrentParagraph.Length - 1;
				return;
			}

			while (numLines > 0) {
				MoveNextParagraph ();
				para = CurrentParagraph;
				if (numLines > para.LineCount)
					numLines -= para.LineCount;
				else {					
					CursorPosition = para.PositionAtLineIndex (GetCursorColoumn (), numLines - 1);
					return;
				}
			}
		}			

		public virtual void MovePrevLine()
		{			
			Paragraph para = CurrentParagraph;
			int line = para.LineFromPosition (CursorPosition);
			if (line > 0) {
				//int pos = para.FastPositionAtIndex (CursorPosition);
				//CursorPosition = para.PositionAtLineIndex (pos, line - 1);
				CursorPosition = para.PositionAtLineIndex (GetCursorColoumn(), line - 1);
			} else if (CurrentParagraphIndex > 0) {
				CurrentParagraphIndex--;
				para = CurrentParagraph;
				CursorPosition = para.PositionAtLineIndex (GetCursorColoumn(), para.LineCount - 1);
			}				
		}

		public virtual void MoveNextLine()
		{
			Paragraph para = CurrentParagraph;
			int line = para.LineFromPosition (CursorPosition);
			if (line < para.LineCount -1) {
				CursorPosition = para.PositionAtLineIndex (GetCursorColoumn(), line + 1);
			} else if (CurrentParagraphIndex < Paragraphs.Count -1) {
				CurrentParagraphIndex++;
				para = CurrentParagraph;
				CursorPosition = para.PositionAtLineIndex (GetCursorColoumn(), 0);
			}
		}

		public virtual void MovePrevChar()
		{			
			if (CursorPosition > 0)
				CursorPosition--;
			else if (CurrentParagraphIndex > 0) {				
				MovePrevParagraph();
				CursorPosition = CurrentParagraph.Length - 1;
			}

			ResetCursorColoumns ();
		}

		public virtual void MoveNextChar()
		{
			if (CursorPosition < CurrentParagraph.Length - 1)
				CursorPosition++;
			else if (CurrentParagraphIndex < Paragraphs.Count - 1) {					
				MoveNextParagraph ();
				CursorPosition = 0;
			}

			ResetCursorColoumns ();
		}
			
		public void MoveParagraphHome()
		{			
			ParagraphPosition pt = CurrentParagraph.PositionAtIndex (CursorPosition);
			int line = pt.LineIndex;
			int col = pt.Column;
			if (col <= 0) {
				CursorPosition = 0;
			} else {
				CursorPosition = CurrentParagraph.PositionAtLineIndex (0, line);
			}

			ResetCursorColoumns ();
			CurrentColumnBOL = true;
		}

		public void MoveParagraphEnd()
		{			
			int line = CurrentParagraph.LineFromPosition (CursorPosition);
			int line2 = CurrentParagraph.LineFromPosition (CursorPosition + 1);
			if (line2 > line) {	// we are EOL
				CursorPosition = CurrentParagraph.Length - 1;
			} else {
				CursorPosition = CurrentParagraph.EolIndex (line);
			}

			ResetCursorColoumns ();
			CurrentColumnEOL = true;
		}

		public virtual void MoveHome()
		{
			CursorPosition = 0;
			CurrentParagraphIndex = 0;
			ResetCursorColoumns ();
		}

		public virtual void MoveEnd()
		{
			CurrentParagraphIndex = Paragraphs.Count - 1;
			CursorPosition = CurrentParagraph.Length - 1;
			ResetCursorColoumns ();
		}

		public virtual void MovePrevWord()
		{			
			if (CursorPosition == 0) {
				if (CurrentParagraphIndex > 0) {
					MovePrevChar ();
					while (CurrentParagraph.Length <= 1)
						MovePrevChar ();
					if (CursorPosition > 0)
						CursorPosition--;				
					while (CurrentParagraph.Glyphs [CursorPosition].Char == ' ' && CursorPosition > 0)
						CursorPosition--;
					CursorPosition++;
				}
				ResetCursorColoumns();
				return;
			}

			GlyphList.Node n = CurrentParagraph.Glyphs.Head;
			int i = 0;
			int k = 0;

			bool spaceFlag = false;
			while (k != CursorPosition && n != null) {
				if (n.Value.Char == ' ')
					spaceFlag = true;
				else if (spaceFlag) {
					i = k;
					spaceFlag = false;
				}
					
				n = n.Next;
				k++;
			}

			CursorPosition = i;
			ResetCursorColoumns();
		}

		public void MoveEndOfWord()
		{
			GlyphList.Node n = CurrentParagraph.Glyphs.Head.Skip(CursorPosition);
			while (n != null && !Paragraph.IsSpaceWrapCharacter(n.Value.Char)) {
				n = n.Next;
				CursorPosition++;
			}
		}

		public void MoveNextWord()
		{	
			bool loopFirst = true;
			if (CursorPosition >= CurrentParagraph.Length - 1) {
				if (CurrentParagraphIndex < Paragraphs.Count - 1) {					
					MoveNextChar ();
					while (CurrentParagraph.Length <= 1)
						MoveNextChar ();
					loopFirst = false;
				}
			}

			GlyphList.Node n = CurrentParagraph.Glyphs.Head;
			int k = CursorPosition;

			if (loopFirst) {
				for (int i = 0; i < CursorPosition; i++)
					n = n.Next;
				k = CursorPosition;
				while (n != null && n.Value.Char != ' ') {
					n = n.Next;
					k++;
				}
			}

			while (n != null && n.Value.Char == ' ') {
				n = n.Next;
				k++;
			}
			if (k < CurrentParagraph.Length - 1)
				CursorPosition = k;
			else
				CursorPosition = CurrentParagraph.Length - 1;			

			ResetCursorColoumns();
		}

		public bool IsLastPosition
		{
			get{
				return CurrentParagraphIndex >= 0 && CurrentParagraphIndex == Paragraphs.Count - 1
					&& CursorPosition == CurrentParagraph.Length;
			}
		}

		public static char AutoDetectLineBreakChar(string s, int start = 2, int len = 1024)
		{
			string sub = s.StrMid (start + 1, len);
			if (String.IsNullOrEmpty(sub) || sub.Any (c => c == (char)10) || start + (len * 2) + 1 > s.Length)
				return '\n';
			if (sub.Any (c => c == (char)13))
				return '\r';			
			return AutoDetectLineBreakChar (s, start + len, len);
		}
        		
        public bool GroupParagraphs { get; set; }

		public void LoadTextAsync (string value, float breakWidth)
		{
			BreakWidth = breakWidth;
            if (value == null)
                value = String.Empty;

            Task.Factory.StartNew (() => {
				Stopwatch sw = Stopwatch.StartNew();

				int index = 0;
				Paragraphs.Clear ();
				ParagraphList paragraphs = new ParagraphList(LineHeight, BreakWidth);
				char splitChar = AutoDetectLineBreakChar(value);

				if (!GroupParagraphs) {
                    Strings.Split (value, splitChar).ForEach (line =>
						paragraphs.AddLast (new Paragraph (index++, BreakWidth, line, Font, Flags))
					);
				} else {
					const string splitStr = "\r\n\r\n";
					Strings.Split (value, splitStr).ForEach (line => {
						paragraphs.AddLast (new Paragraph (index++, BreakWidth, line.Replace("\n", " ") + "\n", Font, Flags));
						paragraphs.AddLast (new Paragraph (index++, BreakWidth, "\n", Font, Flags));
					});						
				}					
					
				paragraphs.OnUpdate ();
				Concurrency.LockFreeUpdate(ref m_Paragraphs, paragraphs);

				this.LogVerbose ("{0} characters of text in {1} paragraphs loaded into the editor in {2} ms.", value.Length.ToString("n0"), Paragraphs.Count.ToString("n0"), sw.ElapsedMilliseconds.ToString("n0"));
			}).ContinueWith((t) => {
				if (t.Status == TaskStatus.RanToCompletion) {
					if (Paragraphs.BreakWidth != BreakWidth) {
						Paragraphs.OnUpdateBreakWidthAsync(BreakWidth);
					}				
				}
			}).ContinueWith((t) => {
				Owner.UndoRedoManager.Clear();
				if (LoadingCompleted != null)
					LoadingCompleted(this, EventArgs.Empty);				
			});
		}

		protected override void CleanupUnmanagedResources ()
		{			
			if (Paragraphs != null) {				
				Paragraphs.Clear ();
			}
			Owner = null;
			base.CleanupUnmanagedResources ();
		}
	}
}

