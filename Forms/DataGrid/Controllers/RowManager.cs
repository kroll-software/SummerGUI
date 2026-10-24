using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using KS.Foundation;

namespace SummerGUI.DataGrid
{
	public interface IRowManager : IObservable<EventMessage>, IDisposable, IController
	{
		IRowManagerOwner Owner { get; set; }
		float WindowSize { get; }
		float DocumentSize { get; }

		float RowHeight { get; }
		int RowCount { get; set; } 
		int CurrentRowIndex { get; set; }

		float ScrollOffsetY { get; set; }

		void EnsureRowindexVisible (int index);

		int RowIndexFromPoint (float y);
		
		RowInfo RowInfoByRowIndex (int rowIndex);
		RowInfo RowInfoFromPoint (float y);
		RectangleF RowBoundsByRowIndex (int rowIndex);

		// VariableRowHeight: kumulative Zeilen-Unterkanten (1:1 Legacy
		// "ViewModelBase.rowoffsets"). Nur gefüllt/wirksam, wenn
		// HasVariableHeightRows — sonst fällt alles auf die einheitliche
		// RowHeight zurück.
		BinarySortedList<float> RowOffsets { get; }

		// VariableRowHeight (1:1 Legacy "ViewModelBase.HasVariableHeightRows"):
		// wird von der View im RebuildItems/PerformRowUpdate gesetzt →
		// RowIndexFromPoint nimmt dann den Binärsuche-Pfad über RowOffsets.
		bool HasVariableHeightRows { get; set; }

		int FirstRowInWindow { get; }
		int LastRowInWindow { get; }
		float LastRowBottom { get; }

		bool IsRowCurrent (int index);

		void MoveFirst ();
		void MoveNext ();
		void MovePrevious ();
		void MoveLast ();
		void MovePageUp ();
		void MovePageDown ();
		bool CanMoveBack { get; }
		bool CanMoveForward { get; }

		void OnRowsChanged ();
	}

	public interface IRowManagerOwner
	{
		
		VerticalScrollBar VScrollBar { get; }
		RectangleF ScrollBounds { get; }
		float RowHeight { get; }
		float ScrollOffsetY { get; set; }
		void OnRowCountChanged ();
		void OnCurrentRowChanged ();
	}

	public static class RowManagerMessages
	{		
		public const string RowsChanged = "RowsChanged";
	}

	public class RowManager : Controller, IRowManager, IObservable<EventMessage>
	{
		public IRowManagerOwner Owner { get; set; }
		public VerticalScrollBar Scrollbar { get; private set; }

		public Observable<EventMessage> m_Observable { get; protected set; }
		public IDisposable Subscribe(IObserver<EventMessage> observer)
		{
			return m_Observable.Subscribe (observer);
		}
			
		public BinarySortedList<float> RowOffsets { get; } = new BinarySortedList<float> ();

		public RowManager (IController parent, IRowManagerOwner owner)
			: base(parent)
		{			
			Owner = owner;
			if (Owner != null)
				Scrollbar = Owner.VScrollBar;
			m_Observable = new Observable<EventMessage> ();
		}

		// Params: int Current RowIndex, int Total RowCount
		public void OnRowsChanged()
		{				
			if (Owner != null)
				Owner.OnRowCountChanged ();
			m_Observable.InvokeSendMessage(new EventMessage(this, RowManagerMessages.RowsChanged, true, m_CurrentRowIndex, m_RowCount));
		}

		protected int m_CurrentRowIndex = -1;
		public int CurrentRowIndex 
		{ 
			get {
				return m_CurrentRowIndex;
			}
			set {
				if (m_CurrentRowIndex != value) {
					if (value <= 0)
						ScrollOffsetY = 0;
					m_CurrentRowIndex = value;
					OnRowsChanged ();
				}
			}
		}

		private int m_RowCount;
		public int RowCount 
		{ 
			get {
				return m_RowCount;
			}
			set {
				if (m_RowCount != value) {
					m_RowCount = value;
					if (m_RowCount == 0)
						m_CurrentRowIndex = -1;
					else if (m_CurrentRowIndex < 0)
						m_CurrentRowIndex = 0;      // Laden/Aktualisieren mit Daten: erste Zeile markieren (nur wenn noch nichts gesetzt)
					else if (m_CurrentRowIndex > m_RowCount - 1)
						m_CurrentRowIndex = m_RowCount - 1;  // Absichern: Index bleibt gültig
					OnRowsChanged();
				}
			}
		}

