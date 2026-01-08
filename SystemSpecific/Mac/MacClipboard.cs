using System;
using System.Diagnostics;
using KS.Foundation;

namespace SummerGUI.SystemSpecific.Mac
{	
	public class MacClipboard : IClipboard
	{
		/// <summary>
		/// Copy on MAC OS X using pbcopy commandline
		/// </summary>
		/// <param name="textToCopy"></param>
		public void SetText(string textToCopy)
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
					p.StandardInput.Write(textToCopy);
					p.StandardInput.Close();
					p.WaitForExit();
				}
			}
			catch (Exception ex)
			{
				ex.LogError ();
			}
		}

		/// <summary>
		/// Paste on MAC OS X using pbpaste commandline
		/// </summary>
		/// <returns></returns>
		public string GetText()
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
	}
}
