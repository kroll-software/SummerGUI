using System;
using OpenTK;
using KS.Foundation;
using System.Runtime.InteropServices;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Mathematics;

namespace SummerGUI.SystemSpecific.Linux
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

		public static unsafe void SetParent(NativeWindow window, NativeWindow parent)
        {
            nint displayHandle = GLFW.GetX11Display(); 
			if (displayHandle == IntPtr.Zero) return; // Nicht unter X11 oder kein Handle

			nint parentXid = (nint)GLFW.GetX11Window((Window*)parent.WindowPtr);
			nint childXid = (nint)GLFW.GetX11Window((Window*)window.WindowPtr);

			if (parentXid != IntPtr.Zero && childXid != IntPtr.Zero)
			{
				// XSetTransientForHint setzt die WM_TRANSIENT_FOR Property
				X11Interface.XSetTransientForHint(displayHandle, childXid, parentXid);
			}
        }

		// Nur das Konzept, Du musst die XSendEvent-Struktur implementieren!
		// Angenommen, diese Funktion gehört zu Deiner SummerGUIWindow Klasse oder einem Utility-Helper
		public static unsafe void SetModalState(NativeWindow child, NativeWindow parent, bool isModal)
		{
			IntPtr displayHandle = GLFW.GetX11Display();
			if (displayHandle == IntPtr.Zero) return;

			// 1. Hole XIDs
			IntPtr childXid = (nint)GLFW.GetX11Window((Window*)child.WindowPtr);
			
			if (childXid == IntPtr.Zero) return;

			// 2. Erzeuge/Hole die Atoms (muss nur einmal geschehen!)
			// Speichere die Ergebnisse statisch, falls möglich, um XInternAtom zu vermeiden.
			IntPtr wmStateAtom = X11Interface.XInternAtom(displayHandle, "_NET_WM_STATE", false);
			IntPtr modalAtom = X11Interface.XInternAtom(displayHandle, "_NET_WM_STATE_MODAL", false);
			IntPtr windowManagerWindow = X11Interface.XRootWindow(displayHandle, 0); // Hole das Root Window (X-Screen 0)
			
			// Stelle sicher, dass XRootWindow in X11Interface implementiert ist! (DllImport("libX11", ...) extern public static IntPtr XRootWindow(IntPtr display, int screen);)

			// 3. Setze den Transient Hint (Die Eltern-Kind-Beziehung)
			IntPtr parentXid = (nint)GLFW.GetX11Window((Window*)parent.WindowPtr);
			if (parentXid != IntPtr.Zero)
			{
				X11Interface.XSetTransientForHint(displayHandle, childXid, parentXid);
			}

			// 4. ClientMessage Event vorbereiten
			X11Interface.XEvent ev = new X11Interface.XEvent();
			ev.type = 33; // ClientMessage
			
			// Die Nachricht wird an das Root-Fenster gesendet, damit der Fenstermanager sie erhält
			ev.xclient.window = childXid; 
			ev.xclient.message_type = wmStateAtom;
			ev.xclient.format = 32;

			// Daten (EWMH-Spezifikation für _NET_WM_STATE):
			// data0: Action (1 = Add, 0 = Remove)
			// data1: Atom 1 (hier: _NET_WM_STATE_MODAL)
			// data2: Atom 2 (0)
			// data3: Source Indicator (2 = Application, empfohlen)

			ev.xclient.data0 = (IntPtr)(isModal ? 1 : 0); // 1 (Add) oder 0 (Remove)
			ev.xclient.data1 = modalAtom;
			ev.xclient.data2 = IntPtr.Zero;
			ev.xclient.data3 = (IntPtr)2; // Quelle: Applikation

			// 5. Nachricht senden (an das Root Window)
			// windowManagerWindow ist das Root-Fenster.
			// propagate = false
			// event_mask = (IntPtr)0xFFFFFF // Gesamte Maske für SubstructureNotifyMask/SubstructureRedirectMask
			IntPtr eventMask = (IntPtr)(1L << 19 | 1L << 20); // SubstructureNotifyMask | SubstructureRedirectMask

			X11Interface.XSendEvent(
				displayHandle, 
				windowManagerWindow, 
				false, 
				eventMask, 
				ref ev
			);

			X11Interface.XFlush(displayHandle);
		}

		// Sie müssen die Methode mit 'unsafe' markieren, da Pointer-Typen verwendet werden.
		public static unsafe void BringToFront(NativeWindow window)
		{
			// 1. Initialisierung und Null-Check
			void* windowPointer = window.WindowPtr;
			if (window == null || (nint)windowPointer == IntPtr.Zero)
				return;

			Window* windowHandle = (Window*)windowPointer;

			if (window.WindowState == WindowState.Minimized)
			{
				//window.WindowState = WindowState.Normal;
			}

			GLFW.RequestWindowAttention(windowHandle);
			GLFW.FocusWindow(windowHandle);

			if (window.WindowState != WindowState.Normal)
			{
				//window.WindowState = WindowState.Normal;
			}

			IntPtr displayHandle = GLFW.GetX11Display();
			nint xidHandle = (nint)GLFW.GetX11Window(windowHandle);

			if (displayHandle != IntPtr.Zero && xidHandle != IntPtr.Zero)
			{
				// 6. Direkte X11-Operationen für X11-Umgebungen (z.B. Ubuntu Standard)
				// Diese sind nötig, da glfwRequestWindowAttention unter X11 nicht immer sofort klappt.
				X11Interface.XRaiseWindow(displayHandle, xidHandle);
				X11Interface.XSetInputFocus(displayHandle, xidHandle, 0, 0); // Setzen des Fokus
				//X11Interface.XFlush(displayHandle);
			}
		}		

		public static void HideFromTaskbar(NativeWindow window)
		{
			///if (window == null || window.WindowInfo.Handle == IntPtr.Zero)
			///	return;
		}		

		public static unsafe void MakeParentWindow(NativeWindow parent, NativeWindow child)
		{
			// Die Eigenschaft child.WindowPtr ist vom Typ Window*. Wir müssen sie als void* behandeln.
			void* parentPointer = parent.WindowPtr;
			void* childPointer = child.WindowPtr;

			// 1. Handle-Prüfung (Vergleich des Pointers mit 0/null)
			if (parent == null || (nint)parentPointer == IntPtr.Zero || child == null || (nint)childPointer == IntPtr.Zero)
				return;
			
			// 2. X11 Display Handle abrufen
			//    GLFW.GetX11Display gibt den Display-Pointer zurück (IntPtr)
			IntPtr displayHandle = GLFW.GetX11Display(); 
			if (displayHandle == IntPtr.Zero)
				return;

			// 3. X11 Window ID Handles abrufen (nuint -> nint Cast erforderlich)
			//    Hier wird der GLFW-Pointer (Window*) in den X11 Window ID Handle (nuint) konvertiert.
			nint childXidHandle = (nint)GLFW.GetX11Window((Window*)childPointer);
			nint parentXidHandle = (nint)GLFW.GetX11Window((Window*)parentPointer);

			if (childXidHandle == nint.Zero || parentXidHandle == nint.Zero)
				return;

			// 4. Position abrufen
			//    Die Position (X, Y) ist jetzt in NativeWindow.Location (Vector2i)
			Vector2i childLocation = child.Location;
			int childX = childLocation.X;
			int childY = childLocation.Y;

			// 5. XReparentWindow Aufruf
			// X11Interface.XReparentWindow erwartet:
			// (Display*)displayHandle, (Window)childXidHandle, (Window)parentXidHandle, x, y
			
			X11Interface.XReparentWindow(
				displayHandle, 
				childXidHandle, 
				parentXidHandle, 
				childX, 
				childY
			);
			
			// Die ReflectionUtils.SetPropertyValue Zeile wird ignoriert, da sie nicht mehr relevant ist.
			
			// Optional: Child-Fenster anzeigen (falls es zuvor versteckt war)
			// X11Interface.XMapWindow(displayHandle, childXidHandle);
		}
	}
}

