using System;
using System.Collections.Generic;
using System.Drawing;

namespace SummerGUI
{
	/// <summary>
	/// CPU-seitige Dash-Tessellierung entlang von Segmenten und Polylines —
	/// wie ein GDI+/WinForms-Rasterer: Das Muster {on, off, on, off, ...}
	/// (Einheiten = Pen-Breite, gerader Index = Strich, ungerader = Lücke)
	/// wird entlang des Bogendurchmessers "laufen" und jedes "on"-Läuft
	/// wird zu einem (geraden) Untersegment aufgeteilt.
	/// Solid = keine Muster, die ganze Linie ist "on".
	/// </summary>
	public static class DashEngine
	{
		public struct Segment
		{
			public float x1, y1, x2, y2;
			public Segment(float x1, float y1, float x2, float y2)
			{
				this.x1 = x1; this.y1 = y1; this.x2 = x2; this.y2 = y2;
			}
		}

		/// <summary>
		/// Zerlegt das Segment p1→p2 in "on"-Läufe gemäß <paramref name="patternWidthUnits"/>.
		/// null/leere Muster → solid (exakt ein "on"-Läuft = die ganze Linie).
		/// Muster-Einträge sind Vielfache der Pen-Breite (wie WinForms' GdiPlus-Dash-Pattern).
		/// </summary>
		public static List<Segment> Tessellate(float x1, float y1, float x2, float y2,
			float width, float[] patternWidthUnits)
		{
			var result = new List<Segment>();

			float dx = x2 - x1, dy = y2 - y1;
			float length = (float)MathF.Sqrt(dx * dx + dy * dy);
			if (length < 0.0001f)
				return result;

			if (patternWidthUnits == null || patternWidthUnits.Length == 0)
			{
				result.Add(new Segment(x1, y1, x2, y2));
				return result;
			}

			if (!ResolvePattern(patternWidthUnits, width, out float[] pat, out float cycle))
			{
				// Muster ist leer → solid
				result.Add(new Segment(x1, y1, x2, y2));
				return result;
			}

			float fx = dx / length, fy = dy / length;
			int n = pat.Length;

			float s = 0f;
			for (int entry = 0; s < length && entry < 10_000; entry++)
			{
				int idx = entry % n;
				float segLen = pat[idx];
				if (idx % 2 == 0)          // "on"
				{
					float a = s;
					float b = MathF.Min(s + segLen, length);
					if (b > a)
					{
						float ax = x1 + fx * a, ay = y1 + fy * a;
						float bx = x1 + fx * b, by = y1 + fy * b;
						result.Add(new Segment(ax, ay, bx, by));
					}
				}
				s += segLen;
			}

			return result;
		}

		public static float[] GetPattern (DashStyle style, float[] customPattern)
		{
			// GDI+-kompatible Standardmuster (Einheiten = Pen-Breite); Solid/empty → null.
			switch (style)
			{
				case DashStyle.Dot:
					return new float[] {1f, 1f};
				case DashStyle.Dash:
					return new float[] {3f, 2f};
				case DashStyle.DashDot:
					return new float[] {3f, 1f, 1f, 1f};
				case DashStyle.DashDotDot:
					return new float[] {3f, 1f, 1f, 1f, 1f, 1f};
				case DashStyle.Custom:
					return (customPattern != null && customPattern.Length > 0) ? customPattern : null;
				default: // Solid
					return null;
			}
		}

		/// <summary>
		/// Zerlegt eine Polyline (optional geschlossen) in "on"-Läufe.
		/// Jedes "on"-Läuft kann mehrere Kanten überspannen; es wird zu geraden Chords aufgeteilt,
		/// die an den Kanten-Grenzen abschließen.
		/// </summary>
		public static List<Segment> TessellatePolyline (IList<PointF> pts, bool closed,
			float width, float[] patternWidthUnits)
		{
			var result = new List<Segment>();
			if (pts == null || pts.Count < 2)
				return result;

			int count = closed ? pts.Count : pts.Count - 1;
			if (count < 1)
				return result;

			// Kumulative Längen über die Kanten.
			float[] cum = new float[count + 1];
			float total = 0f;
			for (int i = 0; i < count; i++)
			{
				PointF a = pts[i];
				PointF b = pts[(i + 1) % pts.Count];
				total += (float)MathF.Sqrt((b.X - a.X) * (b.X - a.X) + (b.Y - a.Y) * (b.Y - a.Y));
				cum[i + 1] = total;
			}

			if (total < 0.0001f)
				return result;

			if (patternWidthUnits == null || patternWidthUnits.Length == 0 ||
				!ResolvePattern(patternWidthUnits, width, out float[] pat, out float cycle))
			{
				// Solid: Kante für Kante übernehmen
				for (int i = 0; i < count; i++)
				{
					PointF a = pts[i];
					PointF b = pts[(i + 1) % pts.Count];
					result.Add(new Segment(a.X, a.Y, b.X, b.Y));
				}
				return result;
			}

			// Punkt bei Bogendurchmesser t (stufenweise über die Kanten).
			PointF PtAt (float t)
			{
				if (t <= cum[0]) return pts[0];
				int k = count - 1;
				while (k > 0 && cum[k] > t) k--;
				PointF a = pts[k];
				PointF b = pts[(k + 1) % pts.Count];
				float segLen = cum[k + 1] - cum[k];
				float f = (segLen > 0.0001f) ? (t - cum[k]) / segLen : 0f;
				return new PointF (a.X + (b.X - a.X) * f, a.Y + (b.Y - a.Y) * f);
			}

			int n = pat.Length;
			float s = 0f;
			for (int entry = 0; s < total && entry < 100_000; entry++)
			{
				int idx = entry % n;
				float segLen = pat[idx];

				if (idx % 2 == 0)          // "on"
				{
					float a = MathF.Max(0f, s);
					float b = MathF.Min(s + segLen, total);
					if (b > a)
						EmitRun(result, pts, count, cum, a, b, PtAt);
				}

				s += segLen;
			}

			return result;
		}

		/// <summary>Zero-Abbruch bei leerem Muster; false wenn kein gültiges Muster ist.</summary>
		static bool ResolvePattern(float[] pattern, float width, out float[] pat, out float cycle)
		{
			pat = null;
			cycle = 0f;
			if (pattern == null || pattern.Length == 0)
				return false;

			pat = new float[pattern.Length];
			for (int i = 0; i < pattern.Length; i++)
			{
				pat[i] = MathF.Max(0f, pattern[i]) * width;
				cycle += pat[i];
			}
			return cycle > 0.0001f;
		}

		/// <summary>Schreibt ein "on"-Läuft [a,b) entlang einer Polyline als Chords über die Kanten.</summary>
		static void EmitRun (List<Segment> result, IList<PointF> pts, int count, float[] cum,
			float a, float b, Func<float, PointF> PtAt)
		{
			// Kanten, die [a,b] berühren (linear; count ist klein genug).
			int start = count - 1;
			while (start > 0 && cum[start] > a) start--;       // cum[start] <= a < cum[start+1]
			int stop = 0;
			while (stop < count && cum[stop] < b) stop++;      // cum[stop-1] < b <= cum[stop] → letze Kante stop-1
			for (int k = start; k < stop; k++)
			{
				float ca = MathF.Max (a, cum[k]);
				float cb = MathF.Min (b, cum[k + 1]);
				if (cb <= ca + 0.0001f)
					continue;
				PointF p1 = PtAt (ca);
				PointF p2 = PtAt (cb);
				if (!p1.Equals (p2))
					result.Add (new Segment (p1.X, p1.Y, p2.X, p2.Y));
			}
		}
	}
}
