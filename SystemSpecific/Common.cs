using System;

namespace SummerGUI.SystemSpecific
{
	public interface IClipboard
	{
		string GetText();
		void SetText(string text);
	}		
}