		public float WindowSize
		{
			get{
				return Owner.ScrollBounds.Height;
			}
		}

				// 1:1 Legacy (GanttControl vertikale Dokument- / Scroll-Groesse):
		//   if (m_Controller.CurrentView.HasVariableHeightRows && ItemCount > 0)
		//       documentSize = Controller.CurrentView.LastRowBottom;
		//   else
		//       documentSize = (MaxRowIndex + 1) * GanttSettings.RowHeight;
		public virtual float DocumentSize { 
			get {
				if (HasVariableHeightRows && RowCount > 0)
					return LastRowBottom;
				return RowCount * RowHeight;
			}
		}

		public int FirstRowInWindow
		{
			get{
				return RowIndexFromPoint (Owner.ScrollBounds.Top);
			}
		}

		public int LastRowInWindow
		{
			get{
				return Math.Min(RowCount - 1, RowIndexFromPoint (Owner.ScrollBounds.Bottom));
			}
		}

		public void MoveFirst()
		{
			if (RowCount <= 0)
				m_CurrentRowIndex = -1;			
			else
				m_CurrentRowIndex = 0;		
			EnsureRowindexVisible (m_CurrentRowIndex);
			OnRowsChanged ();
		}			

		public void MovePrevious ()
		{
			if (CanMoveBack) {
				m_CurrentRowIndex--;				
				EnsureRowindexVisible (m_CurrentRowIndex);
				OnRowsChanged ();
			}
		}

		public void MoveNext ()
		{
			if (CanMoveForward) {
				m_CurrentRowIndex++;				
				EnsureRowindexVisible (m_CurrentRowIndex);
				OnRowsChanged ();
			}
		}

		public void MoveLast ()
		{
			if (RowCount <= 0)
				m_CurrentRowIndex = -1;
			else
				m_CurrentRowIndex = RowCount - 1;			
			EnsureRowindexVisible (m_CurrentRowIndex);
			OnRowsChanged ();
		}

		public float PageSize
		{
			get{
				if (RowHeight <= 0)
					return 0;
				return WindowSize / RowHeight;
			}
		}

		public void MovePageUp()
		{
			if (CanMoveBack) {
				m_CurrentRowIndex = (int)Math.Max (0, m_CurrentRowIndex - PageSize + 0.5f);
				EnsureRowindexVisible (m_CurrentRowIndex);
				OnRowsChanged ();
			}
		}

		public void MovePageDown()
		{
			if (CanMoveForward) {
				m_CurrentRowIndex = (int)Math.Min (RowCount - 1, m_CurrentRowIndex + PageSize + 0.5f);
				EnsureRowindexVisible (m_CurrentRowIndex);
				OnRowsChanged ();
			}
		}

		public bool CanMoveBack 
		{ 
			get{
				return m_CurrentRowIndex > 0;
			}
		}

		public bool CanMoveForward
		{ 
			get{
				return RowCount > 0 && m_CurrentRowIndex < RowCount - 1;
			}
		}						

		// 1:1 Legacy (ViewModelBase: "protected bool m_HasVariableHeightRows" +
		// "public virtual bool HasVariableHeightRows { get; }"): wird von der View
		// im RebuildItems (PerformRowUpdate) gesetzt:
		//   Rows.HasVariableHeightRows = rowoffsets.HasVariableRowHeights (Owner.RowHeight);
		// Aktiv -> RowIndexFromPoint wechselt auf die Binärsuche ueber RowOffsets
		// (BinarySortedList.IndexOfElementOrSuccessor); RowInfoByRowIndex /
		// LastRowBottom / DocumentSize lesen dann die Offsets.
		public bool HasVariableHeightRows { get; set; }

		public float RowHeight 
		{ 
			get {
				return Owner.RowHeight;
			}
		}

		public int MaxRowIndex
		{
			get{
				return RowCount - 1;
			}
		}

		public float ScrollOffsetY 
		{ 
			get{
				return Owner.ScrollOffsetY;
			}
			set{
				Owner.ScrollOffsetY = value;
			}
		}

		public float VerticalOffset 
		{ 
			get {
				return Owner.ScrollBounds.Top + ScrollOffsetY;
			}
		}

