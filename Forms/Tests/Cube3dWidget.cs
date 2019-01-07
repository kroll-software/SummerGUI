using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using KS.Foundation;


namespace SummerGUI
{
	
	public struct ImageStruct
	{
		public UInt32 Texture;
		public float rotationX;
		public float rotationY;
	}

	public class Cube3dWidgetStyle : WidgetStyle
	{
		public override void InitStyle ()
		{
			SetBackColor (Theme.Colors.Base01);
			SetForeColor (Theme.Colors.Base3);
			SetBorderColor (Color.Empty);
		}
	}

	public class Cube3dWidget : Widget
	{
		public TextureImage Image { get; private set; }

		public Cube3dWidget (string name)
			: base (name, Docking.Fill, new Cube3dWidgetStyle ())
		{
		}			

		private string m_ImageFilePath;
		public string ImageFilePath
		{
			get{
				return m_ImageFilePath;
			}
		}

		public void LoadTextureImageFromFile(string filepath, IGUIContext ctx)
		{			
			filepath = filepath.FixedExpandedPath ();

			if (m_ImageFilePath != filepath) {
				m_ImageFilePath = filepath;

				if (Image != null) {
					Image.Dispose ();
					Image = null;
				}

				if (String.IsNullOrEmpty (filepath) || ctx == null)
					return;

				try {					
					Image = TextureImage.FromFile(filepath, ctx);				
				} catch (Exception ex) {
					ex.LogError ();
				}
			}
		}

		protected virtual void StartScene()
		{
			GL.PushMatrix();
			GL.Enable (EnableCap.DepthTest);
			GL.Enable (EnableCap.Lighting);

			GL.Enable (EnableCap.Texture2D);
			GL.Enable(EnableCap.TextureRectangle);
			GL.Enable(EnableCap.AlphaTest);
			GL.Enable(EnableCap.Blend);
			GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

			/*** ***/
			//GL.ClearColor (0, 0, 0, 1);
			//GL.ClearColor (0.5f, 0.5f, 0.5f, 0);
			GL.ClearDepth(1);
			GL.DepthFunc (DepthFunction.Lequal);

			Vector4 ambient = new Vector4 (0.5f, 0.5f, 0.5f, 1f);
			Vector4 diffuse = new Vector4 (1f, 1f, 1f, 1f);
			Vector4 lightPosition = new Vector4 (-5, 5, 10, 0);

			GL.Light (LightName.Light0, LightParameter.Ambient, ambient);
			GL.Light (LightName.Light0, LightParameter.Diffuse, diffuse);
			GL.Light (LightName.Light0, LightParameter.Position, lightPosition);
			GL.Enable (EnableCap.Light0);


			//GL.MatrixMode (MatrixMode.Projection);
			GL.LoadIdentity ();
			GL.Frustum (-1, 1, -1, 1, 1.0, 10.0);
			GL.MatrixMode (MatrixMode.Modelview);
			GL.LoadIdentity ();
		}

		protected virtual void EndScene()
		{
			GL.PopMatrix ();
			GL.Disable (EnableCap.Lighting);
		}

		float rotationX = 0;
		float rotationY = 0;

