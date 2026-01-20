using System;
using System.Text;
using System.Diagnostics;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KS.Foundation;
using GenericUndoRedo;

namespace SummerGUI.Editor
{
	public interface ITextBox
	{
		void DeleteRange (int start, int len);
		void SetCursorPosition (int pos);
		void InsertRange (string text);
		int SelStart { get; set; }
		int SelLength { get; set; }
		PointF ScrollOffset { get; set; }
	}

	public abstract class UndoRedoMementoBase
	{
		public PointF ScrollOffset { get; set; }
		public int SelStart { get; set; }
		public int SelLength { get; set; }
		public string SelectedText { get; set; }
		public int Position { get; set; }
		public string Data { get; set; }

		protected UndoRedoMementoBase ()
		{
		}

		public int DataLength
		{
			get{
				if (Data == null)
					return 0;				
				return Data.Length;
			}
		}

		public int SelectedTextLength
		{
			get{
				if (SelectedText == null)
					return 0;
				return SelectedText.Length;
			}
		}

		public virtual void PerformUndo(ITextBox textbox)
		{
			textbox.ScrollOffset = ScrollOffset;
		}
		public virtual void PerformRedo(ITextBox textbox)
		{
			textbox.ScrollOffset = ScrollOffset;
		}
	}		

	public class UndoRedoInsertMemento : UndoRedoMementoBase
	{
		public UndoRedoInsertMemento()
		{
		}

		public override void PerformUndo (ITextBox textbox)
		{
            textbox.DeleteRange(SelStart, DataLength);

            if (SelLength > 0)
            {
                textbox.SetCursorPosition(SelStart);
                textbox.InsertRange(SelectedText);
                textbox.SelStart = SelStart;
                textbox.SelLength = SelLength;
            }
            else
            {
                textbox.SetCursorPosition(Position);
                textbox.SelStart = Position;
                textbox.SelLength = 0;
            }

            base.PerformUndo (textbox);
		}

		public override void PerformRedo(ITextBox textbox)
		{
			// 1. Falls etwas markiert war, muss es zuerst weg
			if (SelLength > 0)
			{
				textbox.DeleteRange(SelStart, SelLength);
			}

			// 2. Neue Daten einfügen
			// Wir nutzen hier SelStart als stabilen Anker, falls Position 
			// durch das Löschen ungültig geworden wäre.
			textbox.SetCursorPosition(SelStart); 
			textbox.InsertRange(Data);

			// 3. Cursor hinter den eingefügten Text setzen
			int newPos = SelStart + DataLength;
			textbox.SetCursorPosition(newPos);
			textbox.SelStart = newPos;
			textbox.SelLength = 0;

			base.PerformRedo(textbox);
		}		
	}

	public class UndoRedoDeleteMemento : UndoRedoMementoBase
	{
		public UndoRedoDeleteMemento()
		{
		}

		public override void PerformUndo (ITextBox textbox)
		{	
			if (SelLength > 0) {
				textbox.SetCursorPosition (SelStart);				
				textbox.InsertRange (SelectedText);
			} else {
				textbox.SetCursorPosition (Position);
				textbox.InsertRange(Data);
			}

			textbox.SelStart = SelStart;
			textbox.SelLength = SelLength;			
			textbox.SetCursorPosition (Position);

			base.PerformUndo (textbox);
		}

		public override void PerformRedo (ITextBox textbox)
		{	
			if (SelLength > 0) {				
				textbox.DeleteRange(SelStart, SelLength);
				textbox.SetCursorPosition (SelStart);
			} else {
				textbox.DeleteRange(Position, DataLength);
				textbox.SetCursorPosition (Position);
			}

			textbox.SelStart = SelStart;
			textbox.SelLength = 0;

			base.PerformRedo (textbox);
		}
	}

	public class UndoRedoBackspaceMemento : UndoRedoMementoBase
	{
		public UndoRedoBackspaceMemento()
		{
		}

		public override void PerformUndo (ITextBox textbox)
		{	
			if (SelLength > 0) {
				textbox.SetCursorPosition (SelStart);
				textbox.InsertRange (SelectedText);
			} else {
				textbox.SetCursorPosition (Position);
				textbox.InsertRange(Data);
			}

			textbox.SelStart = SelStart;
			textbox.SelLength = SelLength;
			textbox.SetCursorPosition (Position + 1);

			base.PerformUndo (textbox);
		}

		public override void PerformRedo (ITextBox textbox)
		{	
			if (SelLength > 0) {				
				textbox.DeleteRange(SelStart, SelLength);
				textbox.SetCursorPosition (SelStart);
			} else {
				textbox.DeleteRange(Position, DataLength);
				textbox.SetCursorPosition (Position);
			}

			textbox.SelStart = SelStart;
			textbox.SelLength = 0;

			base.PerformRedo (textbox);
		}
	}

	public class UndoRedoStack : DisposableObject
	{		
		readonly RoundStack<UndoRedoMementoBase> UndoStack;
		readonly RoundStack<UndoRedoMementoBase> RedoStack;

		public ITextBox Owner { get; private set; }
		public int MaxUndo { get; set; }

		public UndoRedoStack(ITextBox owner, int maxUndo)
		{			
			Owner = owner;
			MaxUndo = maxUndo;
			UndoStack = new RoundStack<UndoRedoMementoBase> (MaxUndo);
			RedoStack = new RoundStack<UndoRedoMementoBase> (MaxUndo);
		}			

		public void Do (UndoRedoMementoBase action)
		{
			lock (SyncObject) {
				RedoStack.Clear ();
				UndoStack.Push (action);
			}
		}

		public void Undo ()
		{			
			lock (SyncObject) {
				if (CanUndo) {
					UndoRedoMementoBase action = UndoStack.Pop ();
					RedoStack.Push (action);
					action.PerformUndo (Owner);
				}
			}
		}

		public void Redo ()
		{
			lock (SyncObject) {
				if (CanRedo) {
					UndoRedoMementoBase action = RedoStack.Pop ();
					UndoStack.Push (action);
					action.PerformRedo (Owner);
				}
			}
		}

		public bool CanUndo
		{
			get{
				// these calls come from another thread.
				// To avoid any deadlocks, we simply return false, 
				// because it really doesn't matter so much..
				// but it's very important to lock the main Undo/Redo actions.

				if (!Monitor.TryEnter (SyncObject, 100))
					return false;
				try {
					return UndoStack.Count > 0;	
				} catch (Exception ex) {
					ex.LogWarning ();
					return false;
				}
				finally {
					Monitor.Exit (SyncObject);
				}
			}
		}

		public bool CanRedo
		{
			get{
				if (!Monitor.TryEnter (SyncObject, 100))
					return false;
				try {
					return RedoStack.Count > 0;	
				} catch (Exception ex) {
					ex.LogWarning ();
					return false;
				}
				finally {
					Monitor.Exit (SyncObject);
				}
			}
		}

		public void Clear()
		{
			lock (SyncObject) {
				UndoStack.Clear ();
				RedoStack.Clear ();
			}
		}

		protected override void CleanupManagedResources ()
		{
			Owner = null;
			UndoStack.Clear ();
			RedoStack.Clear ();
			base.CleanupManagedResources ();
		}
	}
}

