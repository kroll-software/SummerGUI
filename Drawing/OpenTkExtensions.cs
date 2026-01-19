using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace SummerGUI
{
    [Flags]
    public enum RenderingFlags
    {
        None = 0,
        MultiSample = 1,
        Smooth = 2,
        HighQuality = MultiSample | Smooth
    }

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

		public static Rectangle GetScisorBounds()
		{
			// Array für 4 Integer (X, Y, Width, Height)
            int[] scissorBox = new int[4];
            GL.GetInteger(GetPName.ScissorBox, scissorBox);
            
			return new Rectangle(scissorBox[0], scissorBox[1], scissorBox[2], scissorBox[3]);

            // Status des Scissor-Tests prüfen
            //bool isScissorEnabled = GL.IsEnabled(EnableCap.ScissorTest);
		}

		// Vorsicht: Sehr langsam!
		public static Rectangle CurrentScissorBounds(this IGUIContext ctx)
		{
			int[] box = new int[4];
			GL.GetInteger(GetPName.ScissorBox, box);

			// OpenGL Box: [0]=X, [1]=Y (Bottom), [2]=Width, [3]=Height
			int x = box[0];
			int width = box[2];
			int height = box[3];

			// Umrechnung von Bottom-Up zu Top-Down:
			// Der Top-Wert in SummerGUI ist der Abstand von der Oberkante.
			// In OpenGL ist der 'Y' Wert der Abstand von der Unterkante zur Unterkante des Rechtecks.
			int top = ctx.Height - (box[1] + height);

			return new Rectangle(x, top, width, height);
		}

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

		public static float ScaleValue(this IGUIContext ctx, float value)
		{
			return value * ctx.ScaleFactor;
		}

		public static int ScaleValue(this IGUIContext ctx, int value)
		{
			return (int)(value * ctx.ScaleFactor + 0.5f);
		}		
	}
}

