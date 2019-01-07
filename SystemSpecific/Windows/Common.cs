using System;
using KS.Foundation;
using OpenTK;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SummerGUI.SystemSpecific.Windows
{	
	public static class Common
	{		
		static string _ClipboardText;
		public static void SetClipboardText(string text)
		{
			_ClipboardText = text;
		}

		public static string GetClipboardText()
		{
			return _ClipboardText;
		}

		public static bool IsClipboardTextAvailable()
		{
			return !String.IsNullOrEmpty (_ClipboardText);
		}


		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool ShowWindowAsync(IntPtr windowHandle, int nCmdShow);
		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool SetForegroundWindow(IntPtr windowHandle);
		[DllImport("User32.dll", SetLastError = true)]
		private static extern bool ShowWindow(IntPtr handle, int nCmdShow);
		[DllImport("User32.dll", SetLastError = true)]
		private static extern bool IsIconic(IntPtr handle);

		// Hide from Taskbar
		[DllImport("User32.dll")]
		public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
		[DllImport("User32.dll")]
		public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

		const int SW_HIDE = 0;
		const int SW_SHOW = 5;
		const int SW_RESTORE = 9;
		const int SW_SHOWDEFAULT = 10;
		const int WS_EX_APPWINDOW = 0x40000;
		const int GWL_EXSTYLE = -0x14;
		const int GWL_HWNDPARENT = -8;
		const int WS_EX_TOOLWINDOW = 0x00000080;

        [StructLayout(LayoutKind.Sequential)]
        struct POINT
        {
            public int X;
            public int Y;
        }

        [DllImport("user32.dll")]
        static extern bool SetCursorPos(int xPos, int yPos);

        [DllImport("user32.dll")]
        static extern bool GetCursorPos(out POINT lpPoint);        

		public static void BringToFront(INativeWindow window)
		{
			try {
				if (window == null || window.WindowInfo.Handle == IntPtr.Zero)
					return;

				IntPtr windowHandle = window.WindowInfo.Handle;

				window.WindowState = WindowState.Minimized;
				SetForegroundWindow(windowHandle);	
				window.WindowState = WindowState.Normal;
			} catch (Exception ex) {
				ex.LogError ();
			}				
		}

		public static void BringProcessToFront(Process process)
		{
			// Not Supported by Mono
			IntPtr handle = process.MainWindowHandle;
			if (IsIconic(handle))
			{
				ShowWindow(handle, SW_RESTORE);
			}

			SetForegroundWindow(handle);
		}

		public static void HideFromTaskbar(INativeWindow window)
		{
			if (window == null || window.WindowInfo.Handle == IntPtr.Zero)
				return;
			
			IntPtr Handle = window.WindowInfo.Handle;
			int windowStyle = GetWindowLong(Handle, GWL_EXSTYLE);
			SetWindowLong(Handle, GWL_EXSTYLE, windowStyle | WS_EX_TOOLWINDOW);
		}

        public static void RefreshCursor()
        {
            try
            {
                POINT p;
                if (GetCursorPos(out p))
                    SetCursorPos(p.X, p.Y);
            }
            catch (Exception ex)
            {
                ex.LogWarning();
            }            
        }
	}
}

