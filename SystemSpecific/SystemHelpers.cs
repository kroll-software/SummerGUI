using System;

namespace SummerGUI
{
	public static class SystemHelpers
	{
		public static string StringFromChar(this IntPtr ptr)
		{
			return System.Runtime.InteropServices.Marshal.PtrToStringAnsi(ptr);
		}
	}
}

