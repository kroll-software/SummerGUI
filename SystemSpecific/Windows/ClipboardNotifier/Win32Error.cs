using System;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text;

// Remark by Kroll-Software:
// This code was published on http://www.vbaccelerator.com
// under the Creative Commons Licence
// http://creativecommons.org/licenses/by/1.0/

//namespace vbAccelerator.Components.Win32
namespace ProjectPlanner.Win32
{
	/// <summary>
	/// Helper method for returning the description associated with a 
	/// <see cref="System.Runtime.InteropServices.Marshal.GetLastWin32Error"/>
	/// error code.
	/// </summary>
	public class Win32Error
	{

		private const int FORMAT_MESSAGE_ALLOCATE_BUFFER = 0x100;
		private const int FORMAT_MESSAGE_ARGUMENT_ARRAY = 0x2000;
		private const int FORMAT_MESSAGE_FROM_HMODULE = 0x800;
		private const int FORMAT_MESSAGE_FROM_STRING = 0x400;
		private const int FORMAT_MESSAGE_FROM_SYSTEM = 0x1000;
		private const int FORMAT_MESSAGE_IGNORE_INSERTS = 0x200;
		private const int FORMAT_MESSAGE_MAX_WIDTH_MASK = 0xFF;
		
		[DllImport("kernel32", CharSet=CharSet.Auto)]
		private static extern int FormatMessage (
			int dwFlags, 
			IntPtr lpSource, 
			int dwMessageId, 
			int dwLanguageId,
			[MarshalAs(UnmanagedType.LPTStr)]
			StringBuilder lpBuffer, 
			int nSize, 
			IntPtr Arguments);

		/// <summary>
		/// Returns a string containing the error message for a Win32 API error code.
		/// </summary>
		/// <param name="lastWin32Error">Win32 Error</param>
		/// <returns>Error Message associated with the Win32 Error</returns>
		public static string ErrorMessage(
			int lastWin32Error
			)
		{
			StringBuilder msg = new StringBuilder(256, 256);
			int size = FormatMessage(
				FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS,
				IntPtr.Zero, lastWin32Error, 0,
				msg, msg.MaxCapacity, IntPtr.Zero);
			return msg.ToString();
		}

		/// <summary>
		/// Private constructor - static methods.
		/// </summary>
		private Win32Error()
		{
			// intentionally blank
		}
	}
}
