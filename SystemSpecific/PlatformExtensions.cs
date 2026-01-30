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

		public static void HideMouseCursor(NativeWindow window)
		{
			switch (CurrentOS) {
			case OS.Android:
			case OS.Linux:
				window.Cursor = OpenTK.Windowing.Common.Input.MouseCursor.Empty;
				break;			

			case OS.Windows:
				SystemSpecific.Windows.Common.HideMouseCursor();
				break;

			case OS.Mac:				
				window.Cursor = OpenTK.Windowing.Common.Input.MouseCursor.Empty;
				break;
			}
		}

		public static void ShowMouseCursor(NativeWindow window)
		{
			switch (CurrentOS) {
			case OS.Android:
			case OS.Linux:
				window.Cursor = OpenTK.Windowing.Common.Input.MouseCursor.Default;
				break;			

			case OS.Windows:
				SystemSpecific.Windows.Common.ShowMouseCursor();
				break;

			case OS.Mac:				
				window.Cursor = OpenTK.Windowing.Common.Input.MouseCursor.Default;
				break;
			}
		}

		public static void MakeWindowModal(NativeWindow child, NativeWindow parent)
		{			
			switch (CurrentOS) {
			case OS.Android:
			case OS.Linux:
				SystemSpecific.Linux.Common.MakeWindowModal (child, parent);
				break;			

			case OS.Windows:
				SystemSpecific.Windows.Common.MakeWindowModal (child, parent);
				break;

			case OS.Mac:				
				SystemSpecific.Mac.Common.MakeWindowModal (child, parent);
				break;
			}			
		}

		public static void EnableWindow(NativeWindow window)
		{
			switch (CurrentOS) {
			case OS.Android:
			case OS.Linux:
				SystemSpecific.Linux.Common.EnableWindow (window);
				break;			

			case OS.Windows:
				SystemSpecific.Windows.Common.EnableWindow (window);
				break;

			case OS.Mac:				
				SystemSpecific.Mac.Common.EnableWindow (window);
				break;
			}			
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

