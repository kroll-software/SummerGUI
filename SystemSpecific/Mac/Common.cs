using System;
using System.Diagnostics;
using KS.Foundation;
using OpenTK;

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
	}
}