		public virtual void EnsureRowindexVisible (int index)
		{
			if (RowCount <= 0)
				ScrollOffsetY = 0;
			else {
				RowInfo info = RowInfoByRowIndex (index);
				EnsureVerticalVisible (info);
			}
		}
			
		public void EnsureVerticalVisible(RowInfo r)
		{                        
			if (r.RowTop < -ScrollOffsetY || r.RowHeight > WindowSize)
			{
				ScrollOffsetY = -r.RowTop;
			}
			else if (r.RowBottom > -ScrollOffsetY + WindowSize)
			{
				ScrollOffsetY = -(r.RowTop - (WindowSize - r.RowHeight));
			}
		}

		public int RowIndexFromPoint(float y)
		{
			if (HasVariableHeightRows)
			{
				if (RowOffsets == null || RowOffsets.Count == 0)
					return -1;
				
				return RowOffsets.IndexOfElementOrSuccessor(y - VerticalOffset);
			}
			else
			{
				return (int)((y - VerticalOffset) / RowHeight);				
			}
		}		

		public RowInfo RowInfoByRowIndex(int rowIndex)
		{
			if (HasVariableHeightRows) {
				if (RowOffsets.IsNullOrEmpty () || rowIndex < 0 || rowIndex >= RowOffsets.Count)
					return RowInfo.Empty;

				if (rowIndex == 0)
					return new RowInfo (rowIndex, 0, RowOffsets [rowIndex]);
				else
					return new RowInfo (rowIndex, RowOffsets [rowIndex - 1], RowOffsets [rowIndex]);
			} else {
				return new RowInfo (rowIndex, rowIndex * RowHeight, (rowIndex + 1) * RowHeight);	
			}
		}

		public RowInfo RowInfoFromPoint(float y)
		{            
			return RowInfoByRowIndex(RowIndexFromPoint(y));
		}

		// 1:1 Legacy (ViewModelBase.RowBoundsByRowIndex, float-Variante):
		//   RowInfo info = RowInfoByRowIndex(rowIndex);
		//   return new Rectangle(gantt.ClientRectangle.Left, info.RowTop + gantt.VerticalOffset,
		//                        gantt.ClientRectangle.Width, info.RowHeight);
		public RectangleF RowBoundsByRowIndex (int rowIndex)
		{
			RowInfo info = RowInfoByRowIndex (rowIndex);
			return new RectangleF (Owner.ScrollBounds.Left, info.RowTop + VerticalOffset, Owner.ScrollBounds.Width, info.RowHeight);
		}

		public float LastRowBottom
		{
			get{
				if (HasVariableHeightRows) {
					if (RowOffsets.IsNullOrEmpty ())
						return 0;
					return RowOffsets.Last;
				} else
					return RowCount * RowHeight;
			}
		}			

		public bool IsRowCurrent (int index)
		{
			return index == m_CurrentRowIndex && index >= 0;
		}

		protected override void CleanupManagedResources ()
		{
			m_Observable.Dispose ();
			Scrollbar = null;
			Owner = null;
			base.CleanupManagedResources ();
		}
	}

/// <summary>
/// 1:1 Legacy-Extensions (KS.Gantt/Controller/ViewModels/RowInfo.cs, dort statische Klasse
/// GanttViewModelExtensions; hier an die 2026er-BinarySortedList&lt;float&gt; angepasst).
/// </summary>
	public static class RowManagerExtensions
	{
		// Legacy 1:1:
		//   public static bool HasVariableRowHeights (this BinarySortedList&lt;int&gt; rowoffsets, int defaultRowHeight)
		//   {
		//       int lastRowBottom = 0;
		//       for (int i = 0; i < rowoffsets.Count; i++) {
		//           if (rowoffsets[i] - lastRowBottom != defaultRowHeight)
		//               return true;
		//           lastRowBottom = rowoffsets[i];
		//       }
		//       return false;
		//   }
		public static bool HasVariableRowHeights (this BinarySortedList<float> rowoffsets, float defaultRowHeight)
		{
			if (rowoffsets == null || rowoffsets.Count == 0)
				return false;

			float lastRowBottom = 0;
			for (int i = 0; i < rowoffsets.Count; i++) {
				if (Math.Abs (rowoffsets [i] - lastRowBottom - defaultRowHeight) > 0.00001)
					return true;

				lastRowBottom = rowoffsets[i];
			}

			return false;
		}
	}
}
