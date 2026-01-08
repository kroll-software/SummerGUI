using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

namespace SummerGUI
{
	public class X11Interface
	{
		// Die Definition der XClientMessageEvent, die wir für _NET_WM_STATE benötigen    

		[StructLayout(LayoutKind.Sequential)]
		public struct XClientMessageEvent
		{
			public int type; // Muss 33 (ClientMessage) sein
			public IntPtr serial;
			public bool send_event;
			public IntPtr display;
			public IntPtr window;
			public IntPtr message_type; // Das Atom, das die Nachricht identifiziert (z.B. _NET_WM_STATE)
			public int format; // Muss 32 sein (für 32-Bit-Daten)

			// Die Daten-Union der XClientMessageEvent: 5x long/IntPtr
			public IntPtr data0; 
			public IntPtr data1;
			public IntPtr data2;
			public IntPtr data3;
			public IntPtr data4;
		}

		// Die XEvent-Union (wir brauchen nur den Teil der ClientMessage)
		[StructLayout(LayoutKind.Explicit)]
		public struct XEvent
		{
			[FieldOffset(0)]
			public int type;

			[FieldOffset(0)]
			public XClientMessageEvent xclient;

			// XEvent ist mindestens 192 Bytes groß (48 * 4-Byte-Longs), wir brauchen nur den ClientMessage-Teil
			// Der Rest des Speichers muss in C# reserviert werden, um Pufferüberläufe zu vermeiden.
			// Ein typischer Wert ist 200 Bytes.
			[FieldOffset(0)]
			private unsafe fixed byte padding[200];
		}

		/// <summary> Change the parent window of the specified window. </summary>
		/// <param name="x11display"> The display pointer, that specifies the connection to the X server. <see cref="IntPtr"/> </param>
		/// <param name="x11window"> The window to resize. <see cref="IntPtr"/> </param>
		/// <param name="x"> The new x coordinate, which defines the new location of the top-left pixel of the window inside the new parent window. <see cref="System.Int32"/> </param>
		/// <param name="y"> The new y coordinate, which defines the new location of the top-left pixel of the window inside the new parent window. <see cref="System.Int32"/> </param>
		/// <remarks> If the specified window is mapped, XReparentWindow() automatically performs an UnmapWindow request on it,
		///   removes it from its current position in the hierarchy, and inserts it as the child of the specified parent.
		///   The window is placed in the stacking order on top with respect to sibling windows.
		/// * After reparenting the specified window, XReparentWindow() causes the X server to generate a ReparentNotify event.
		///   The override_redirect member returned in this event is set to the window's corresponding attribute. Window manager
		///   clients usually should ignore this window if this member is set to True. Finally, if the specified window was
		///   originally mapped, the X server automatically performs a MapWindow request on it.
		/// * The X server performs normal exposure processing on formerly obscured windows. The X server might not generate Expose
		///   events for regions from the initial UnmapWindow request that are immediately obscured by the final MapWindow request. </remarks>
		[DllImport("libX11", CallingConvention = CallingConvention.Cdecl)]
		extern public static void XReparentWindow (IntPtr x11display, IntPtr x11window, IntPtr x11parentWindow, int x, int y);

        [DllImport("libX11", CallingConvention = CallingConvention.Cdecl)]
        extern public static IntPtr XOpenDisplay(IntPtr display_name);
        //char *display_name;

		[DllImport("libX11", CallingConvention = CallingConvention.Cdecl)]
        extern public static void XSetTransientForHint(IntPtr display, IntPtr window, IntPtr propWindow);

        [DllImport("libX11", CallingConvention = CallingConvention.Cdecl)]
        extern public static void XRaiseWindow(IntPtr display, IntPtr w);
        //Display* display;
        //Window w;

        [DllImport("libX11", CallingConvention = CallingConvention.Cdecl)]
        extern public static void XSetInputFocus(IntPtr display, IntPtr focus, int revert_to, uint time);
        //Display* display;
        //Window focus;
        //int revert_to;
        //Time time;

        [DllImport("libX11", CallingConvention = CallingConvention.Cdecl)]
        extern public static void XMapRaised(IntPtr display, IntPtr window);

		// NEU: Übersetzt einen String-Namen in ein X-Atom (XID-Typ)
        // Benötigt für EWMH-Properties wie "_NET_WM_STATE"
        [DllImport("libX11", CallingConvention = CallingConvention.Cdecl)]
        extern public static IntPtr XInternAtom(IntPtr display, string atom_name, bool only_if_exists);

        // NEU: Erzwingt die sofortige Ausführung aller gepufferten X11-Befehle
        // Wichtig, nachdem man Eigenschaften gesetzt oder Events gesendet hat.
        [DllImport("libX11", CallingConvention = CallingConvention.Cdecl)]
        extern public static int XFlush(IntPtr display);        

		[DllImport("libX11", CallingConvention = CallingConvention.Cdecl)]
        extern public static int XSendEvent(IntPtr display, IntPtr window, bool propagate, IntPtr event_mask, ref XEvent event_send);

		[DllImport("libX11", CallingConvention = CallingConvention.Cdecl)]
        extern public static IntPtr XRootWindow(IntPtr display, int screen_number);
    }
}