		protected virtual void DrawScene()
		{
			//GL.Clear (ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
			GL.Clear (ClearBufferMask.DepthBufferBit);

			//GL.MatrixMode (MatrixMode.Modelview);
			//GL.LoadIdentity ();

			//GL.Enable (EnableCap.Normalize);
			//GL.Enable (EnableCap.Multisample);
			//GL.Enable (EnableCap.PrimitiveRestart);
			//GL.Enable (EnableCap.RescaleNormal);
			//GL.Enable (EnableCap.Texture1D);
			//GL.Enable (EnableCap.Texture3DExt);
			/***
			GL.Enable (EnableCap.Texture4DSgis);
			GL.Enable (EnableCap.TextureCoordArray);
			GL.Enable (EnableCap.TextureCubeMap);
			***/
			//GL.Enable (EnableCap.VertexArray);

			/***
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) TextureMinFilter.Linear);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) TextureMagFilter.Linear);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int) TextureWrapMode.ClampToEdge);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int) TextureWrapMode.ClampToEdge);
			***/

			GL.Translate (0, 0, -3.0);
			//GL.Translate (0, 0, 3.0);
			GL.Rotate (rotationX, 1, 0, 0);
			GL.Rotate (rotationY, 0, 1, 0);

			rotationX += 0.5f;
			rotationY += 0.5f;

			GL.BindTexture (TextureTarget.Texture2D, Image.TextureID);
			GL.Color4 (Color.White);

			GL.Begin (PrimitiveType.Quads);

			GL.Normal3 (0, 0, 1);
			GL.TexCoord2 (0, 0);
			GL.Vertex3 (-1, -1, 1);				

			GL.TexCoord2 (1, 0);
			GL.Vertex3(1, -1, 1);
			GL.TexCoord2(1, 1);
			GL.Vertex3(1, 1, 1);
			GL.TexCoord2(0, 1);
			GL.Vertex3(-1, 1, 1);

			GL.Normal3(0, 0, -1);
			GL.TexCoord2(1, 0);
			GL.Vertex3(-1, -1, -1);
			GL.TexCoord2(1, 1);
			GL.Vertex3(-1, 1, -1);
			GL.TexCoord2(0, 1);
			GL.Vertex3(1, 1, -1);
			GL.TexCoord2(0, 0);
			GL.Vertex3(1, -1, -1);

			GL.Normal3(0, 1, 0);
			GL.TexCoord2(0, 1);
			GL.Vertex3(-1, 1, -1);
			GL.TexCoord2(0, 0);
			GL.Vertex3(-1, 1, 1);
			GL.TexCoord2(1, 0);
			GL.Vertex3(1, 1, 1);
			GL.TexCoord2(1, 1);
			GL.Vertex3(1, 1, -1);

			GL.Normal3(0, -1, 0);
			GL.TexCoord2(1, 1);
			GL.Vertex3(-1, -1, -1);
			GL.TexCoord2(0, 1);
			GL.Vertex3(1, -1, -1);
			GL.TexCoord2(0, 0);
			GL.Vertex3(1, -1, 1);
			GL.TexCoord2(1, 0);
			GL.Vertex3(-1, -1, 1);

			GL.Normal3(1, 0, 0);
			GL.TexCoord2(1, 0);
			GL.Vertex3(1, -1, -1);
			GL.TexCoord2(1, 1);
			GL.Vertex3(1, 1, -1);
			GL.TexCoord2(0, 1);
			GL.Vertex3(1, 1, 1);
			GL.TexCoord2(0, 0);
			GL.Vertex3(1, -1, 1);

			GL.Normal3(-1, 0, 0);
			GL.TexCoord2(0, 0);
			GL.Vertex3(-1, -1, -1);
			GL.TexCoord2(1, 0);
			GL.Vertex3(-1, -1, 1);
			GL.TexCoord2(1, 1);
			GL.Vertex3(-1, 1, 1);
			GL.TexCoord2(0, 1);
			GL.Vertex3(-1, 1, -1);

			GL.End();
		}

		public override void OnLayout (IGUIContext ctx, RectangleF bounds)
		{
			base.OnLayout (ctx, bounds);
			Invalidate ();
		}

		public override void OnPaint (IGUIContext ctx, RectangleF bounds)
		{
			try {				
				StartScene ();
				DrawScene();
			} catch (Exception ex) {
				ex.LogError ();				
			}
			finally {
				EndScene ();
			}				
		}

		protected override void CleanupUnmanagedResources ()
		{
			if (Image != null)
				Image.Dispose ();
			base.CleanupUnmanagedResources ();
		}			
	}
}

