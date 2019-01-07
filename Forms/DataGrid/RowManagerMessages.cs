using System;
using System.Linq;
using KS.Foundation;

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

	public class UpdateRecordCountMessage : EventMessage<RecordCountInfo>
	{
		public UpdateRecordCountMessage (int rowCount, int rowIndex)
			: base (RowManagerMessages.RowsChanged, new RecordCountInfo(rowCount, rowIndex)) {}
	}
}

