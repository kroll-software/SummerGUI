using System;
using System.IO;
using System.Reflection;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System.Runtime.InteropServices;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Mathematics;

namespace SummerGUI
{
	public static class PlatformExtensions
	{
		public enum OS
		{
			Unknown,
			Windows,
			Linux,
			Mac,
			Android
		}

		public static string ApplicationDirectory
		{
			get{
				//return Path.GetDirectoryName(Assembly.GetAssembly().CodeBase);
				return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			}
		}

		public static bool Is64BitProcess 
		{
			get{
				return IntPtr.Size == 8;
			}
		}

		public static bool IsRelativePath(this string path)
		{
			if (String.IsNullOrEmpty(path))
				return true;

			if (path.IndexOf (':') > 0 || path.IndexOf ("\\\\") == 0  || path.IndexOf ("//") == 0)
				return false;

			if (path[0] == '\\' || path[0] == '/' || path[0] == '.')
				return true;

			return true;
		}

		public static OS CurrentOS
		{
			get
			{
				// 1. Windows
				if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
					return OS.Windows;

				// 2. Linux
				//    (Unter .NET Core ist dies der zuverlässige Weg, da es alle Linux-Distributionen abdeckt)
				if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
					return OS.Linux;

				// 3. macOS
				if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
					return OS.Mac;

				// Anmerkung: Android und iOS/tvOS sind in der Enumeration enthalten, 
				// wenn Sie eine .NET-Plattform wie Xamarin/MAUI verwenden.
				// Für reines OpenTK/Desktop (GLFW) sind Linux/Windows/OSX relevant.
				
				// 4. Fallback (z.B. BSD, andere Unix-Derivate)
				if (RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
					return OS.Linux; // Oder ein spezifischeres OS.BSD, falls in Ihrer OS-Enum vorhanden

				// Unbekannt (Sollte selten erreicht werden)
				return OS.Unknown;
			}
		}

		public static string FixPathForPlatform(this string path)
		{						
			switch (Environment.OSVersion.Platform) {
			case PlatformID.Unix:
			case PlatformID.MacOSX:
				return path.Replace ('\\', '/');
			default:
				return path.Replace ('/', '\\');
			}				
		}

		public static string FixedExpandedPath(this string path)
		{
			path = FixPathForPlatform (path);
			if (path.IsRelativePath())
				return Path.Combine (ApplicationDirectory, path);
			return path;
		}

		public static void BringToFront(this NativeWindow window)
		{						
			if (window.WindowState == WindowState.Minimized)
			{
				// Wir setzen es auf Normal, bevor wir X11/GLFW anweisen, es zu fokussieren.
				//window.WindowState = WindowState.Normal;
			}

			switch (CurrentOS) {
			case OS.Android:
			case OS.Linux:
				SystemSpecific.Linux.Common.BringToFront (window);
				break;

			case OS.Mac:
				SystemSpecific.Mac.Common.BringToFront (window);
				break;

			case OS.Windows:
				SystemSpecific.Windows.Common.BringToFront (window);
				break;
			}
		}

		public static void SetParent(this NativeWindow window, NativeWindow parent)
		{						
			switch (CurrentOS) {
			case OS.Android:
			case OS.Linux:
				SystemSpecific.Linux.Common.SetParent (window, parent);
				break;

			case OS.Mac:
				SystemSpecific.Mac.Common.SetParent (window, parent);
				break;

			case OS.Windows:
				SystemSpecific.Windows.Common.SetParent (window, parent);
				break;
			}
		}
		
		public static void SetModalState(this NativeWindow window, NativeWindow parent, bool isModal)		
		{						
			switch (CurrentOS) {
			case OS.Android:
			case OS.Linux:
				SystemSpecific.Linux.Common.SetModalState (window, parent, isModal);
				break;

			case OS.Mac:
				//SystemSpecific.Mac.Common.MakeParent (window, parent);
				break;

			case OS.Windows:
				//SystemSpecific.Windows.Common.MakeParent (window, parent);
				break;
			}
		}

		public static void HideFromTaskbar(this NativeWindow window)
		{						
			switch (CurrentOS) {
			case OS.Android:
			case OS.Linux:
				SystemSpecific.Linux.Common.HideFromTaskbar (window);
				break;

			case OS.Mac:
				SystemSpecific.Mac.Common.HideFromTaskbar (window);
				break;

			case OS.Windows:
				SystemSpecific.Windows.Common.HideFromTaskbar (window);
				break;
			}
		}

		public static void SetClipboardText(string text)
		{						
			switch (CurrentOS) {
			case OS.Android:
			case OS.Linux:
				SystemSpecific.Linux.Common.SetClipboardText(text);
				break;

			case OS.Mac:
				SystemSpecific.Mac.Common.SetClipboardText(text);
				break;

			case OS.Windows:
				SystemSpecific.Windows.Common.SetClipboardText(text);
				break;
			}
		}

		public static string GetClipboardText()
		{						
			switch (CurrentOS) {
			case OS.Android:
			case OS.Linux:
				return SystemSpecific.Linux.Common.GetClipboardText ();
			case OS.Mac:
				return SystemSpecific.Mac.Common.GetClipboardText ();
			case OS.Windows:
				return SystemSpecific.Windows.Common.GetClipboardText ();
			default:
				return null;
			}				
		}

		public static bool IsClipboardTextAvailable()
		{						
			switch (CurrentOS) {
			case OS.Android:
			case OS.Linux:
				return SystemSpecific.Linux.Common.IsClipboardTextAvailable ();
			case OS.Mac:
				return SystemSpecific.Mac.Common.IsClipboardTextAvailable ();
			case OS.Windows:
				return SystemSpecific.Windows.Common.IsClipboardTextAvailable ();
			default:
				return true;
			}				
		}

        public static void RefreshCursor()
        {
            switch (CurrentOS)
            {                
                case OS.Windows:
                    SystemSpecific.Windows.Common.RefreshCursor();
                break;
            }
        }
	}
}

