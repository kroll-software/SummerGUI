using System;
using KS.Foundation;
using OpenTK;
using System.Diagnostics;
using System.Runtime.InteropServices;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Mathematics;

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

		public const int GWL_STYLE = -16;
		//public const int GWL_EXSTYLE = -20;

		public const uint WS_MAXIMIZEBOX = 0x00010000;
		public const uint WS_MINIMIZEBOX = 0x00020000;
		public const uint WS_EX_DLGMODALFRAME = 0x00000001;
		//public const uint WS_EX_TOOLWINDOW = 0x00000080; // Versteckt das Fenster in der Taskleiste

		// Zusätzliche P/Invokes für den Refresh
		[DllImport("user32.dll")]
		public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

		public const uint SWP_NOSIZE = 0x0001;
		public const uint SWP_NOMOVE = 0x0002;
		public const uint SWP_NOZORDER = 0x0004;
		public const uint SWP_FRAMECHANGED = 0x0020; // WICHTIG: Erzwingt Neuzeichnen des Rahmens

		[DllImport("user32.dll", EntryPoint = "SetClassLongPtr")]
		public static extern IntPtr SetClassLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

		[DllImport("user32.dll", EntryPoint = "GetClassLongPtr")]
		public static extern IntPtr GetClassLongPtr(IntPtr hWnd, int nIndex);

		// Die benötigten Konstanten
		public const int GCL_STYLE = -26;
		public const uint CS_VREDRAW = 0x0001;
		public const uint CS_HREDRAW = 0x0002;
		// Falls du auf einem 32-Bit System kompilierst, bräuchtest du SetClassLong.
		// Da SummerGUI aber modern auf 64-Bit ausgelegt ist, ist dies der Standard:
		public const int GCLP_HBRBACKGROUND = -10;

		[DllImport("user32.dll")]
		private static extern int ShowCursor(bool bShow);

		public static void HideMouseCursor()
		{		
			while (ShowCursor(false) >= 0);
		}

		public static void ShowMouseCursor()
		{
			while (ShowCursor(true) < 0);			
		}
				
		// In SummerGUI.SystemSpecific.Windows.PlatformExtensions
		public static unsafe void MakeWindowModal(NativeWindow child, NativeWindow parent)
		{
			IntPtr childHwnd = GLFW.GetWin32Window(child.WindowPtr);
			IntPtr parentHwnd = GLFW.GetWin32Window(parent.WindowPtr);

			if (childHwnd != IntPtr.Zero && parentHwnd != IntPtr.Zero)
			{
				// 1. Owner setzen (Owner != Parent im Win32-Sinn)
				// Das sorgt dafür, dass das Child-Fenster KEIN Icon in der Taskleiste hat,
				// solange es einen validen Owner besitzt und kein AppWindow-Style gesetzt ist.
				SetWindowLongPtr(childHwnd, -8, parentHwnd);

				// 2. Styles anpassen: Normaler Dialog-Look ohne Min/Max
				uint style = GetWindowLong(childHwnd, GWL_STYLE);
				style &= ~(WS_MAXIMIZEBOX | WS_MINIMIZEBOX);
				SetWindowLong(childHwnd, GWL_STYLE, style);

				// 3. Extended Styles: 
				// Wir entfernen TOOLWINDOW und setzen DLGMODALFRAME für den Dialog-Rahmen.
				// Das normale 'X' bleibt erhalten.
				uint exStyle = GetWindowLong(childHwnd, GWL_EXSTYLE);
				exStyle &= ~WS_EX_TOOLWINDOW; // Tool-Look entfernen
				exStyle |= WS_EX_DLGMODALFRAME;
				//exStyle |= (0x02000000 | 0x04000000); // WS_CLIPCHILDREN | WS_CLIPSIBLINGS	// Test
				SetWindowLong(childHwnd, GWL_EXSTYLE, exStyle);

				//SetClassLongPtr(childHwnd, -10, IntPtr.Zero); // GCLP_HBRBACKGROUND = -10
				//SetClassLongPtr(childHwnd, -26, (IntPtr)(GetClassLongPtr(childHwnd, -26).ToInt64() | 0x0001 | 0x0002));

				// 4. Hauptfenster deaktivieren
				EnableWindow(parentHwnd, false);
				
				// 5. Fenster-Update erzwingen (damit die Rahmenänderung sofort sichtbar wird)
				SetWindowPos(childHwnd, IntPtr.Zero, 0, 0, 0, 0, 
					SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);
			}
		}
		public static unsafe void EnableWindow(NativeWindow window)
		{
			IntPtr hwnd = GLFW.GetWin32Window(window.WindowPtr);
			EnableWindow(hwnd, true);
			SetForegroundWindow(hwnd);
		}

		[DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
		private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

		[DllImport("user32.dll")]
		private static extern bool EnableWindow(IntPtr hWnd, bool bEnable);

		[DllImport("user32.dll", SetLastError = true)]
    	public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

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
		public static extern uint SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);
		[DllImport("User32.dll")]
		public static extern uint GetWindowLong(IntPtr hWnd, int nIndex);

		[DllImport("user32.dll", SetLastError = true)]
		static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

		[DllImport("user32.dll")]
    	public static extern int SendMessage(IntPtr hWnd, int wMsg, IntPtr wParam, IntPtr lParam);

		[DllImport("user32.dll", SetLastError = true)]
		public static extern IntPtr SetActiveWindow(IntPtr hWnd);

		// 1. Die Win32 MSG-Struktur
        // Sie repräsentiert eine Nachricht in der Windows-Warteschlange.
        [StructLayout(LayoutKind.Sequential)]
        public struct MSG
        {
            public IntPtr hwnd;
            public uint message;
            public IntPtr wParam;
            public IntPtr lParam;
            public uint time;
            // Point Struktur ist OpenTK.Mathematics.Point oder System.Drawing.Point, 
            // je nachdem, was Du in Deinem Namespace verwendest. 
            // Hier verwenden wir die native Win32-Definition, die 2 int-Felder sind.
            public int ptX; 
            public int ptY;
            // Die lPrivate Variable wird manchmal weggelassen, ist aber Teil der vollen Struktur.
        }

        // --- Die drei Hauptfunktionen zur Nachrichtenverarbeitung ---

        // 2. PeekMessage: Prüft auf Nachrichten und entfernt sie optional
        // wRemoveMsg: PM_REMOVE (1) = Nachricht aus der Queue entfernen
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool PeekMessage(
            out MSG lpMsg, 
            IntPtr hWnd, 
            uint wMsgFilterMin, 
            uint wMsgFilterMax, 
            uint wRemoveMsg
        );
        
        // Konstante für wRemoveMsg
        public const uint PM_REMOVE = 0x0001; 

        // 3. TranslateMessage: Übersetzt virtuelle Tastencodes in Zeichen-Nachrichten
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool TranslateMessage(ref MSG lpMsg);

        // 4. DispatchMessage: Sendet die Nachricht an die entsprechende Fensterprozedur
        [DllImport("user32.dll")]
        public static extern IntPtr DispatchMessage(ref MSG lpMsg);

		[DllImport("user32.dll")]
		public static extern IntPtr GetForegroundWindow();

		[DllImport("user32.dll")]
		public static extern IntPtr GetActiveWindow();

		[DllImport("user32.dll", SetLastError=true)]
		public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

		// Win32 Konstanten für Keyboard Events
		const uint WM_SETFOCUS = 0x0007; // Setzt den Fokus
		const uint WM_ACTIVATE = 0x0006; // Aktiviert das Fenster
		const int WA_ACTIVE = 1; // Zustand für WM_ACTIVATE

		const int WM_LBUTTONDOWN = 0x0201;
		const int WM_LBUTTONUP = 0x0202;

		const int SW_HIDE = 0;
		const int SW_SHOW = 5;
		const int SW_RESTORE = 9;
		const int SW_SHOWDEFAULT = 10;
		const int WS_EX_APPWINDOW = 0x40000;
		public const int GWL_EXSTYLE = -0x14;
		public const uint WS_EX_NOACTIVATE = 0x08000000;
		const int GWL_HWNDPARENT = -8;
		const uint WS_EX_TOOLWINDOW = 0x00000080;

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

		public static unsafe void SetParent(NativeWindow window, NativeWindow parent)
        {
            if (window == null | true)
				return;
				
			// 1. Hole die Handles
			IntPtr childHandle = IntPtr.Zero;
			IntPtr parentHandle = IntPtr.Zero;

			// Hole den Win32 HWND-Handle des Kind-Fensters
			if (window.WindowPtr != null)
			{
				childHandle = GLFW.GetWin32Window(window.WindowPtr);
			}

			// Hole den Win32 HWND-Handle des Eltern-Fensters
			if (parent != null && parent.WindowPtr != null)
			{
				parentHandle = GLFW.GetWin32Window(parent.WindowPtr);
			}

			if (childHandle == IntPtr.Zero)
			{
				Debug.WriteLine("SetParent: Child HWND konnte nicht abgerufen werden.");
				return;
			}

			// 2. Setze die Parent-Beziehung
			// Der Rückgabewert ist der Handle des alten Parent-Fensters oder 0 bei Fehler.
			IntPtr oldParent = SetParent(childHandle, parentHandle);

			if (oldParent == IntPtr.Zero && Marshal.GetLastWin32Error() != 0)
			{
				Debug.WriteLine($"SetParent fehlgeschlagen. Win32 Error: {Marshal.GetLastWin32Error()}");
			}
			
			// 3. Optionaler Fix: Setze den WS_EX_TOOLWINDOW Stil, um das Kind-Fenster 
			// aus der Taskleiste zu entfernen (da es ein Child ist).
			if (parentHandle != IntPtr.Zero)
			{
				// Hole die aktuellen erweiterten Stile
				uint exStyle = GetWindowLong(childHandle, GWL_EXSTYLE);
				
				// Füge den WS_EX_TOOLWINDOW Stil hinzu
				exStyle |= WS_EX_TOOLWINDOW;

				// Setze die neuen Stile
				SetWindowLong(childHandle, GWL_EXSTYLE, exStyle);
			}
        }

		public static void BringToFront(NativeWindow window)
		{			
			try{
				unsafe // Fügen Sie unsafe zur Methode hinzu
				{
					Window* windowPointer = window.WindowPtr;
					IntPtr windowHandle = GLFW.GetWin32Window(windowPointer);		

					GLFW.FocusWindow(windowPointer);
										
					//IntPtr lParam = (IntPtr)((-1 << 16) | (-1 & 0xFFFF));
					IntPtr negativeCoords = (IntPtr)((-1 << 16) | (0xFFFF));
					SendMessage(windowHandle, WM_LBUTTONDOWN, IntPtr.Zero, negativeCoords);
					SendMessage(windowHandle, WM_LBUTTONUP, IntPtr.Zero, negativeCoords);					
				}
			}
			catch (Exception ex) 
			{
				ex.LogError(); 
				// Logik für die Fehlerprotokollierung
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

		public static unsafe void HideFromTaskbar(NativeWindow window)
		{
			// 1. Hole den GLFW Window Pointer (Window*)
			void* windowPointer = window.WindowPtr;
			
			try 
			{
				// 2. Handle-Prüfung
				if (window == null || (nint)windowPointer == IntPtr.Zero)
					return;

				// 3. HWND abrufen
				// GLFW.GetWin32Window erwartet Window* und gibt den HWND (IntPtr) zurück.
				IntPtr windowHandle = GLFW.GetWin32Window((Window*)windowPointer);

				if (windowHandle == IntPtr.Zero)
					return;
					
				// 4. Win32 Aufrufe
				// Die Logik zur Modifizierung der Fensterstile bleibt unverändert.
				uint windowStyle = GetWindowLong(windowHandle, GWL_EXSTYLE);
				
				// Fügt den WS_EX_TOOLWINDOW-Stil hinzu
				SetWindowLong(windowHandle, GWL_EXSTYLE, windowStyle | WS_EX_TOOLWINDOW);
			} 
			catch (Exception ex) 
			{
				ex.LogError();
				// Logik für die Fehlerprotokollierung
			}               
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

