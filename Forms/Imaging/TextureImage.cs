using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using KS.Foundation;
using SummerGUI;

namespace SummerGUI
{
	/***
	public enum BlendModes
	{
		Default,
		Multiply
	}
	***/

	public class TextureImage : DisposableObject
	{				
		public int TextureID  { get; private set; }
		public string URI { get; private set; }
		public string Key { get; private set; }
		public Size Size { get; private set; }

		//public BlendModes BlendMode { get; set; }
		public float Opacity { get; set; }
		public Color BlendColor { get; set; }

		public TextureImage(string key)
		{
			Key = key;
			TextureID = -1;
			Opacity = 1;
			//BlendColor = Color.FromArgb (255, 255, 255, 255);
		}

		public int Width 
		{ 
			get{
				return Size.Width;
			}
		}

		public int Height 
		{ 
			get{
				return Size.Height;
			}
		}

		public byte BPP { get; private set; }
		public System.Drawing.Imaging.PixelFormat PixFormat  { get; private set; }

		public static TextureImage FromFile(string filePath, IGUIContext ctx, string key = null, Size size = default(Size))
		{
			if (String.IsNullOrEmpty (filePath))
				throw new ArgumentException ("filePath");

			if (!File.Exists (filePath))
				throw new ArgumentException ("{0} does not exist", filePath);

			if (String.IsNullOrEmpty (key)) {
				// set key to relative Path below /assets with unix filenames
				if (filePath.IndexOf ("Assets") < 0) {
					key = filePath;
				}
				else {
					string appPath = Strings.ApplicationPath (true) + "Assets";
					int extLen = Path.GetExtension (filePath).Length;
					key = Path.GetFullPath (filePath).StrMid(appPath.Length + 2, filePath.Length - appPath.Length - 1 - extLen);
					if (String.IsNullOrEmpty (key)) {
						key = filePath;
					}
					if (Path.DirectorySeparatorChar != '/') {
						Strings.Replace (key, Path.DirectorySeparatorChar.ToString (), "/");
					}
				}
			}

			try {
				using (Bitmap bmp = new Bitmap(filePath))
				{
					return FromBitmap(bmp, ctx, key, size);
				}				
			} catch (Exception ex) {
				ex.LogError ();
				throw;
			}
		}			

		public static TextureImage FromBitmap(Bitmap bmp, IGUIContext ctx, string key, Size size = default(Size))
		{
			if (bmp == null || bmp.Width < 1 || bmp.Height < 1)
				return null;

			try {
				TextureImage retVal = new TextureImage(key);
				if (size != Size.Empty && size != bmp.Size) {
					using (Bitmap bmpScaled = new Bitmap (bmp, size))
					{
						retVal.TextureID = LoadTexture(bmpScaled, ctx);
						retVal.Size = bmpScaled.Size;
						retVal.PixFormat = bmpScaled.PixelFormat;
					}
				} else {
					retVal.TextureID = LoadTexture(bmp, ctx);
					retVal.Size = bmp.Size;
					retVal.PixFormat = bmp.PixelFormat;
				}
				return retVal;
			} catch (Exception ex) {
				ex.LogError ();
				throw;
			}
		}			

		static int LoadTexture(Bitmap bmp, IGUIContext ctx, int id = -1)
		{	
			using (new PaintWrapper (null)) {				
				GL.Hint(HintTarget.PerspectiveCorrectionHint, HintMode.Nicest);
				GL.Enable (EnableCap.Texture2D);

				if (id < 0)
					id = GL.GenTexture ();
				GL.BindTexture (TextureTarget.Texture2D, id);

				// We will not upload mipmaps, so disable mipmapping (otherwise the texture will not appear).
				// We can use GL.GenerateMipmaps() or GL.Ext.GenerateMipmaps() to create
				// mipmaps automatically. In that case, use TextureMinFilter.LinearMipmapLinear to enable them.
				GL.TexParameter (TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
				GL.TexParameter (TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

				BitmapData data = null;
				try {
					data = bmp.LockBits (new Rectangle (0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
					GL.TexImage2D (TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0,
						OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
				} catch (Exception ex) {
					ex.LogError ();
				} finally {
					bmp.UnlockBits (data);
				}					

				return id;
			}				
		}			

		public void Unload()
		{
			if (TextureID < 0)
				return;

			try {
				GL.BindTexture (TextureTarget.Texture2D, 0);
				GL.DeleteTexture (TextureID);	
			} catch (Exception ex) {	
				ex.LogError ();
			}
			finally {
				TextureID = -1;
				this.Size = Size.Empty;
			}
		}

		protected override void CleanupManagedResources ()
		{
			Unload ();
			base.CleanupManagedResources ();
		}
	}

	public static class TextureImageExtensions
	{		
		public static void Paint(this TextureImage image, IGUIContext ctx, RectangleF dest)
		{
			image.Paint (ctx, Rectangle.Round (dest));
		}	

		public static void Paint(this TextureImage image, IGUIContext ctx, Rectangle dest)
		{
			if (image.IsDisposed || image.TextureID < 0) {
				image.LogWarning ("Image is not loaded (has no TextureID).");
				return;
			}

			if (dest.Width < 0 || dest.Height < 0 || !ctx.ClipBoundStack.IsOnScreen (dest))
				return;

			using (new PaintWrapper (null)) {
				GL.Enable(EnableCap.Blend);
				BlendingFactor srcBlend = BlendingFactor.SrcAlpha;
				BlendingFactor destBlend = BlendingFactor.OneMinusSrcAlpha;
				BlendEquationMode mode = BlendEquationMode.FuncAdd;

				if (image.Opacity < 1) {
					// Multiply
					srcBlend = BlendingFactor.DstColor;
					destBlend = BlendingFactor.OneMinusSrcAlpha;
					GL.Color4 (image.Opacity, image.Opacity, image.Opacity, image.Opacity);
				} else {
					// mandatory here, 
					// but take care not to forget the f for float
					// otherwise there is a dangreous overloading with uint
					// which would set a very different value
					if (image.BlendColor != Color.Empty)
						GL.Color4(image.BlendColor);
					else
						GL.Color4 (1f, 1f, 1f, 1f);
				}


				GL.BlendEquation(mode);
				GL.BlendFunc(srcBlend, destBlend);

				GL.Enable (EnableCap.Texture2D);
				GL.BindTexture (TextureTarget.Texture2D, image.TextureID);

				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) TextureMinFilter.Linear);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) TextureMagFilter.Linear);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int) TextureWrapMode.ClampToEdge);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int) TextureWrapMode.ClampToEdge);

				GL.Begin (PrimitiveType.Quads);

				GL.TexCoord2(0, 0);
				GL.Vertex3 (dest.X, dest.Y, 0);

				GL.TexCoord2(1, 0);
				GL.Vertex3 (dest.X + dest.Width, dest.Y, 0);

				GL.TexCoord2(1, 1);
				GL.Vertex3 (dest.X + dest.Width, dest.Y + dest.Height, 0);

				GL.TexCoord2(0, 1);
				GL.Vertex3 (dest.X, dest.Y + dest.Height, 0);

				GL.End ();
			};
		}
	}
}

