using System;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace SummerGUI
{
	public static class OpenTkExtensions
	{
		/***
		private static Rectangle m_ClipRectangle = Rectangle.Empty;
		public static Rectangle ClipRectangle
		{
			get{
				return m_ClipRectangle;
			}
		}
		***/

		public static  Color4 ToRGBA(this Color4 color, float alpha)
		{
			return new Color4 (color.R, color.G, color.B, alpha);
		}

		public static  Color4 ToRGBA(this Color color)
		{
			return new Color4 (color.R, color.G, color.B, color.A);
		}

		public static  Color4 ToRGBA(this Color color, float alpha)
		{
			return new Color4 (color.R, color.G, color.B, alpha);
		}
			
		/***
		public static void SetClip(this IGUIContext ctx, Rectangle rect)
		{			
			if (rect.Width > 0 && rect.Height > 0)
			{
				GL.Enable (EnableCap.ScissorTest);
				GL.Scissor(rect.Left, ctx.Height - rect.Bottom, rect.Width, rect.Height);
			}
			else
			{
				GL.Scissor (0, 0, ctx.Width, ctx.Height);
				GL.Disable (EnableCap.ScissorTest);
			}
				
			m_ClipRectangle = rect;
			//System.Diagnostics.Debug.WriteLine(m_ClipRectangle.ToString(), "CLIP");
		}			

		public static void CombineClip(this IGUIContext ctx, Rectangle rect)
		{			
			if (m_ClipRectangle == Rectangle.Empty)
				m_ClipRectangle = new Rectangle (0, 0, ctx.Width, ctx.Height);

			rect.Intersect (m_ClipRectangle);
			SetClip (ctx, rect);
		}

		public static void ResetClip(this IGUIContext ctx)
		{			
			SetClip (ctx, Rectangle.Empty);
		}
		***/

		public static void SetDefaultRenderingOptions(this IGUIContext context)
		{
			SetDefaultRenderingOptions ();
		}			

		public static float ScaleValue(this IGUIContext ctx, float value)
		{
			return value * ctx.ScaleFactor;
		}

		public static int ScaleValue(this IGUIContext ctx, int value)
		{
			return (int)(value * ctx.ScaleFactor + 0.5f);
		}

		public static bool HighQuality = true;

		public static void SetDefaultRenderingOptions(bool highquality = true)
		{			
			//GL.Translate (0, 0, 0);

			GL.Disable(EnableCap.DepthTest);
			GL.Disable(EnableCap.CullFace);		
			//GL.Disable(EnableCap.LineStipple);

			GL.Disable(EnableCap.Texture2D);
			GL.Disable(EnableCap.Texture1D);
			GL.Disable(EnableCap.TextureRectangle);

			// Blending
			GL.Enable(EnableCap.AlphaTest);
			GL.Enable(EnableCap.Blend);
			GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            // high quality

            if (highquality)
            {
                GL.Enable(EnableCap.Multisample);
                GL.ShadeModel(ShadingModel.Smooth);

                GL.Enable(EnableCap.PointSmooth);
                GL.Hint(HintTarget.PointSmoothHint, HintMode.Nicest);

                GL.Enable(EnableCap.LineSmooth);
                GL.Hint(HintTarget.LineSmoothHint, HintMode.Nicest);

                GL.Enable(EnableCap.PolygonSmooth);
                GL.Hint(HintTarget.PolygonSmoothHint, HintMode.Nicest);
            }
            else
            {
                GL.Disable(EnableCap.PolygonSmooth);
            }
		}
	}
}

