using System;
using System.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Diagnostics;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace SummerGUI
{	
	public static class SystemBrushes
	{
		static SystemBrushes()
		{
			WindowText = new SolidBrush (Color.Black);
		}

		public static SolidBrush WindowText { get; private set; }
	}		
		
	public struct PaintWrapper : IDisposable
	{
		// Ensure to always call it with a parameter
		// e.g. new PaintWrapper(null)
		public PaintWrapper(RenderingFlags flags = RenderingFlags.HighQuality)
		{		
			GL.PushMatrix();
			GL.LoadIdentity();
			OpenTkExtensions.SetDefaultRenderingOptions (flags);
		}

		public void Dispose()
		{
			GL.PopMatrix ();
			GC.SuppressFinalize (this);
		}
			
		public override bool Equals (object obj)
		{
			return false;
		}

		public override int GetHashCode ()
		{
			return 0;
		}
	}

	public static class GUIDrawing
	{				
		// Convert back and forth between System.Drawing.Color and OpenGL.Color4

		public static Color ToColor(this Color4 color4)
		{
			return Color.FromArgb ((int)color4.A, (int)color4.R, (int)color4.G, (int)color4.B);
		}

		public static Color4 ToColor4(this Color color)
		{
			return new Color4 (color.R / 255, color.G / 255, color.B / 255, color.A / 255);
		}			

		// ******************************************
		// LINES

		public static void DrawLine(this IGUIContext ctx, Pen pen, float x1, float y1, float x2, float y2)
		{
			using (new PaintWrapper (RenderingFlags.HighQuality)) {
				pen.DoGL ();
					
				if (Math.Abs(y1 - y2) < float.Epsilon) {
					y1 -= 0.5f;
					y2 = y1;
				} else if (Math.Abs(x1 - x2) < float.Epsilon)  {
					x1 -= 0.5f;
					x2 = x1;
				}

				GL.Color4 (pen.Color);
				GL.LineWidth (pen.Width);
				GL.Begin (PrimitiveType.Lines);
				GL.Vertex2 (x2, y2);
				GL.Vertex2 (x1, y1);
				GL.End ();
				pen.UndoGL ();
			}
		}

		public static void DrawThickLine(this IGUIContext ctx, Pen pen, int x1, int y1, int x2, int y2) {

			Vector2 start = new Vector2(x1, y1);
			Vector2 end = new Vector2(x2, y2);

			float dx = x1 - x2;
			float dy = y1 - y2;

			float halfwidth = pen.Width / 2;

			Vector2 rightSide = new Vector2(dy, -dx);
			if (rightSide.Length > 0) {
				rightSide.Normalize();
				Vector2.Multiply (rightSide, halfwidth);
			}
			Vector2 leftSide = new Vector2(-dy, dx);
			if (leftSide.Length > 0) {
				leftSide.Normalize();
				Vector2.Multiply (leftSide, halfwidth);
			}

			Vector2 one = new Vector2();
			Vector2.Add(in leftSide, in start, out one);

			Vector2 two = new Vector2();
			Vector2.Add(in rightSide, in start, out two);

			Vector2 three = new Vector2();
			Vector2.Add(in rightSide, in end, out three);

			Vector2 four = new Vector2();
			Vector2.Add(in leftSide, in end, out four);

			using (new PaintWrapper (RenderingFlags.HighQuality)) {
				pen.DoGL ();
				GL.Color4 (pen.Color.R, pen.Color.G, pen.Color.B, pen.Color.A);
				GL.Begin (PrimitiveType.Quads);
				GL.Vertex3 (one.X, one.Y, 0);
				GL.Vertex3 (two.X, two.Y, 0);
				GL.Vertex3 (three.X, three.Y, 0);
				GL.Vertex3 (four.X, four.Y, 0);				
				GL.End ();
				pen.UndoGL ();
			}
		}			

		public static SizeF DrawString(this IGUIContext ctx, string text, string fontTag, Brush brush, PointF point, FontFormat format)
		{
			Color c = Color.Empty;
			if (brush != null)
				c = brush.Color;			
			return FontManager.Manager.Print (ctx, text, fontTag.ToString(), 
				new RectangleF (point, SizeF.Empty), format, c);
		}

		public static SizeF DrawString(this IGUIContext ctx, string text, IGUIFont font, Brush brush, PointF point, FontFormat format)
		{
			return DrawString (ctx, text, font, brush, point.X, point.Y, format);
		}
			
		public static SizeF DrawString(this IGUIContext ctx, string text, IGUIFont font, Brush brush, float x, float y, FontFormat format)
		{
			if (ctx == null)
				return SizeF.Empty;

			SizeF contentSize = font.Measure(text);

			switch (format.HAlign) {
			case Alignment.Near:
				break;
			case Alignment.Center:
				x -= contentSize.Width / 2f;
				break;
			case Alignment.Far:
				x -= contentSize.Width;
				break;
			}

			// ToDo: Was soll das hier noch ?
			switch (format.VAlign) {
			case Alignment.Near:
				y -= contentSize.Height / 2f;
				break;
			case Alignment.Center:
				y += contentSize.Height / 2f;
				break;
			case Alignment.Far:
			case Alignment.Baseline:
				y += contentSize.Height / 2;
				break;
			}
				
			SizeF retVal;
			font.Begin();
			Color c = Color.Empty;
			if (brush != null)
				c = brush.Color;
			retVal = font.Print(text, new RectangleF (x, y, contentSize.Width, contentSize.Height), format, c);
			font.End();
			return retVal;
		}
		/*** ***/

		public static SizeF DrawString(this IGUIContext ctx, string text, string fontTag, Color color, RectangleF bounds, FontFormat format)
		{
			return FontManager.Manager.Print (ctx, fontTag, text, bounds, format, color);
		}
			
		public static SizeF DrawString(this IGUIContext ctx, string text, IGUIFont font, Brush brush, RectangleF rect, FontFormat format)
		{
			if (ctx == null || text == null)
				return SizeF.Empty;

			SizeF retVal;

			Color c = Color.Empty;
			if (brush != null)
				c = brush.Color;

			font.Begin();
			retVal = font.Print(text, rect, format, c);
			font.End();
			return retVal;
		}


		public static SizeF DrawSelectedString(this IGUIContext ctx, string text, string fontTag, int selStart, int selLength, RectangleF bounds, float offsetX, FontFormat format, Color foreColor, Color selectionBackColor, Color selectionForeColor)
		{
			return FontManager.Manager.PrintSelectedString (ctx, fontTag, text, selStart, selLength, bounds, offsetX, format, foreColor, selectionBackColor, selectionForeColor);
		}

		public static SizeF DrawSelectedString(this IGUIContext ctx, string text, IGUIFont font, int selStart, int selLength, RectangleF rect, float offsetX, FontFormat format, Color foreColor, Color selectionBackColor, Color selectionForeColor)
		{
			if (ctx == null || text == null)
				return SizeF.Empty;

			SizeF retVal;
			font.Begin();
			retVal = font.PrintSelectedString(text, selStart, selLength, rect, offsetX, format, foreColor, selectionBackColor, selectionForeColor);
			font.End();
			return retVal;
		}

		public static SizeF MeasureString(this IGUIContext ctx, string text, string fontTag, int start = 0, int len = -1)
		{
			return FontManager.Manager.Measure (fontTag, text, start, len);
		}

		public static SizeF MeasureString(this IGUIContext ctx, string text, string fontTag, RectangleF rect, FontFormat format)
		{
			return FontManager.Manager.Measure (fontTag, text, rect.Width, format);
		}			

		public static SizeF MeasureString(this IGUIContext ctx, string text, IGUIFont font, RectangleF rect, FontFormat format)
		{
			return font.Measure (text, rect.Width, format);
		}

		public static SizeF MeasureMnemonicString(this IGUIContext ctx, string text, string fontTag)
		{
			return FontManager.Manager.MeasureMnemonicString (fontTag, text);
		}

		// ******************************************

		public static void DrawPolygon(this IGUIContext ctx, Pen pen, PointF[] points)
		{
			// Sicherheitsprüfung: Stellen Sie sicher, dass Punkte vorhanden sind und es mindestens 3 Punkte gibt
			if (points == null || points.Length < 3)
				return;

			// Verwenden Sie PaintWrapper, um den Zustand zu sichern/wiederherzustellen
			using (new PaintWrapper(RenderingFlags.HighQuality)) 
			{
				GL.Color4(pen.Color);

				// Optional: Hinzufügen der Eckpunkt-Logik für dickere Linien
				if (pen.Width > 2) {
					GL.PointSize (pen.Width / 2f + 0.5f);
					GL.Begin (PrimitiveType.Points);
					foreach (PointF p in points) {
						GL.Vertex2 (p.X, p.Y);
					}
					GL.End ();
				}				
				
				// 1. Anwendung der Pen-Einstellungen (LineWidth und möglicherweise Stippling/Dashing)
				GL.LineWidth(pen.Width);
				pen.DoGL(); // Wenden Sie zusätzliche GL-Einstellungen an, falls in pen.DoGL definiert

				// 2. Zeichnen der Eckpunkte
				// Wir verwenden PrimitiveType.LineLoop, um die Linien zu zeichnen und 
				// den letzten Punkt automatisch mit dem ersten zu verbinden.
				GL.Begin(PrimitiveType.LineLoop);
				
				foreach (PointF p in points) 
				{
					GL.Vertex2(p.X, p.Y);
				}
				
				GL.End();
				
				pen.UndoGL(); // Entfernen Sie zusätzliche GL-Einstellungen
			}
		}

		public static void FillPolygon(this IGUIContext ctx, Brush brush, PointF[] points)
		{
			if (points == null || points.Length == 0)
				return;

			using (new PaintWrapper(RenderingFlags.HighQuality)) {
				GL.Color4 (brush.Color);
				GL.Begin (PrimitiveType.Polygon);
				foreach (PointF p in points) {
					GL.Vertex2 (p.X, p.Y);
				}
				GL.End ();
			}
		}

		public static void FillRectangle(this IGUIContext ctx, Brush brush, RectangleF r)
		{
			if (brush as SolidBrush != null)
				FillRectangle (ctx, brush as SolidBrush, r);
			else if (brush as LinearGradientBrush != null)
				FillRectangle (ctx, brush as LinearGradientBrush, r);
			else if (brush as HatchBrush != null)
				FillRectangle (ctx, brush as HatchBrush, r);			
		}

		public static void FillRectangle(this IGUIContext ctx, HatchBrush brush, RectangleF r)
		{
			using (new PaintWrapper(RenderingFlags.HighQuality)) {
				GL.Color4 (brush.Color);
				GL.Rect (r);
			}
		}

		public static void FillRectangle(this IGUIContext ctx, LinearGradientBrush brush, int x, int y, int width, int height)
		{
			FillRectangle (ctx, brush, new Rectangle (x, y, width, height));
		}

		public static void FillRectangle(this IGUIContext ctx, LinearGradientBrush brush, RectangleF r)
		{
			switch (brush.Direction) 
			{
			case GradientDirections.Horizontal:
				FillRectangleHorizontal (ctx, brush, r);
				break;

			case GradientDirections.Vertical:
				FillRectangleVertical (ctx, brush, r);
				break;

			case GradientDirections.TopLeft:
				FillRectangleTopLeft (ctx, brush, r);
				break;

			case GradientDirections.ForwardDiagonal:
				FillRectangleForwardDiagonal (ctx, brush, r);
				break;

			case GradientDirections.BackwardDiagonal:
				FillRectangleBackwardDiagonal (ctx, brush, r);
				break;							
			}
		}

		private static void FillRectangleHorizontal(this IGUIContext ctx, LinearGradientBrush brush, RectangleF r)
		{
			using (new PaintWrapper(RenderingFlags.None)) {
				GL.Color4 (brush.Color);
				GL.Begin (PrimitiveType.Polygon);
				GL.Vertex2 (r.Left, r.Bottom);
				GL.Color4 (brush.GradientColor);
				GL.Vertex2 (r.Right, r.Bottom);
				GL.Vertex2 (r.Right, r.Top);
				GL.Color4 (brush.Color);
				GL.Vertex2 (r.Left, r.Top);
				GL.End ();
			}
		}
			
		private static void FillRectangleVertical(this IGUIContext ctx, LinearGradientBrush brush, RectangleF r)
		{
			using (new PaintWrapper(RenderingFlags.None)) {
				GL.Color4 (brush.GradientColor);
				GL.Begin (PrimitiveType.Polygon);
				GL.Vertex2 (r.Left, r.Bottom);
				GL.Vertex2 (r.Right, r.Bottom);
				GL.Color4 (brush.Color);
				GL.Vertex2 (r.Right, r.Top);
				GL.Vertex2 (r.Left, r.Top);
				GL.End ();
			}
		}

		private static void FillRectangleTopLeft(this IGUIContext ctx, LinearGradientBrush brush, RectangleF r)
		{	
			using (new PaintWrapper(RenderingFlags.None)) {
				GL.Begin (PrimitiveType.Polygon);
				GL.Color4 (brush.Color);
				GL.Vertex2 (r.Left, r.Bottom);
				GL.Vertex2 (r.Right, r.Bottom);
				GL.Vertex2 (r.Right, r.Top);
				GL.Color4 (brush.GradientColor);
				GL.Vertex2 (r.Left, r.Top);
				GL.End ();
			}
		}

		private static void FillRectangleForwardDiagonal(this IGUIContext ctx, LinearGradientBrush brush, RectangleF r)
		{
			using (new PaintWrapper(RenderingFlags.None)) {
				GL.Color4 (brush.Color);
				GL.Begin (PrimitiveType.Polygon);
				GL.Vertex2 (r.Left, r.Bottom);
				GL.Color4 (brush.GradientColor);
				GL.Vertex2 (r.Right, r.Bottom);
				GL.Vertex2 (r.Right, r.Top);
				GL.Vertex2 (r.Left, r.Top);
				GL.End ();		
			}
		}

		private static void FillRectangleBackwardDiagonal(this IGUIContext ctx, LinearGradientBrush brush, RectangleF r)
		{	
			using (new PaintWrapper(RenderingFlags.None)) {
				GL.Color4 (brush.GradientColor);
				GL.Begin (PrimitiveType.Polygon);
				GL.Vertex2 (r.Left, r.Bottom);
				GL.Vertex2 (r.Right, r.Bottom);
				GL.Vertex2 (r.Right, r.Top);
				GL.Color4 (brush.Color);
				GL.Vertex2 (r.Left, r.Top);
				GL.End ();
			}
		}

		// *** FillRectangle

		public static void StartRendering()
		{				
			GL.PushMatrix ();
			GL.LoadIdentity ();
			OpenTkExtensions.SetDefaultRenderingOptions ();
		}

		public static void EndRendering()
		{
			GL.PopMatrix ();
		}


		public static void FillRectangle(this IGUIContext ctx, SolidBrush brush, RectangleF r)
		{
			if (r.Width < 0 || r.Height < 0)
				return;

			using (new PaintWrapper (RenderingFlags.None)) {
                GL.Color4 (brush.Color);
				GL.Rect (r);
            }
		}

		public static void FillRectangle(this IGUIContext ctx, Brush brush, RectangleF r, float alpha)
		{			
			if (r.Width < 0 || r.Height < 0)
				return;

			using (new PaintWrapper (RenderingFlags.None)) {
				GL.Color4 (brush.Color.ToRGBA (alpha));
				GL.Rect (r);
			}
		}

		public static void FillRectangle(this IGUIContext ctx, Brush brush, int x, int y, int width, int height)
		{
			if (width < 0 || height < 0)
				return;

			using (new PaintWrapper (RenderingFlags.None)) {
				GL.Color4 (brush.Color);
				GL.Rect (x, y, x + width, y + height);
			}
		}
			
		public static void FillRectangle(this IGUIContext ctx, Brush brush, float x, float y, float width, float height)
		{
			if (width < 0 || height < 0)
				return;

			using (new PaintWrapper (RenderingFlags.None)) {
				GL.Color4 (brush.Color);		
				GL.Rect (x, y, x + width, y + height);
			}
		}

		// *** DrawRectangle

		public static void DrawRectangle(this IGUIContext ctx, Pen pen, int x, int y, int width, int height)
		{			
			DrawRectangle (ctx, pen, new Rectangle (x, y, width, height));
		}

		public static void DrawRectangle(this IGUIContext ctx, Pen pen, float x, float y, float width, float height)
		{			
			DrawRectangle (ctx, pen, new RectangleF (x, y, width, height));
		}
			
		public static void DrawRectangle(this IGUIContext ctx, Pen pen, RectangleF r)
		{	
			if (r.Width < 0 || r.Height < 0)
				return;

			/***
			float hw = pen.Width / 2f;
			r.X += hw;
			r.Y += hw;
			r.Width -= pen.Width;
			r.Height -= pen.Width;
			***/

			using (new PaintWrapper (RenderingFlags.HighQuality)) {
				GL.Color4 (pen.Color);

				if (pen.Width > 2) {
					GL.PointSize (pen.Width / 2f + 0.5f);
					GL.Begin (PrimitiveType.Points);
					GL.Vertex2 (r.Left, r.Bottom);
					GL.Vertex2 (r.Right, r.Bottom);
					GL.Vertex2 (r.Right, r.Top);
					GL.Vertex2 (r.Left, r.Top);
					GL.End ();
				}

				GL.LineWidth (pen.Width);
				pen.DoGL ();
				GL.Begin (PrimitiveType.LineLoop);
				GL.Vertex2 (r.Left, r.Bottom);
				GL.Vertex2 (r.Right, r.Bottom);
				GL.Vertex2 (r.Right, r.Top);
				GL.Vertex2 (r.Left, r.Top);		
				GL.End ();
				pen.UndoGL ();
			}
		}


		// ******************************************
		// CIRCLE

		public static void DrawCircle(this IGUIContext ctx, Pen pen, float cx, float cy, float radius, float scale = 1)
		{
			ctx.DrawEllipse (pen, cx, cy, radius, radius, scale);
		}

		public static void FillCircle(this IGUIContext ctx, Brush brush, float cx, float cy, float radius, float scale = 1)
		{			
			ctx.FillEllipse (brush, cx, cy, radius, radius, scale);
		}

		//static readonly double Tau = Math.PI * 2d;
		static readonly float Tau = (float)(Math.PI * 2d);

		public static void DrawEllipse(this IGUIContext ctx, Pen pen, float cx, float cy, float radiusX, float radiusY, float scale = 1)
		{
			float r = (Math.Abs(radiusX) + Math.Abs(radiusY)) / 2f;
			float da = (float)(Math.Acos(r / (r + 0.125f)) * 2f / scale);
			int numSteps = (int)Math.Round(Tau / da);

			if (numSteps < 1 || da < float.Epsilon)
				return;

			using (new PaintWrapper (RenderingFlags.HighQuality)) {	
				GL.Color4 (pen.Color);
				pen.DoGL ();
				GL.LineWidth (pen.Width);
				GL.Begin (PrimitiveType.LineLoop);
				float angle = 0;
				for (int i = 0; i < numSteps; i++)
				{					
					GL.Vertex2((Math.Cos(angle) * radiusX) + cx, (Math.Sin(angle) * radiusY) + cy);
					angle += da;
				}
				GL.Vertex2 ((radiusY * Math.Cos (0)) + cx, (radiusX * Math.Sin (0)) + cy);
				GL.End ();
				pen.UndoGL ();
			}
		}

		public static void FillEllipse(this IGUIContext ctx, Brush brush, float cx, float cy, float radiusX, float radiusY, float scale = 1)
		{
			float r = (Math.Abs(radiusX) + Math.Abs(radiusY)) / 2f;
			float da = (float)(Math.Acos(r / (r + 0.125f)) * 2f / scale);
			int numSteps = (int)Math.Round(Tau / da);

			if (numSteps < 1 || da < float.Epsilon)
				return;

			using (new PaintWrapper (RenderingFlags.None)) {
				GL.Color4 (brush.Color);
				GL.Begin (PrimitiveType.TriangleFan);
				float angle = 0;
				for (int i = 0; i < numSteps; i++)
				{										
					GL.Vertex2((radiusX * Math.Cos(angle)) + cx, (radiusY * Math.Sin(angle)) + cy);
					angle += da;
				}
				GL.Vertex2 ((radiusY * Math.Cos (0)) + cx, (radiusX * Math.Sin (0)) + cy);
				GL.End ();
			}
		}			

		public static void DrawPie(this IGUIContext ctx, Pen pen, RectangleF rec, float startAngle, float sweepAngle)
		{
			DrawPie (ctx, pen, rec.X + (rec.Width / 2f), rec.Y + (rec.Height / 2f), rec.Width / 2f, rec.Height / 2f, startAngle, sweepAngle);
		}

		public static void DrawPie(this IGUIContext ctx, Pen pen, float x, float y, float radiusX, float radiusY, float startAngle, float sweepAngle)
		{			
			float r = (Math.Abs(radiusX) + Math.Abs(radiusY)) / 2f;
			float segratio = (sweepAngle / 360f);
			float da = (float)Math.Acos(r / (r + 0.125f)) * 2f * segratio;
			int numSteps = (int)((Tau / da) * segratio + 0.5f) + 1;

			if (numSteps < 1 || da < float.Epsilon)
				return;

			startAngle -= 90f;

			using (new PaintWrapper (RenderingFlags.HighQuality)) {
				GL.Color4 (pen.Color);
				pen.DoGL ();

				GL.Begin (PrimitiveType.LineStrip);

				float angle = startAngle * Tau / 360f;
				if (sweepAngle < 360f)
					GL.Vertex2 (x, y);

				for (int i = 0; i < numSteps; i++)
				{
					GL.Vertex2 (x + (Math.Cos (angle) * radiusY), y + (Math.Sin (angle) * radiusX));
					angle += da;
				}

				if (sweepAngle < 360f)
					GL.Vertex2 (x, y);
				else
					GL.Vertex2 (x + (Math.Cos (angle) * radiusY), y + (Math.Sin (angle) * radiusX));
				
				GL.End ();
				pen.UndoGL ();
			}
		}

		public static void FillPie(this IGUIContext ctx, Brush brush, RectangleF rec, float startAngle, float sweepAngle)
		{
			FillPie (ctx, brush, rec.X + (rec.Width / 2f), rec.Y + (rec.Height / 2f), rec.Width / 2f, rec.Height / 2f, startAngle, sweepAngle);
		}			

		public static void FillPie(this IGUIContext ctx, Brush brush, float x, float y, float radiusX, float radiusY, float startAngle, float sweepAngle)
		{									
			float r = (Math.Abs(radiusX) + Math.Abs(radiusY)) / 2f;
			float segratio = (sweepAngle / 360f);
			float da = (float)Math.Acos(r / (r + 0.125f)) * 2f * segratio;
			int numSteps = (int)((Tau / da) * segratio + 0.5f) + 1;

			if (numSteps < 1 || da < float.Epsilon)
				return;

			startAngle -= 90f;

			using (new PaintWrapper (RenderingFlags.None)) {
				GL.Color4 (brush.Color);

				GL.Begin (PrimitiveType.TriangleFan);

				float angle = startAngle * Tau / 360f;
				if (sweepAngle < 360f)
					GL.Vertex2 (x, y);
				
				for (int i = 0; i < numSteps; i++)
				{
					GL.Vertex2 (x + (Math.Cos (angle) * radiusY), y + (Math.Sin (angle) * radiusX));
					angle += da;
				}
					
				angle = (startAngle + sweepAngle) * Tau / 360f;
				GL.Vertex2 (x + (Math.Cos (angle) * radiusY), y + (Math.Sin (angle) * radiusX));

				GL.End ();
			}
		}

		public static void DrawGrayButton(this IGUIContext ctx, RectangleF rect, byte alpha = 255)
		{			
			DrawButton(ctx, rect, Color.FromArgb(alpha, Theme.Colors.LightGrayButton), Color.FromArgb(alpha, Theme.Colors.GrayButton), Theme.Colors.Base1);
		}

		public static void DrawHighlightButton(this IGUIContext ctx, RectangleF rect, byte alpha = 255)
		{
			DrawButton(ctx, rect, Color.FromArgb(alpha, Theme.Colors.LightHighLightButton), Color.FromArgb(alpha, Theme.Colors.HighLightButton), Theme.Colors.HighLightButtonBorder);
		}

		public static void DrawButton(this IGUIContext ctx, RectangleF rect, Color TopColor, Color BottomColor, Color LineColor)
		{
			RectangleF topPart = new RectangleF(rect.Left, rect.Top, rect.Width, (int)(rect.Height * 3f / 5f) - 2);
			RectangleF lowPart = new RectangleF(topPart.Left, topPart.Bottom, topPart.Width, rect.Height - topPart.Height);

			int gradientHeight = (int)(topPart.Height / 3f) + 1;
			RectangleF gradientPart = new RectangleF(topPart.Left, topPart.Bottom - gradientHeight, topPart.Width, gradientHeight);

			if (topPart.Width > 0 && topPart.Height > 0)
			{
				using (var brush = new SolidBrush (TopColor)) {
					ctx.FillRectangle (brush, topPart);
				}
			}

			if (lowPart.Width > 0 && lowPart.Height > 0)
			{
				using (var brush = new SolidBrush (BottomColor)) {
					ctx.FillRectangle (brush, lowPart);
				}
			}

			if (gradientPart.Width > 0 && gradientPart.Height > 0)
			{
				using (var aGB = new LinearGradientBrush (TopColor, BottomColor, GradientDirections.Vertical)) {
					ctx.FillRectangle (aGB, gradientPart);
				}
			}

			if (rect.Width > 0 && rect.Height > 0)
			{
				using (var aPen = new Pen (LineColor)) {
					ctx.DrawRectangle (aPen, rect.Left - 0.5f, rect.Top - 0.5f, rect.Width, rect.Height);
				}
			}
		}			
	}
}

