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

		object GetValue (int row, string key);

		string GetFormattedValue (int row, string key);
		int GroupLevel (int row);
		void ApplySort ();
		void Clear();
		void Remove (int index);
	}

	/// <summary>
	/// Optionale Ergänzung zu <see cref="IDataProvider"/> für Cell-Editing (Phase 1).
	/// <see cref="IDataProvider"/> bleibt unverändert; Implementer implementieren beide Interfaces
	/// parallel (Konzeption §2.11/§2.12: Daten-Lücke).
	/// </summary>
	public interface IEditableDataProvider : IDataProvider
	{
			/// <summary>
			/// Setzt einen Zellwert. Fallback false, wenn der Provider die Spalte/Key nicht kennt.</summary>
			bool TrySetValue (int row, string key, object value);
		}

		/// <summary>
		/// Optionale Ergänzung zu <see cref="IEditableDataProvider"/> für das DataGridView-Feature
		/// <c>AllowNew</c> (WinForms-„AllowUserToAddRows"-Verhalten): der Provider übernimmt das
		/// Anlegen eines neuen Datensatzes aus dem ersten Zellwert der leeren Letztzeile.
		/// Das Grid bietet die Letztzeile an, sobald <c>AllowNew=true</c> UND der Provider
		/// dieses Interface implementiert.
		/// </summary>
		public interface INewRowProvider
		{
			/// <summary>
			/// Legt einen neuen Datensatz aus dem ersten Wert (Key der editierten Spalte) an.
			/// Vertrag: nach Erfolg <see cref="IRowManager.RowCount"/> erhöhen
			/// (.RowsChanged wird dann automatisch an das Grid gemeldet).
			/// true = Erfolg; false = abgelehnt (z.B. Key unbekannt, Wert ungültig).</summary>
			bool TryAddNew (string key, object value);
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

		public abstract object GetValue (int row, string key);

		public virtual string GetFormattedValue (int row, string key)
		{
			return GetValue(row, key).SafeString();
		}

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

		public virtual void ApplySort()
		{
		}		
	}
}

