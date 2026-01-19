using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using KS.Foundation;
using OpenTK;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Mathematics;


namespace SummerGUI.SystemSpecific.Mac
{	
	public static class Common
	{
		public static void SetClipboardText(string text)
		{
			try
			{
				using (var p = new Process())
				{

					p.StartInfo = new ProcessStartInfo("pbcopy", "-pboard general -Prefer txt");
					p.StartInfo.UseShellExecute = false;
					p.StartInfo.RedirectStandardOutput = false;
					p.StartInfo.RedirectStandardInput = true;
					p.Start();
					p.StandardInput.Write(text);
					p.StandardInput.Close();
					p.WaitForExit();
				}
			}
			catch (Exception ex)
			{
				ex.LogError ();
			}
		}

		public static string GetClipboardText()
		{
			try
			{
				string pasteText;
				using (var p = new Process())
				{

					p.StartInfo = new ProcessStartInfo("pbpaste", "-pboard general");
					p.StartInfo.UseShellExecute = false;
					p.StartInfo.RedirectStandardOutput = true;
					p.Start();
					pasteText = p.StandardOutput.ReadToEnd();
					p.WaitForExit();
				}

				return pasteText;
			}
			catch (Exception ex)
			{
				ex.LogError ();
				return null;
			}
		}

		public static bool IsClipboardTextAvailable()
		{
			// ToDo:
			return true;
		}

		// Hilfsmethode für die Modal-Logik
		[DllImport("/usr/lib/libobjc.A.dylib")]
		public static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector, int arg1);

		private static unsafe IntPtr GetNSWindow(NativeWindow window)
        {
            // GLFW liefert uns den Pointer zum Cocoa-Window
            return GLFW.GetCocoaWindow(window.WindowPtr);
        }

		public static void MakeWindowModal(NativeWindow child, NativeWindow parent)
        {
            IntPtr childPtr = GetNSWindow(child);
            IntPtr parentPtr = GetNSWindow(parent);

            if (childPtr != IntPtr.Zero && parentPtr != IntPtr.Zero)
            {
                // 1. Das Child-Fenster an das Parent-Fenster binden
                // Selector: setParentWindow:
                MacNative.objc_msgSend(childPtr, MacNative.sel_registerName("setParentWindow:"), parentPtr);

                // 2. Den Modal-Status für die App setzen
                // Dies bewirkt, dass Events nur noch an das Child-Fenster gehen
                IntPtr nsAppClass = MacNative.objc_getClass("NSApplication");
                IntPtr sharedApp = MacNative.objc_msgSend(nsAppClass, MacNative.sel_registerName("sharedApplication"));
                
                // Wir starten die Modal-Session
                // Hinweis: Da Run() in OpenTK blockiert, wird die Modal-Session 
                // durch den GLFW-Loop des Child-Windows am Leben erhalten.
                MacNative.objc_msgSend(sharedApp, MacNative.sel_registerName("runModalForWindow:"), childPtr);
            }
        }

        public static void EnableWindow(NativeWindow window)
        {
            // Auf dem Mac beenden wir die Modal-Session
            IntPtr nsAppClass = MacNative.objc_getClass("NSApplication");
            IntPtr sharedApp = MacNative.objc_msgSend(nsAppClass, MacNative.sel_registerName("sharedApplication"));
            
            // Selector: stopModal
            MacNative.objc_msgSend(sharedApp, MacNative.sel_registerName("stopModal"));
        }

		public static void SetParent(NativeWindow window, NativeWindow parent)
        {
            
        }

		public static void BringToFront(NativeWindow window)
		{
			//if (window == null || window.WindowInfo.Handle == IntPtr.Zero)
			//	return;
		}		

		public static void HideFromTaskbar(NativeWindow window)
		{
			//if (window == null || window.WindowInfo.Handle == IntPtr.Zero)
			//	return;
		}
	}
}
