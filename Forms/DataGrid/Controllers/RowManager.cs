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
		RectangleF RowBoundsByRowIndex (int rowIndex);
		RowInfo RowInfoByRowIndex (int rowIndex);
		RowInfo RowInfoFromPoint (float y);

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
			
		public BinarySortedList<float> RowOffsets;

		public RowManager (IController parent, IRowManagerOwner owner)
			: base(parent)
		{			
			Owner = owner;
			if (Owner != null)
				Scrollbar = Owner.VScrollBar;
			m_Observable = new Observable<EventMessage> ();
			RowOffsets = new BinarySortedList<float> ();
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
					else if (m_CurrentRowIndex > m_RowCount - 1)
						m_CurrentRowIndex = m_RowCount - 1;					
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

		public virtual float DocumentSize { 
			get {
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

		void SelectNone()
		{			
			(Parent as IDataProvider).Do (data => data.SelectionManager.Do(sel => sel.SelectNone ()));
		}

		public void MoveFirst()
		{
			if (RowCount <= 0)
				m_CurrentRowIndex = -1;			
			else
				m_CurrentRowIndex = 0;
			SelectNone ();
			EnsureRowindexVisible (m_CurrentRowIndex);
			OnRowsChanged ();
		}			

		public void MovePrevious ()
		{
			if (CanMoveBack) {
				m_CurrentRowIndex--;
				SelectNone ();
				EnsureRowindexVisible (m_CurrentRowIndex);
				OnRowsChanged ();
			}
		}

		public void MoveNext ()
		{
			if (CanMoveForward) {
				m_CurrentRowIndex++;
				SelectNone ();
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
			SelectNone ();
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
				m_CurrentRowIndex = (int)Math.Max (0, m_CurrentRowIndex - PageSize);
				EnsureRowindexVisible (m_CurrentRowIndex);
				OnRowsChanged ();
			}
		}

		public void MovePageDown()
		{
			if (CanMoveForward) {
				m_CurrentRowIndex = (int)Math.Min (RowCount - 1, m_CurrentRowIndex + PageSize);
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

		public bool HasVariableHeightRows { get; private set; }
		public void DetectVariableRowHeights()
		{
			float lastRowBottom = 0;            
			for (int i = 0; i < RowOffsets.Count; i++)
			{
				if (Math.Abs (Math.Abs (RowOffsets [i] - lastRowBottom) - RowHeight) > 0.00001) {
					HasVariableHeightRows = true;
					return;
				}					
				lastRowBottom = RowOffsets[i];
			}
			HasVariableHeightRows = false;
		}

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
			if (!HasVariableHeightRows)
			{
				//return (int)((y - VerticalOffset) / RowHeight + 0.5f);
				return (int)((int)(y - VerticalOffset) / RowHeight);
			}
			else
			{
				if (RowOffsets == null || RowOffsets.Count == 0)
					return -1;

				y -= VerticalOffset;
				return RowOffsets.IndexOfElementOrSuccessor(y);
			}
		}

		public RectangleF RowBoundsByRowIndex(int rowIndex)
		{            
			RowInfo info = RowInfoByRowIndex(rowIndex);
			return new RectangleF(0, info.RowTop + ScrollOffsetY, 500, info.RowHeight);
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
}

