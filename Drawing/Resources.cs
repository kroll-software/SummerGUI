using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using KS.Foundation;

namespace SummerGUI
{	
	public enum Cursors
	{
		Default,
		Wait,
		VSplit,
		HSplit,
		Text,
		Custom
	}

	public class WindowResourceManager : DisposableObject
	{		
		public WindowResourceManager()
		{
			DictCursors = new Dictionary<string, MouseCursor> ();
		}

		public void LoadWindowIcon(INativeWindow wnd, string pngPath)
		{
			if (String.IsNullOrEmpty (pngPath)) {
				this.LogWarning ("LoadWindowIcon: empty path specified.");
				return;
			}
			pngPath = pngPath.FixedExpandedPath ();

			try {				
				if (System.IO.File.Exists(pngPath))
				{
					// ToDo: Remove System.Drawing reference
					wnd.Icon = new System.Drawing.Icon(pngPath);
				} else {
					this.LogWarning ("LoadWindowIcon: path not found.");
				}
			} catch (Exception ex) {
				ex.LogError ();
			}
		}

		readonly Dictionary<string, MouseCursor> DictCursors;

		public void SetCursor(IGUIContext ctx, Cursors cursor)
		{
			SetCursor(ctx, cursor.ToString());
		}

		public void SetCursor(IGUIContext ctx, string name)
		{
            if (String.IsNullOrEmpty(name) || name == Cursors.Default.ToString())
            {
                ctx.GlWindow.Cursor = MouseCursor.Default;
            }
            else
            {
                MouseCursor cursor;
                if (DictCursors.TryGetValue(name, out cursor))                
                    ctx.GlWindow.Cursor = cursor;                
                else
                    DictCursors.LogWarning("Cursor not found: {0}", name);
            }

            // Workaround for Windows..
            PlatformExtensions.RefreshCursor();
		}

		public void LoadCursorFromFile(SummerGUIWindow wnd, string pngPath, Cursors cursor)
		{			
			LoadCursorFromFile (wnd, pngPath, cursor.ToString ());
		}

		// ToDo: Let user specify the cursor size.
		// ToDo: scale to 32x32 rather than 24x24
		// ToDo: how to scale cursors for HiRes?
		// ToDo: add more standard cursors to enum above (and to assets)

		public void LoadCursorFromFile(SummerGUIWindow wnd, string pngPath, string name)
		{			
			try {
				if (String.IsNullOrEmpty(name)) {
					this.LogWarning ("LoadCursorFromFile: invalid/empty name specified.");
				}

				if (DictCursors.ContainsKey(name)) {
					this.LogWarning("Cursor '{0}' was already loaded", name);
					return;
				}

				if (String.IsNullOrEmpty (pngPath)) {
					this.LogWarning ("LoadCursorFromFile: empty path specified.");
					return;
				}
				pngPath = pngPath.FixedExpandedPath ();

				if (System.IO.File.Exists(pngPath))
				{
					using (Bitmap bmp = new Bitmap (pngPath))
					using (Bitmap bmpScaled = new Bitmap (bmp, new Size(24, 24)))
					{						
						int w = bmpScaled.Width;
						int h = bmpScaled.Height;
						int len = w * h;

						//System.Drawing.Imaging.BitmapData data = bmp.LockBits(new Rectangle(0, 0, w, h), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

						byte[] bytes = new byte[len * 4];
						int bp = 0;

						for (int k = 0; k < len; k++)
						{
							int x = k % w;
							int y = k / w;

							/**
							Debug.Write(x, "X");
							Debug.Write(y, "\tY");
							Debug.WriteLine(k, "\tk");
							**/

							Color c = bmpScaled.GetPixel(x, y);						

							bytes[bp++] = (byte)c.B;
							bytes[bp++] = (byte)c.G;
							bytes[bp++] = (byte)c.R;
							bytes[bp++] = (byte)c.A;
						}
						//bmp.UnlockBits(data);

						DictCursors.Add(name, new MouseCursor(w / 2, h / 2, w, h, bytes));
					}
				} else {
					this.LogWarning ("LoadCursorFromFile: path not found.");
				}
			} catch (Exception ex) {
				ex.LogError ();
			}				
		}

		protected override void CleanupManagedResources ()
		{			
			DictCursors.Clear ();
			base.CleanupManagedResources ();
		}
	}
}

