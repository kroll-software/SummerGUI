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
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Windowing.Desktop;
using OpenTK.Mathematics; // Für Vector2i (wenn für Image-Größe benötigt)
using OpenTK.Windowing.Common.Input;


using KS.Foundation;
using StbImageSharp; // Für ImageResult

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
		public static WindowResourceManager Manager
		{
			get {
				return Singleton<WindowResourceManager>.Instance;
			}
		}

		readonly Dictionary<string, MouseCursor> DictCursors;

		public WindowResourceManager()
		{
			DictCursors = new Dictionary<string, MouseCursor> ();
		}

		public void LoadWindowIcon(NativeWindow wnd, string pngPath)
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
					// wnd.Icon = new System.Drawing.Icon(pngPath);
				} else {
					this.LogWarning ("LoadWindowIcon: path not found.");
				}
			} catch (Exception ex) {
				ex.LogError ();
			}
		}		

		public void SetCursor(IGUIContext ctx, Cursors cursor)
		{
			SetCursor(ctx, cursor.ToString());
		}

		public void SetCursor(IGUIContext ctx, string name)
		{
            if (String.IsNullOrEmpty(name) || name == Cursors.Default.ToString())
            {
                //ctx.GlWindow.Cursor = Cursor.Default;
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

		public void LoadCursorFromFile(string pngPath, Cursors cursor)
		{			
			LoadCursorFromFile (pngPath, cursor.ToString ());
		}

		// ToDo: Let user specify the cursor size.
		// ToDo: scale to 32x32 rather than 24x24
		// ToDo: how to scale cursors for HiRes?
		// ToDo: add more standard cursors to enum above (and to assets)		

		// Fügen Sie das 'unsafe' Schlüsselwort zur Methodensignatur hinzu
		public unsafe void LoadCursorFromFile(string pngPath, string name)
		{
			try {
				if (String.IsNullOrEmpty(name)) {
					this.LogWarning("LoadCursorFromFile: invalid/empty name specified.");
					return;
				}

				if (DictCursors.ContainsKey(name))
				{
					//this.LogWarning($"Cursor '{name}' was already loaded.");
					return;
				}

				if (String.IsNullOrEmpty(pngPath)) {
					this.LogWarning("LoadCursorFromFile: empty path specified.");
					return;
				}

				pngPath = pngPath.FixedExpandedPath();
				
				if (System.IO.File.Exists(pngPath))
				{
					using (Stream stream = File.OpenRead(pngPath))
					{
						ImageResult image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
						
						int originalWidth = image.Width;
						int originalHeight = image.Height;
						byte[] originalBytes = image.Data;

						int targetWidth = 24;
						int targetHeight = 24;
						byte[] scaledBytes;

						if (originalWidth == targetWidth && originalHeight == targetHeight)
						{
							scaledBytes = originalBytes;
						}
						else
						{
							// Verwenden Sie die neue UnsafeImageScaler Klasse
							scaledBytes = ImageScaler.ScaleImageData(
								originalBytes, 
								originalWidth, 
								originalHeight, 
								targetWidth, 
								targetHeight
							);
						}

						MouseCursor cursor = new MouseCursor(
							targetWidth / 2, // Hotspot X
							targetHeight / 2, // Hotspot Y
							targetWidth,
							targetHeight,
							scaledBytes
						);

						DictCursors.Add(name, cursor);
					}
				} else {
					this.LogWarning("LoadCursorFromFile: path not found.");
				}
			} catch (Exception ex) {
				ex.LogError();
			}				
		}


		protected override void CleanupManagedResources ()
		{			
			DictCursors.Clear ();
			base.CleanupManagedResources ();
		}
	}
}

