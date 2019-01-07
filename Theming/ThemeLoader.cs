using System;
using System.Linq;
using KS.Foundation;

namespace SummerGUI
{
	public class ThemeLoader
	{
		public string FileName { get; private set; }

		public ThemeLoader (string fname)
		{
			FileName = fname;
			LoadTheme ();
		}

		#pragma warning disable 219
		private void LoadTheme()
		{
			try {
				using (ConfigFile CFG = new ConfigFile (FileName)) {
					string color = CFG.GetSetting ("Colors", "Base03").SafeString();
					string version = CFG.GetSetting ("info", "version").SafeString();

					CFG.SetSetting ("info", "version", "1.0.5");
				}	
			} catch (Exception ex) {
				ex.LogError ();
				//MessageBox.ShowError(ex.Message, parent);
			}				
		}
	}
}

