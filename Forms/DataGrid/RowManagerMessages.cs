using System;
using System.Linq;
using KS.Foundation;
using SummerGUI.DataGrid;

namespace SummerGUI
{	
	public struct RecordCountInfo
	{
		public readonly int RowCount;
		public readonly int CurrentRowIndex;

		public RecordCountInfo(int rowCount, int rowIndex)
		{
			RowCount = rowCount;
			CurrentRowIndex = rowIndex;
		}
	}

	public class UpdateRecordCountMessage : IFoundationMessage
	{
		public RecordCountInfo Info { get; private set; }
		
		// Implementierung der IFoundationMessage Anforderungen
		public string Subject => RowManagerMessages.RowsChanged; // Angenommen, das ist eine Konstante
		public bool ShouldReEnqueue => false; // Standardwert

		// Konstruktor, der die Daten aufnimmt
		public UpdateRecordCountMessage(int rowCount, int rowIndex)
		{
			Info = new RecordCountInfo(rowCount, rowIndex);
		}
		
		// Wenn Sie die ursprüngliche EventMessage zur Übergabe in die Queue benötigen,
		// fügen Sie eine Konvertierungsmethode hinzu:
		public EventMessage ToEventMessage(object sender)
		{
			// Die alte EventMessage benötigt object[] Args im Konstruktor
			return new EventMessage(sender, this.Subject, this.ShouldReEnqueue, new object[] { this.Info });
		}
	}
}

