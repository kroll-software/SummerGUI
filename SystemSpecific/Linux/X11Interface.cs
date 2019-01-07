using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

namespace SummerGUI
{

	public class X11Interface
	{

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
		[DllImport("libX11")]
		extern public static void XReparentWindow (IntPtr x11display, IntPtr x11window, IntPtr x11parentWindow, int x, int y);

	}
}

