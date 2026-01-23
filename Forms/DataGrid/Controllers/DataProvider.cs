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
	public interface IDataProvider
	{
		IColumnManager ColumnManager { get; }
		IRowManager RowManager { get; }
		ISelectionManager SelectionManager { get; }

		string GetValue (int row, int col);
		int GroupLevel (int row);
		Task ApplySort ();
		void Clear();
		void Remove (int index);
	}

	public interface IDataProviderOwner : IColumnManagerOwner, IRowManagerOwner, ISelectionManagerOwner
	{
		void OnDataLoaded();
		ScrollBars ScrollBars { get; set; }
	}

	public abstract class DataProvider : Controller, IDataProvider
	{		
		public void OnDataLoaded()
		{			
			Owner.OnDataLoaded ();
		}

		public IDataProviderOwner Owner { get; set; }
		public IColumnManager ColumnManager { get; set; }
		public IRowManager RowManager { get; set; }
		public ISelectionManager SelectionManager { get; set; }

		protected DataProvider (IController parent, IDataProviderOwner owner)
			: this (parent, owner, null, null, null) {}

		protected DataProvider (IController parent, IDataProviderOwner owner, IColumnManager columnManager, IRowManager rowManager, ISelectionManager selectionManager)
			: base (parent)
		{			
			Owner = owner;
			ColumnManager = columnManager ?? AddSubController (new ColumnManager(this, owner));
			RowManager = rowManager ?? AddSubController (new RowManager(this, owner));
			SelectionManager = selectionManager ?? AddSubController (new SelectionManager(this, owner));;
		}			

		public abstract void InitializeColumns ();
		public abstract void InitializeRows ();
		public abstract string GetValue (int row, int col);

		public virtual void Clear()
		{
		}

		public virtual void Remove(int index)
		{
		}

		public virtual int GroupLevel (int row)
		{
			return 0;
		}

		public virtual async Task ApplySort()
		{
		}		
	}
}

