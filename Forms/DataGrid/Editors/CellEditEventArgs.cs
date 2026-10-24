using System;
using SummerGUI;

namespace SummerGUI.DataGrid.Editors
{
	/// <summary>
	/// Eventdaten der Grid-Edit-Events (CellEdit / CellValidating / CellEdited).</summary>
	public class CellEditEventArgs : EventArgs
	{
		public CellInfo Cell { get; private set; }
		public DataGridColumn Column { get; private set; }
		public object Value { get; set; }
		public bool Cancel { get; set; }

		public CellEditEventArgs (CellInfo cell, DataGridColumn column, object value)
		{
			Cell = cell;
			Column = column;
			Value = value;
		}
	}
}
