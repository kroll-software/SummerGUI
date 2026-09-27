using System;
using System.Linq;
using System.Drawing;
using System.Collections.Generic;
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

	public static class GUIDrawing
	{				
		// Convert back and forth between System.Drawing.Color and OpenGL.Color4

		public static Color ToColor(this Color4 color4)
		{
			// 1. Clamping & Skalierung auf 0-255
			// Math.Clamp wird vom JIT-Compiler direkt in SIMD-CPU-Befehle (minss/maxss) übersetzt.
			float a = Math.Clamp(color4.A, 0f, 1f) * 255f;
			float r = Math.Clamp(color4.R, 0f, 1f) * 255f;
			float g = Math.Clamp(color4.G, 0f, 1f) * 255f;
			float b = Math.Clamp(color4.B, 0f, 1f) * 255f;

			// 2. Schnelles Runden via Cast & Bit-Shifting direkt in ein ARGB-Int
			int argb = ((int)(a + 0.5f) << 24) |
						((int)(r + 0.5f) << 16) |
						((int)(g + 0.5f) << 8)  |
									(int)(b + 0.5f);

			return Color.FromArgb(argb);
		}

		public static Color4 ToColor4(this Color color)
		{
			// Division durch 255f (Float), um Werte zwischen 0.0f und 1.0f zu erhalten
			return new Color4(
				color.R / 255f, 
				color.G / 255f, 
				color.B / 255f, 
				color.A / 255f
			);
		}

		// ******************************************
		// ******************************************
		// LINES
		// Dash wird CPU-seitig entlang der Strecke gesteppt (DashStyle inkl. Custom-Pattern;
		// Musterlängen in Einheiten der Pen-Breite — identische Semantik wie WinForms/GDI+).
		// WinForms: Graphics.DrawLine(Pen pen, float x1, float y1, float x2, float y2)

		public static void DrawLine (this IGUIContext ctx, Pen pen, float x1, float y1, float x2, float y2)
		{
			if (pen == null) return;
			DashPen (ctx, pen, x1, y1, x2, y2);
		}

		// WinForms: Graphics.DrawLine(Pen pen, Point pt1, Point pt2)
		public static void DrawLine (this IGUIContext ctx, Pen pen, System.Drawing.Point pt1, System.Drawing.Point pt2)
			=> DrawLine (ctx, pen, pt1.X, pt1.Y, pt2.X, pt2.Y);

		// WinForms: Graphics.DrawLine(Pen pen, PointF pt1, PointF pt2)
		public static void DrawLine (this IGUIContext ctx, Pen pen, PointF pt1, PointF pt2)
			=> DrawLine (ctx, pen, pt1.X, pt1.Y, pt2.X, pt2.Y);

		// WinForms: Graphics.DrawLines(Pen pen, PointF[] points)
		public static void DrawLines (this IGUIContext ctx, Pen pen, PointF[] points)
		{
			if (pen == null || points == null || points.Length < 2)
				return;
			EmitDashedSegments (ctx, pen, DashEngine.TessellatePolyline (points, false, pen.Width, DashEngine.GetPattern (pen.DashStyle, pen.DashPattern)));
		}

		// WinForms: Graphics.DrawPolygon(Pen pen, PointF[] points) — offene Polyline (letzter→erster wird NICHT verbunden)
		public static void DrawPolygon (this IGUIContext ctx, Pen pen, PointF[] points)
		{
			if (pen == null || points == null || points.Length < 2)
				return;
			EmitDashedSegments (ctx, pen, DashEngine.TessellatePolyline (points, false, pen.Width, DashEngine.GetPattern (pen.DashStyle, pen.DashPattern)));
		}

		// Interne Hilfe: dash-teyllierte Segmente als dicke Lintie-Linien in den Batcher.
		static void EmitDashedSegments (IGUIContext ctx, Pen pen, List<DashEngine.Segment> segs)
		{
			if (segs == null || segs.Count == 0)
				return;

			float w = pen.Width > 0f ? pen.Width : 1f;
			Color4 c = pen.Color.ToColor4 ();
			for (int i = 0; i < segs.Count; i++)
			{
				DashEngine.Segment s = segs[i];
				ctx.Batcher.AddLine (s.x1, s.y1, s.x2, s.y2, c, w);
			}
		}

		// Interne Hilfe: ein gerades Segment mit dash-Pattern.
		static void DashPen (IGUIContext ctx, Pen pen, float x1, float y1, float x2, float y2)
		{
			EmitDashedSegments (ctx, pen, DashEngine.Tessellate (x1, y1, x2, y2, pen.Width, DashEngine.GetPattern (pen.DashStyle, pen.DashPattern)));
		}

		public static void FillPolygon(this IGUIContext ctx, Brush brush, PointF[] points)
		{
			if (points == null || points.Length < 3)
				return;

			// Wir bauen das Polygon als Triangle Fan im Batcher nach
			// Punkt 0 ist das Zentrum für alle Dreiecke
			Vector2 p0 = new Vector2(points[0].X, points[0].Y);
			
			for (int i = 1; i < points.Length - 1; i++)
			{
				Vector2 p1 = new Vector2(points[i].X, points[i].Y);
				Vector2 p2 = new Vector2(points[i + 1].X, points[i + 1].Y);
				
				ctx.Batcher.AddTriangle(p0, p1, p2, brush.Color);
			}
		}

		public static void FillRectangle(this IGUIContext ctx, Brush brush, RectangleF r)
		{
			if (brush == null)
				return;

			if (brush is SolidBrush solid)
				FillRectangle (ctx, solid, r);
			else if (brush is LinearGradientBrush grad)
				FillRectangle (ctx, grad, r);
			else if (brush is HatchBrush hatch)
				FillRectangle (ctx, hatch, r);
		}
		
		public static void FillRectangle(this IGUIContext ctx, LinearGradientBrush brush, float x, float y, float width, float height)
		{
			FillRectangle (ctx, brush, new RectangleF (x, y, width, height));
		}

		public static void FillRectangle(this IGUIContext ctx, LinearGradientBrush brush, RectangleF r)
		{
			if (brush == null || r.Width < 0 || r.Height < 0)
				return;

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
			// Wir definieren die Farben für den horizontalen Verlauf:
			// Links (Top & Bottom) = brush.Color
			// Rechts (Top & Bottom) = brush.GradientColor
			
			ctx.Batcher.AddRectangle(
				r, 
				brush.Color,          // Top-Left
				brush.GradientColor,  // Top-Right
				brush.GradientColor,  // Bottom-Right
				brush.Color           // Bottom-Left
			);
		}
		
		private static void FillRectangleVertical(this IGUIContext ctx, LinearGradientBrush brush, RectangleF r)
		{
			ctx.Batcher.AddRectangle(r, 
				brush.Color,          // TL
				brush.Color,          // TR
				brush.GradientColor,  // BR
				brush.GradientColor); // BL
		}

		private static void FillRectangleTopLeft(this IGUIContext ctx, LinearGradientBrush brush, RectangleF r)
		{   
			ctx.Batcher.AddRectangle(r, 
				brush.GradientColor,  // TL (Anders)
				brush.Color,          // TR
				brush.Color,          // BR
				brush.Color);         // BL
		}

		private static void FillRectangleForwardDiagonal(this IGUIContext ctx, LinearGradientBrush brush, RectangleF r)
		{
			ctx.Batcher.AddRectangle(r, 
				brush.GradientColor,  // TL
				brush.GradientColor,  // TR
				brush.GradientColor,  // BR
				brush.Color);         // BL (Anders)
		}

		private static void FillRectangleBackwardDiagonal(this IGUIContext ctx, LinearGradientBrush brush, RectangleF r)
		{
			ctx.Batcher.AddRectangle(r, 
				brush.Color,          // TL (Anders)
				brush.GradientColor,  // TR
				brush.GradientColor,  // BR
				brush.GradientColor); // BL
		}		
		
		// *** FillRectangle		

		public static void FillRectangle(this IGUIContext ctx, SolidBrush brush, RectangleF r)
		{
			if (brush == null || r.Width <= 0 || r.Height <= 0 || brush.Color.A == 0)
				return;

			ctx.Batcher.AddRectangle(r, brush.Color);
		}
		
		public static void FillRectangle(this IGUIContext ctx, Brush brush, float x, float y, float width, float height)
		{	
			FillRectangle(ctx, brush, new RectangleF(x, y, width, height));
		}

		// *** HatchBrush (WinForms: Graphics.FillRectangle (Brush brush, Rectangle rect)) ***
		// Der Hintergrund (brush.Color) wird erst gefüllt, dann das Motiv (brush.HatchColor).

		public static void FillRectangle(this IGUIContext ctx, HatchBrush brush, RectangleF r)
		{
			if (brush == null || r.Width <= 0 || r.Height <= 0)
				return;

			HatchRenderer.Render (ctx, brush.HatchStyle, r, brush.Color, brush.HatchColor);
		}

		public static void FillRectangle(this IGUIContext ctx, HatchBrush brush, float x, float y, float width, float height)
		{
			FillRectangle (ctx, brush, new RectangleF (x, y, width, height));
		}

		// *** Rounded Rectangles ***

		public static void FillRoundedRectangle(this IGUIContext ctx, Brush brush, RectangleF r, float radius)
		{						
			if (brush == null)
				return;

			SolidBrush solid = brush as SolidBrush;
			if (solid != null)
				FillRoundedRectangle (ctx, solid, r, radius);
			return;
		}

		public static void FillRoundedRectangle(this IGUIContext ctx, SolidBrush brush, RectangleF r, float radius)
		{						
			if (brush == null || r.Width <= 0 || r.Height <= 0 || brush.Color.A == 0)
				return;

			ctx.Batcher.AddRoundedRectangle(r, brush.Color, radius);
		}

		public static void FillRoundedRectangle(this IGUIContext ctx, LinearGradientBrush brush, RectangleF r, float radius)
		{
			if (brush == null || r.Width <= 0 || r.Height <= 0)
				return;

			switch (brush.Direction) 
			{
			case GradientDirections.Horizontal:
				FillRoundedRectangleHorizontal (ctx, brush, r, radius);
				break;

			case GradientDirections.Vertical:
				FillRoundedRectangleVertical (ctx, brush, r, radius);
				break;

			case GradientDirections.TopLeft:
				FillRoundedRectangleTopLeft (ctx, brush, r, radius);
				break;

			case GradientDirections.ForwardDiagonal:
				FillRoundedRectangleForwardDiagonal (ctx, brush, r, radius);
				break;

			case GradientDirections.BackwardDiagonal:
				FillRoundedRectangleBackwardDiagonal (ctx, brush, r, radius);
				break;				
			
			}
		}
		
		private static void FillRoundedRectangleHorizontal(this IGUIContext ctx, LinearGradientBrush brush, RectangleF r, float radius)
		{
			// Wir definieren die Farben für den horizontalen Verlauf:
			// Links (Top & Bottom) = brush.Color
			// Rechts (Top & Bottom) = brush.GradientColor
			
			ctx.Batcher.AddRoundedRectangleGradient(
				r, 
				brush.Color,          // Top-Left
				brush.GradientColor,  // Top-Right
				brush.GradientColor,  // Bottom-Right
				brush.Color,           // Bottom-Left
				radius
			);
		}
		
		private static void FillRoundedRectangleVertical(this IGUIContext ctx, LinearGradientBrush brush, RectangleF r, float radius)
		{
			ctx.Batcher.AddRoundedRectangleGradient(r, 
				brush.Color,          // TL
				brush.Color,          // TR
				brush.GradientColor,  // BR
				brush.GradientColor, radius); // BL
		}

		private static void FillRoundedRectangleTopLeft(this IGUIContext ctx, LinearGradientBrush brush, RectangleF r, float radius)
		{   
			ctx.Batcher.AddRoundedRectangleGradient(r, 
				brush.GradientColor,  // TL (Anders)
				brush.Color,          // TR
				brush.Color,          // BR
				brush.Color, radius);         // BL
		}

		private static void FillRoundedRectangleForwardDiagonal(this IGUIContext ctx, LinearGradientBrush brush, RectangleF r, float radius)
		{
			ctx.Batcher.AddRoundedRectangleGradient(r, 
				brush.GradientColor,  // TL
				brush.GradientColor,  // TR
				brush.GradientColor,  // BR
				brush.Color, radius);         // BL (Anders)
		}

		private static void FillRoundedRectangleBackwardDiagonal(this IGUIContext ctx, LinearGradientBrush brush, RectangleF r, float radius)
		{
			ctx.Batcher.AddRoundedRectangleGradient(r, 
				brush.Color,          // TL (Anders)
				brush.GradientColor,  // TR
				brush.GradientColor,  // BR
				brush.GradientColor, radius); // BL
		}

		// *** DrawRectangle (WinForms: Graphics.DrawRectangle (Pen pen, ...)) ***

		public static void DrawRectangle(this IGUIContext ctx, Pen pen, float x, float y, float width, float height)
		{	
			DrawRectangle (ctx, pen, new RectangleF (x, y, width, height));
		}
		
		public static void DrawRectangle(this IGUIContext ctx, Pen pen, RectangleF r)
		{
			if (pen == null || r.Width <= 0 || r.Height <= 0)
				return;

			// Vier Kanten als offene, geschlossene Polyline — das Dash-Muster läuft
			// nahtlos um die Ecke (WinForms-Verhalten).
			PointF[] pts =
			{
				new PointF (r.Left, r.Top),
				new PointF (r.Right, r.Top),
				new PointF (r.Right, r.Bottom),
				new PointF (r.Left, r.Bottom),
				new PointF (r.Left, r.Top)
			};
			EmitDashedSegments (ctx, pen, DashEngine.TessellatePolyline (pts, false, pen.Width, DashEngine.GetPattern (pen.DashStyle, pen.DashPattern)));
		}

		public static void DrawRoundedRectangle(this IGUIContext ctx, Pen pen, RectangleF r, float radius)
		{
			if (pen == null || r.Width <= 0 || r.Height <= 0 || pen.Color.A == 0)
				return;

			ctx.Batcher.AddRoundedRectangleOutline(r, pen.Color, pen.Width, radius);
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
		static readonly float Tau = MathF.PI * 2f;

		public static void DrawEllipse(this IGUIContext ctx, Pen pen, float cx, float cy, float radiusX, float radiusY, float scale = 1)
		{
			if (pen == null) return;
			float r = (MathF.Abs(radiusX) + MathF.Abs(radiusY)) / 2f;
			if (r < float.Epsilon) return;

			float da = MathF.Acos(r / (r + 0.125f)) * 2f / scale;
			int numSteps = (int)MathF.Round(Tau / da);

			if (numSteps < 3) numSteps = 12; // Sicherheitshalber Minimum

			PointF firstPoint = PointF.Empty;
			PointF lastPoint = PointF.Empty;

			for (int i = 0; i <= numSteps; i++)
			{                   
				float angle = i * da;
				float x = (MathF.Cos(angle) * radiusX) + cx;
				float y = (MathF.Sin(angle) * radiusY) + cy;
				PointF currentPoint = new PointF(x, y);

				if (i == 0) {
					firstPoint = currentPoint;
				} else {
					DashPen (ctx, pen, lastPoint.X, lastPoint.Y, currentPoint.X, currentPoint.Y);
				}
				lastPoint = currentPoint;
			}
			
			// Den Kreis schließen
			DashPen (ctx, pen, lastPoint.X, lastPoint.Y, firstPoint.X, firstPoint.Y);
		}

		public static void FillEllipse(this IGUIContext ctx, Brush brush, float cx, float cy, float radiusX, float radiusY, float scale = 1)
		{
			if (brush == null) return;
			ctx.Batcher.FillEllipse(brush.Color, cx, cy, radiusX, radiusY, scale);			
		}

		public static void DrawPie(this IGUIContext ctx, Pen pen, RectangleF rec, float startAngle, float sweepAngle)
		{
			DrawPie (ctx, pen, rec.X + (rec.Width / 2f), rec.Y + (rec.Height / 2f), rec.Width / 2f, rec.Height / 2f, startAngle, sweepAngle);
		}

		public static void DrawPie(this IGUIContext ctx, Pen pen, float x, float y, float radiusX, float radiusY, float startAngle, float sweepAngle)
		{
			if (pen == null) return;
			float r = (MathF.Abs(radiusX) + MathF.Abs(radiusY)) / 2f;
			float segratio = MathF.Abs(sweepAngle) / 360f;
			
			// Deine bewährte Formel für die Schrittweite
			float da = MathF.Acos(r / (r + 0.125f)) * 2f; 
			int numSteps = (int)MathF.Max(1, (Tau / da) * segratio);
			da = (sweepAngle * (Tau / 360f)) / numSteps; // Exakte Schrittweite für sweepAngle

			float angle = (startAngle - 90f) * (Tau / 360f);
			float currentAngle = angle;

			Vector2 center = new Vector2(x, y);
			Vector2 lastPoint = Vector2.Zero;
			Vector2 firstPoint = Vector2.Zero;

			for (int i = 0; i <= numSteps; i++)
			{
				Vector2 nextPoint = new Vector2(x + MathF.Cos(currentAngle) * radiusX, y + MathF.Sin(currentAngle) * radiusY);
				
				if (i == 0) firstPoint = nextPoint;

				if (i > 0)				
				
					DashPen (ctx, pen, lastPoint.X, lastPoint.Y, nextPoint.X, nextPoint.Y);
				
				lastPoint = nextPoint;
				currentAngle += da;
			}

			// Die "Kuchenstücke"-Seitenlinien zeichnen, wenn es kein voller Kreis ist
			if (sweepAngle < 360f)
			{
				DashPen (ctx, pen, center.X, center.Y, firstPoint.X, firstPoint.Y);
				DashPen (ctx, pen, center.X, center.Y, lastPoint.X, lastPoint.Y);
			}
		}		

		public static void FillPie(this IGUIContext ctx, Brush brush, RectangleF rec, float startAngle, float sweepAngle)
		{
			FillPie (ctx, brush, rec.X + (rec.Width / 2f), rec.Y + (rec.Height / 2f), rec.Width / 2f, rec.Height / 2f, startAngle, sweepAngle);
		}		

		public static void FillPie(this IGUIContext ctx, Brush brush, float x, float y, float radiusX, float radiusY, float startAngle, float sweepAngle)
		{                                   
			if (brush == null) return;
			if (MathF.Abs(sweepAngle) < 0.01f) 
				return;

			float r = (Math.Abs(radiusX) + Math.Abs(radiusY)) / 2f;
			float segratio = Math.Abs(sweepAngle) / 360f;
			
			float da_limit = MathF.Acos(r / (r + 0.125f)) * 2f; 
			int numSteps = (int)MathF.Max(6, (Tau / da_limit) * segratio); // Mindestens 6 Segmente für kleine Stücke
			float da = (sweepAngle * (Tau / 360f)) / numSteps;

			float angle = (startAngle - 90f) * (Tau / 360f);
    			Vector2 center = new Vector2(x, y);

			for (int i = 0; i < numSteps; i++)
			{
				Vector2 p1 = new Vector2(x + MathF.Cos(angle) * radiusX, y + MathF.Sin(angle) * radiusY);
				angle += da;
				Vector2 p2 = new Vector2(x + MathF.Cos(angle) * radiusX, y + MathF.Sin(angle) * radiusY);

				// Ein Dreieck pro Segment zum Batcher hinzufügen
				// Wir nutzen Type 0 (Solid/Image Modus) und die whiteTexture
				ctx.Batcher.AddTriangle(center, p1, p2, brush.Color);
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
			RectangleF topPart = new RectangleF(rect.Left, rect.Top, rect.Width, (rect.Height * 3f / 5f) - 2f);
			RectangleF lowPart = new RectangleF(topPart.Left, topPart.Bottom, topPart.Width, rect.Height - topPart.Height);

			float gradientHeight = (topPart.Height / 3f) + 1f;
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
					ctx.DrawRectangle (aPen, rect.Left, rect.Top, rect.Width, rect.Height);
				}
			}
		}		
	}
}
