using System;
using System.IO;
using System.Reflection;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System.Runtime.InteropServices;

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
				return (IntPtr.Size == 8);
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
			get{
				if (Configuration.RunningOnWindows)
					return OS.Windows;				

				if (Configuration.RunningOnLinux || Configuration.RunningOnUnix)
					return OS.Linux;

				if (Configuration.RunningOnMacOS)
					return OS.Mac;

				if (Configuration.RunningOnAndroid)
					return OS.Android;

				return OS.Unknown;

				// ha ha ha ha ha ha ha:
				// returns MacOSX for my Ubuntu System
				// Useless crap, this function..
				// Environment.OSVersion
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

		public static void BringToFront(this INativeWindow window)
		{						
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

		public static void HideFromTaskbar(this INativeWindow window)
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

