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
	public interface ISelectionManager : IObservable<EventMessage>, IDisposable, ISupportsSelection
	{
		ISelectionManagerOwner Owner { get; set; }

		DataGridSelectionModes SelectionMode { get; set; }
		void SelectRow (int rowIndex);
		void UnselectRow (int rowIndex);
		void SelectNone ();
		IEnumerable<int> 	SelectedRowIndices { get; }
		bool IsRowSelected (int index);
		void StartSelectionTimer ();
		int SelectedRowsCount { get; }

		void OnSelectionChanged ();
	}

	public interface ISelectionManagerOwner
	{
		void OnSelectionChanged();
	}

	public static class SelectionManagerMessages
	{		
		public const string SelectionChanged = "SelectionChanged";
	}

	public enum DataGridSelectionModes
	{
		Row,
		Cell
	}
		
	public class SelectionManager : Controller, ISelectionManager
	{
		public ISelectionManagerOwner Owner { get; set; }
		HashSet<int> m_SelectedRowIndices;

		public Observable<EventMessage> m_Observable { get; protected set; }
		public IDisposable Subscribe(IObserver<EventMessage> observer)
		{
			return m_Observable.Subscribe (observer);
		}

		public DataGridSelectionModes SelectionMode { get; set; }

		//DelayedAction daction;

		public SelectionManager (IController parent, ISelectionManagerOwner owner)
			: base(parent)
		{
			Owner = owner;
			m_SelectedRowIndices = new HashSet<int> ();
			m_Observable = new Observable<EventMessage> ();

			//daction = new DelayedAction (250, () =>	OnSelectionChanged ());
		}			

		// Params: None
		public void OnSelectionChanged()
		{								
			Owner.OnSelectionChanged ();
			m_Observable.InvokeSendMessage(new EventMessage(this, SelectionManagerMessages.SelectionChanged));
		}

		public IEnumerable<int> SelectedRowIndices
		{
			get{
				return m_SelectedRowIndices;
			}
		}

		public void SelectNone()
		{
			m_SelectedRowIndices.Clear ();
			OnSelectionChanged ();
		}

		public void SelectRow(int rowIndex)
		{
			if (!m_SelectedRowIndices.Contains (rowIndex))
				m_SelectedRowIndices.Add (rowIndex);
			OnSelectionChanged ();
		}

		public void UnselectRow(int rowIndex)
		{			
			if (m_SelectedRowIndices.Remove (rowIndex))
				OnSelectionChanged ();
		}

		public bool IsRowSelected (int index)
		{
			return m_SelectedRowIndices.Contains (index);
		}

		public int SelectedRowsCount 
		{ 
			get {
				return m_SelectedRowIndices.Count;
			}
		}
			
		public void StartSelectionTimer()
		{
			OnSelectionChanged ();
			//daction.Start ();
		}

		private int RowCount
		{
			get{
				IDataProvider d = Parent as IDataProvider;
				if (d != null && d.RowManager != null)
					return d.RowManager.RowCount;
				return 0;
			}
		}

		public bool CanSelectAll
		{
			get{				
				int rCount = RowCount;
				return rCount > 0 && m_SelectedRowIndices.Count < rCount;
			}
		}

		public bool CanInvertSelection
		{
			get{				
				return m_SelectedRowIndices.Count > 0;
			}
		}

		public void SelectAll()
		{			
			int rowCount = RowCount;
			m_SelectedRowIndices.Clear ();
			for (int i = 0; i < rowCount; i++)
				m_SelectedRowIndices.Add (i);
			OnSelectionChanged ();
		}

		public void InvertSelection()
		{
			int rowCount = RowCount;
			HashSet<int> sel = new HashSet<int> ();
						
			for (int i = 0; i < rowCount; i++) {
				if (!m_SelectedRowIndices.Contains (i))
					sel.Add (i);
			}

			Concurrency.LockFreeUpdate (ref m_SelectedRowIndices, sel);
			OnSelectionChanged ();
		}

		public void UpdateMenus()
		{			
		}
			
		protected override void CleanupManagedResources ()
		{
			//daction.Dispose ();
			m_Observable.Dispose ();
			Owner = null;
			base.CleanupManagedResources ();
		}
	}
}

