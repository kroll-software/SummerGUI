using System;
using OpenTK;
using System.Runtime.InteropServices;

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

		public static void BringToFront(INativeWindow window)
		{
			if (window == null || window.WindowInfo.Handle == IntPtr.Zero)
				return;
		}

		public static void HideFromTaskbar(INativeWindow window)
		{
			if (window == null || window.WindowInfo.Handle == IntPtr.Zero)
				return;
		}

		public static void MakeParentWindow(IntPtr display, INativeWindow parent, INativeWindow child)
		{
			if (parent == null || parent.WindowInfo.Handle == IntPtr.Zero || child == null || child.WindowInfo.Handle == IntPtr.Zero)
				return;

			X11Interface.XReparentWindow (display, child.WindowInfo.Handle, parent.WindowInfo.Handle, child.X, child.Y);
		}
	}
}

