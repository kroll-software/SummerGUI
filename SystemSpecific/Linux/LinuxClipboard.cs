using System;

namespace SummerGUI.SystemSpecific.Linux
{
	public class LinuxClipboard : IClipboard
	{
		private string Text;

		public string GetText ()
		{
			return Text;
		}

		public void SetText (string text)
		{
			Text = text;
		}
	}
}

