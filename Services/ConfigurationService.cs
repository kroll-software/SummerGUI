using System;
using System.Drawing;
using KS.Foundation;

namespace SummerGUI
{
	public class ConfigurationService : DisposableObject
	{		
		public static ConfigurationService Instance
		{
			get{
				return Singleton<ConfigurationService>.Instance;
			}
		}

		public ConfigFile ConfigFile { get; private set; }

		public void InitConfig (string filepath)
		{
			if (!String.IsNullOrEmpty (filepath)) {
				ConfigFile = new ConfigFile (filepath);
			}
		}

		public Size GetWindowSize(string windowName, Size defaultSize)
		{
			if (ConfigFile == null)
				return defaultSize;
						
			int w = ConfigFile.GetSetting (windowName, "Width").SafeInt (defaultSize.Width);
			int h = ConfigFile.GetSetting (windowName, "Height").SafeInt (defaultSize.Height);

			return new Size (w, h);
		}

		public ConfigurationService ()
		{
		}

		protected override void CleanupManagedResources ()
		{
			if (ConfigFile != null)
				ConfigFile.Dispose ();
			base.CleanupManagedResources ();
		}
	}
}

