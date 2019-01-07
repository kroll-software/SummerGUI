using System;
using System.Collections;
using System.Collections.Generic;

namespace SummerGUI
{	
	public class StatusMessageItem
	{
		public string Status { get; set; }
		public bool WaitCursor { get; set; }

		public StatusMessageItem(string status, bool waitcursor)
		{
			Status = status;
			WaitCursor = waitcursor;
		}
	}

	public class StatusMessageStack : Stack<StatusMessageItem>
	{
		public StatusMessageStack ()
		{
		}
	}
}

