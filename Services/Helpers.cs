using System;
using System.Diagnostics;
using KS.Foundation;

namespace SummerGUI
{
	public static class Helpers
	{
		public static bool MakeDir(string strPath, ApplicationWindow parent)
		{
			if (String.IsNullOrEmpty(strPath)) 
				return false;

			strPath = strPath.BackSlash(false);
			if (Strings.DirExists(strPath))
			{
				return true;
			}
			else
			{
				try
				{
					System.IO.Directory.CreateDirectory(strPath);
				}
				catch (Exception ex)
				{					
					ex.LogError ();
					if (parent != null)
						parent.ShowError (ex);
					return false;
				}

				return true;
			}
		}

		public static void OpenURL(string url, ApplicationWindow parent)
		{
			if (!String.IsNullOrEmpty(url))
			{
				try
				{
					Process.Start(url);
				}
				catch (Exception ex)
				{
					ex.LogError ();
					if (parent != null)
						parent.ShowError (ex);					
				}
			}
		}
	}
}

