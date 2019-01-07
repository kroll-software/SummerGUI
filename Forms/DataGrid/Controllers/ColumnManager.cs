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
	public interface IColumnManager : IObservable<EventMessage>, IDisposable
	{
		IColumnManagerOwner Owner { get; set; }
		DataGridColumnCollection Columns { get; }
		void InitializeColumns ();
		int CurrentColumnIndex { get; set; }
		int ColumnIndexFromPoint (float x);
		DataGridColumn ColumnFromPoint (float x);
		DataGridColumn ColumnFromColumnIndex (int index);
		void OnColumnsChanged ();
	}

	public interface IColumnManagerOwner
	{		
		HorizontalScrollBar HScrollBar { get; } 
		RectangleF ScrollBounds { get; }
		float ScrollOffsetX { get; set; }
		void OnColumnsChanged ();
	}

	public static class ColumnManagerMessages
	{				
		public const string ColumnsChanged = "ColumnsChanged";
		public const string CurrentColumnChanged = "CurrentColumnChanged";
	}

	public class ColumnManager : Controller, IColumnManager, IObservable<EventMessage>
	{
		public IColumnManagerOwner Owner { get; set; }
		public HorizontalScrollBar Scrollbar { get; set; }

		public DataGridColumnCollection Columns { get; set; }

		public Observable<EventMessage> m_Observable { get; protected set; }
		public IDisposable Subscribe(IObserver<EventMessage> observer)
		{
			return m_Observable.Subscribe (observer);
		}

		public ColumnManager (IController parent, IColumnManagerOwner owner)
			:base(parent)
		{
			Owner = owner;

			if (Owner != null)
				Scrollbar = Owner.HScrollBar;
			Columns = new DataGridColumnCollection ();
			m_Observable = new Observable<EventMessage> ();
		}

		public void OnColumnsChanged ()
		{			
			ResetCachedData ();		// Important to call whenever columns changed !
			if (Owner != null)
				Owner.OnColumnsChanged();
			m_Observable.InvokeSendMessage (new EventMessage(this, ColumnManagerMessages.ColumnsChanged));
		}

		public void OnCurrentColumnChanged ()
		{
			m_Observable.InvokeSendMessage (new EventMessage(this, ColumnManagerMessages.CurrentColumnChanged));
		}

		public virtual void InitializeColumns ()
		{
			ResetCachedData ();
		}

		protected DataGridColumn[] m_SortedVisibleColumns;
		DataGridColumn[] SortedVisibleColumns
		{
			get{
				return m_SortedVisibleColumns;
			}
		}

		protected BinarySortedList<float> m_ColumnOffsets;
		protected BinarySortedList<float> ColumnOffsets
		{
			get{
				return m_ColumnOffsets;
			}
		}

		void ResetCachedData()
		{
			Concurrency.LockFreeUpdate(ref m_SortedVisibleColumns, Columns.SortedVisibleColumns.ToArray ());

			var lst = new BinarySortedList<float> ();
			float offset = 0;
			Columns.Where (col => col.Visible).ForEach (col => {
				lst.AddLast(offset);
				offset += col.Width;
			});
			Concurrency.LockFreeUpdate(ref m_ColumnOffsets, lst);
		}


		protected int m_CurrentColumnIndex;
		public int CurrentColumnIndex
		{ 
			get {
				return m_CurrentColumnIndex;
			}
			set {
				if (m_CurrentColumnIndex != value) {
					if (value <= 0)
						ScrollOffsetX = 0;
					m_CurrentColumnIndex = value;
					OnCurrentColumnChanged ();
				}
			}
		}

		private int m_ColumnCount;
		public int ColumnCount 
		{ 
			get {
				return m_ColumnCount;
			}
			set {
				if (m_ColumnCount != value) {
					m_ColumnCount = value;
					if (m_ColumnCount == 0)
						m_CurrentColumnIndex = -1;
					else if (m_CurrentColumnIndex > m_ColumnCount - 1)
						m_CurrentColumnIndex = m_ColumnCount - 1;
					OnColumnsChanged();
				}
			}
		}

		public float ScrollOffsetX
		{ 
			get{
				return Owner.ScrollOffsetX;
			}
			set{
				Owner.ScrollOffsetX = value;
			}
		}

		public virtual int ColumnIndexFromPoint(float x)
		{		
			try {
				return ColumnOffsets.IndexOfElementOrPredecessor (x);
			} catch (Exception ex) {
				ex.LogError ();
				return -1;
			}	
		}

		public virtual DataGridColumn ColumnFromPoint(float x)
		{
			try {				
				return ColumnFromColumnIndex(ColumnOffsets.IndexOfElementOrPredecessor (x));
			} catch (Exception ex) {
				ex.LogError ();
				return null;
			}
		}

		public virtual DataGridColumn ColumnFromColumnIndex(int index)
		{
			try {
				return SortedVisibleColumns [Math.Max(0, Math.Min(SortedVisibleColumns.Length - 1, index))];	
			} catch (Exception ex) {
				ex.LogError ();
				return null;
			}
		}

		protected override void CleanupManagedResources ()
		{
			m_Observable.Dispose ();
			Scrollbar = null;
			Owner = null;
			Columns.Clear ();
			base.CleanupManagedResources ();
		}
	}
}

