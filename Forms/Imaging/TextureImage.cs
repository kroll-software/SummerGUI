using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Drawing;
using StbImageSharp;
//using System.Drawing.Imaging;
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
		public Size Size { get; private set; } // System.Drawing.Size

		//public BlendModes BlendMode { get; set; }
		public float Opacity { get; set; }
		public Color BlendColor { get; set; } // Auskommentiert, da Color-Typ unklar

		public TextureImage(string key)
		{
			Key = key;
			TextureID = -1;
			Opacity = 1;
			BlendColor = Color.FromArgb (255, 255, 255, 255);
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
		// System.Drawing.Imaging.PixelFormat PixFormat wird nicht mehr benötigt

		// FromFile lädt das Bild und ruft dann die interne Ladefunktion auf
		public static TextureImage FromFile(string filePath, IGUIContext ctx, string key = null, Size size = default(Size))
		{
			if (String.IsNullOrEmpty (filePath))
				throw new ArgumentException ("filePath must not be null.");

			if (!File.Exists (filePath))
				throw new ArgumentException ("{0} does not exist", filePath);

			if (String.IsNullOrEmpty (key)) {
				// Ihre Pfad-Logik bleibt unberührt, da sie String-basiert ist
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
				// Verwenden Sie StbImageSharp direkt, keine Bitmap(filePath) Instanziierung
				return LoadTextureFromFile(filePath, ctx, key, size);
			} catch (Exception ex) {
				ex.LogError ();
				throw;
			}
		}
		
		// FromBitmap und LoadTexture(Bitmap bmp...) werden durch eine einzige, neue Methode ersetzt:
		public static TextureImage LoadTextureFromFile(string filePath, IGUIContext ctx, string key, Size size = default(Size))
		{
			try
			{
				// --- Bild mit StbImageSharp laden ---
				using (Stream stream = File.OpenRead(filePath))
				{
					// Fordert RGBA (4 Komponenten) an, was für OpenGL am besten ist
					ImageResult image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

					if (size != Size.Empty && (size.Width != image.Width || size.Height != image.Height))
                    {
                        var scaledBytes = ImageScaler.ScaleImageData(image.Data, image.Width, image.Height, size.Width, size.Height);
						image.Data = scaledBytes;
						image.Width = size.Width;
						image.Height = size.Height;
                    }

					TextureImage retVal = new TextureImage(key);
					retVal.TextureID = UploadTextureToOpenGL(image, ctx);
					retVal.Size = new Size(image.Width, image.Height);
					// BPP und PixelFormat werden nicht mehr explizit gesetzt

					return retVal;
				}
			}
			catch (Exception ex)
			{
				ex.LogError();
				throw;
			}
		}

		// Die neue Methode, die das Bild an OpenGL übergibt
		static int UploadTextureToOpenGL(ImageResult image, IGUIContext ctx, int id = -1)
		{	
			using (new PaintWrapper(RenderingFlags.HighQuality)) 
			{				
				GL.Hint(HintTarget.PerspectiveCorrectionHint, HintMode.Nicest);
				GL.Enable(EnableCap.Texture2D);

				if (id < 0)
					id = GL.GenTexture();
				GL.BindTexture(TextureTarget.Texture2D, id);

				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

				// --- Datenübergabe an OpenGL (kein LockBits mehr nötig) ---
				
				// image.Data ist bereits ein byte[] Array mit RGBA Bytes
				byte[] pixels = image.Data;

				GL.TexImage2D(
					TextureTarget.Texture2D, 
					0, 
					PixelInternalFormat.Rgba, 
					image.Width, 
					image.Height, 
					0,
					// Format ist RGBA, Typ ist Unsigned Byte
					OpenTK.Graphics.OpenGL.PixelFormat.Rgba, 
					PixelType.UnsignedByte, 
					pixels 
				);
				
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

			using (new PaintWrapper (RenderingFlags.HighQuality)) {
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

