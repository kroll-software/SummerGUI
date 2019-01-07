using System;

namespace SummerGUI.SystemSpecific.Windows
{
	public class WindowsClipboard : IClipboard
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

