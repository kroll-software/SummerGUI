using System;
using System.Drawing;
using System.Collections.Generic;
using KS.Foundation;
using OpenTK.Mathematics;

namespace SummerGUI
{
	/// <summary>
	/// Generates the 53 WinForms-compatible hatch textures as plain geometry
	/// (rectangles, thick segments, circles) and submits them through the
	/// existing Batcher primitives. No textures, no new GL state, fully portable.
	/// Geometric motifs are generated in an area slightly larger than the target
	/// rect; the Batcher's scissor (set by the caller, see HatchFill) clips the overflow.
	/// </summary>
	public static class HatchRenderer
	{
		const float Sqrt2 = 1.41421356f;

		public sealed class HatchSpec
		{
			// bars
			public int Direction;            // 0=H, 1=V, 2=diag "\" (downward), 3=diag "/" (upward)
			public float BarThickness;
			public float BarGap;
			public bool Dashed;
			public float DashOn, DashOff;    // in bar-thickness units
			public bool SecondDirection;     // also draw the perpendicular family (cross)

			// dots
			public float DotSpacing;
			public float DotRadius;
			public bool Staggered;

			// special
			public int Mode;                 // 0=bars (default), 1=dots, 2=checker, 3=zigzag,
			//                                        4=wave, 5=brickH, 6=brickD, 7=weave,
			//                                        8=plaid, 9=diamond, 10=confetti, 11=sphere
		}

		public static void Render(IGUIContext ctx, HatchStyle style, RectangleF rect, System.Drawing.Color bg, System.Drawing.Color motif)
		{
			if (rect.Width < 1f || rect.Height < 1f)
				return;

			// First the background
			if (bg.A > 0)
				ctx.Batcher.AddRectangle(rect, bg.ToColor4());

			HatchSpec spec = SpecFor(style);
			if (spec == null)
				return;

			Color4 c = motif.ToColor4();
			float ox = rect.Left, oy = rect.Top, w = rect.Width, h = rect.Height;

			// Clips the motifs to the target rectangle — the bars/dots are
			// intentionally generated with a bit of overflow.
			using (var clip = new ClipBoundClip(ctx, rect, combine: false))
			{
				switch (spec.Mode)
				{
					case 0: RenderBars(ctx, c, spec, ox, oy, w, h); break;
					case 1: RenderDots(ctx, c, spec, ox, oy, w, h); break;
					case 2: RenderChecker(ctx, c, spec, ox, oy, w, h); break;
					case 3: RenderZigZag(ctx, c, spec, ox, oy, w, h); break;
					case 4: RenderWave(ctx, c, spec, ox, oy, w, h); break;
					case 5: RenderBrick(ctx, c, spec, ox, oy, w, h, horizontal: true); break;
					case 6: RenderBrick(ctx, c, spec, ox, oy, w, h, horizontal: false); break;
					case 7: RenderWeave(ctx, c, spec, ox, oy, w, h); break;
					case 8: RenderPlaid(ctx, c, spec, ox, oy, w, h); break;
					case 9: RenderDiamond(ctx, c, spec, ox, oy, w, h); break;
					case 10: RenderConfetti(ctx, c, spec, ox, oy, w, h); break;
					case 11: RenderSphere(ctx, c, spec, ox, oy, w, h); break;
				}
			}
		}

		// ---------------- generators ----------------

		static void Bar(IGUIContext ctx, Color4 c, float x1, float y1, float x2, float y2, float t)
			=> ctx.Batcher.AddLine(x1, y1, x2, y2, c, t);   // plain thick line → quad

		static void HBar(IGUIContext ctx, Color4 c, RectangleF r, float y, float t)
			=> Bar(ctx, c, r.Left - t, y, r.Right + t, y, t);

		static void VBar(IGUIContext ctx, Color4 c, RectangleF r, float x, float t)
			=> Bar(ctx, c, x, r.Top - t, x, r.Bottom + t, t);

		// 45° bar crossing the rect. slope = +1 → "\" (downward, y = x + c); slope = -1 → "/" (upward, y = -x + c).
		// Renders the chord (the rect ∩ line) instead of guessing endpoints — correct for any offset ox/oy.
		static void DBar(IGUIContext ctx, Color4 c, RectangleF r, float slope, float intercept, float t, HatchSpec spec)
		{
			float xmin = r.Left, xmax = r.Right, ymin = r.Top, ymax = r.Bottom;
			var cx = new float[4];
			var cy = new float[4];
			int n = 0;
			void add(float px, float py) { cx[n] = px; cy[n] = py; n++; }

			float ly;
			ly = slope * xmin + intercept; if (ly >= ymin && ly <= ymax) add(xmin, ly);
			ly = slope * xmax + intercept; if (ly >= ymin && ly <= ymax) add(xmax, ly);
			float lx;
			lx = (ymin - intercept) / slope; if (lx >= xmin && lx <= xmax) add(lx, ymin);
			lx = (ymax - intercept) / slope; if (lx >= xmin && lx <= xmax) add(lx, ymax);

			if (n < 2)
				return;                                   // line misses the rect entirely

			// The line is monotonic in x → endpoints are min-x and max-x intersections.
			int i0 = 0, i1 = 0;
			for (int i = 1; i < n; i++)
			{
				if (cx[i] < cx[i0]) i0 = i;
				if (cx[i] > cx[i1]) i1 = i;
			}
			float x1 = cx[i0], y1 = cy[i0], x2 = cx[i1], y2 = cy[i1];

			if (spec.Dashed)
			{
				foreach (var s in DashEngine.Tessellate(x1, y1, x2, y2, t, new[] { spec.DashOn, spec.Off() }))
					Bar(ctx, c, s.x1, s.y1, s.x2, s.y2, t);
			}
			else
				Bar(ctx, c, x1, y1, x2, y2, t);
		}

				static void RenderBars(IGUIContext ctx, Color4 c, HatchSpec spec, float ox, float oy, float w, float h)
		{
			RectangleF r = new RectangleF(ox, oy, w, h);
			float t = spec.BarThickness;
			float period = t + spec.BarGap;
			if (period <= 0f) period = 1f;

			float diagPer = period * Sqrt2;      // intercept-axis spacing = perpendicular period * sqrt(2)

			switch (spec.Direction)
			{
				case 0: // horizontal bars
					for (float y = oy + period / 2f + t / 2f; y <= oy + h; y += period)
						HBar(ctx, c, r, y, t);
					break;
				case 1: // vertical bars
					for (float x = ox + period / 2f + t / 2f; x <= ox + w; x += period)
						VBar(ctx, c, r, x, t);
					break;
				case 2: // "\" diagonals, slope +1 (y = x + c). Intercept hits: [oy-ox-w .. oy+h-ox].
					{
						float lo = oy - ox - w - diagPer, hi = oy + h - ox + diagPer;
						for (float inr = MathF.Ceiling(lo / diagPer) * diagPer; inr < hi; inr += diagPer)
							DBar(ctx, c, r, 1f, inr + t / 2f, t, spec);
					}
					break;
				case 3: // "/" diagonals, slope -1 (y = -x + c). Intercept hits: [ox+oy .. ox+w+oy+h].
					{
						float lo = ox + oy - diagPer, hi = ox + w + oy + h + diagPer;
						for (float inr = MathF.Ceiling(lo / diagPer) * diagPer; inr < hi; inr += diagPer)
							DBar(ctx, c, r, -1f, inr + t / 2f, t, spec);
					}
					break;
			}

			if (spec.SecondDirection)
			{
				switch (spec.Direction)
				{
					case 0: // add vertical family
						for (float x = ox + period / 2f + t / 2f; x <= ox + w; x += period)
							VBar(ctx, c, r, x, t);
						break;
					case 1: // add horizontal family
						for (float y = oy + period / 2f + t / 2f; y <= oy + h; y += period)
							HBar(ctx, c, r, y, t);
						break;
					case 2: // add opposite (" / ") family  -> true crossed diagonals
						{
							float lo = ox + oy - diagPer, hi = ox + w + oy + h + diagPer;
							for (float inr = MathF.Ceiling(lo / diagPer) * diagPer; inr < hi; inr += diagPer)
								DBar(ctx, c, r, -1f, inr + t / 2f, t, spec);
						}
						break;
					case 3: // add opposite ("\ ") family
						{
							float lo = oy - ox - w - diagPer, hi = oy + h - ox + diagPer;
							for (float inr = MathF.Ceiling(lo / diagPer) * diagPer; inr < hi; inr += diagPer)
								DBar(ctx, c, r, 1f, inr + t / 2f, t, spec);
						}
						break;
				}
			}
		}

		static void RenderDots(IGUIContext ctx, Color4 c, HatchSpec spec, float ox, float oy, float w, float h)
		{
			float sp = spec.DotSpacing, rad = spec.DotRadius;
			if (sp <= 0f) sp = 1f;

			int cols = (int) MathF.Ceiling(w / sp) + 2;
			int rows = (int) MathF.Ceiling(h / sp) + 2;
			for (int j = -1; j < rows; j++)
			{
				for (int i = -1; i < cols; i++)
				{
					float x = ox + (i + (spec.Staggered && (j % 2 != 0) ? 0.5f : 0f)) * sp;
					float y = oy + (j + 0.5f) * sp;
					if (x < ox - rad || x > ox + w + rad || y < oy - rad || y > oy + h + rad)
						continue;
					ctx.Batcher.AddCircle(new Vector2(x, y), rad, c, 12);
				}
			}
		}

		static void RenderChecker(IGUIContext ctx, Color4 c, HatchSpec spec, float ox, float oy, float w, float h)
		{
			float cell = spec.DotSpacing;    // reuse spacing as cell size
			if (cell <= 0f) cell = 4f;
			float thick = MathF.Max(1f, spec.BarThickness);
			float step = cell;

			int startI = (int) MathF.Floor(ox / step);
			int startJ = (int) MathF.Floor(oy / step);
			for (int j = startJ; j * step < oy + h + step; j++)
			{
				for (int i = startI; i * step < ox + w + step; i++)
				{
					if (((i + j) & 1) == 0)
					{
						RectangleF cellR = new RectangleF(i * step, j * step, step, step);
						// inset the filled square slightly so a grid of gaps shows (WinForms checkerboard)
						float inset = MathF.Min(step * 0.25f, thick);
						ctx.Batcher.AddRectangle(new RectangleF(cellR.X + inset, cellR.Y + inset,
							MathF.Max(0.5f, step - 2 * inset), MathF.Max(0.5f, step - 2 * inset)), c);
					}
				}
			}
		}

		static void RenderZigZag(IGUIContext ctx, Color4 c, HatchSpec spec, float ox, float oy, float w, float h)
		{
			float period = spec.DotSpacing;        // wavelength
			float amp = spec.BarThickness;         // amplitude
			float rowStep = spec.BarGap + period;  // row spacing
			if (period <= 2f) period = 8f;
			if (rowStep <= 0f) rowStep = period;

			float t = MathF.Max(1f, MathF.Min(2f, spec.BarThickness * 0.75f));

			for (float y0 = oy + rowStep * 0.5f; y0 < oy + h + rowStep; y0 += rowStep)
			{
				float x = ox - period;
				float y = y0;
				float px = ox, py = y0 + amp;
				// build a polyline across, then emit as plain thin segments
				ctx.Batcher.AddLine(ox - period * 0.5f, y0, ox, y0 + amp, c, t);
				float dir = 1f;
				for (float xx = ox; xx <= ox + w + period; xx += period / 2f)
				{
					float yy = y0 + amp * dir;
					ctx.Batcher.AddLine(px, py, MathF.Min(xx, ox + w + period), yy, c, t);
					px = xx; py = yy; dir = -dir;
					if (px >= ox + w) break;
				}
			}
		}

		static void RenderWave(IGUIContext ctx, Color4 c, HatchSpec spec, float ox, float oy, float w, float h)
		{
			float wavelength = spec.DotSpacing;
			if (wavelength <= 4f) wavelength = 16f;
			float amp = spec.BarThickness;
			float rowStep = spec.BarGap + amp * 2f;
			if (rowStep <= 0f) rowStep = MathF.Max(8f, wavelength * 0.6f);
			float t = MathF.Max(1f, MathF.Min(2f, spec.BarThickness * 0.75f));
			int stepsPerWavelength = 24;

			for (float y0 = oy + rowStep * 0.5f; y0 < oy + h + rowStep; y0 += rowStep)
			{
				float px = ox - 1f;
				float py = y0 + (float)Math.Sin(0) * amp;
				for (float xx = ox - 1f; xx <= ox + w + 1f; xx += wavelength / stepsPerWavelength)
				{
					float yy = y0 + (float)Math.Sin((xx - ox) * 2f * MathF.PI / wavelength) * amp;
					ctx.Batcher.AddLine(px, py, xx, yy, c, t);
					px = xx; py = yy;
				}
			}
		}

		static void RenderBrick(IGUIContext ctx, Color4 c, HatchSpec spec, float ox, float oy, float w, float h, bool horizontal)
		{
			float mortar = spec.BarThickness;          // joint thickness
			float ch = spec.BarGap;                     // course height (brick height)
			if (ch <= 0f) ch = 10f;
			float bw = ch * 2.2f;                       // brick width
			if (!horizontal && (ch <= 0f)) ch = 10f;

			int courses = (int) MathF.Ceiling(h / ch) + 1;
			for (int row = 0; row <= courses; row++)
			{
				float linePos = horizontal ? oy + row * ch : ox + row * ch;
				if (!horizontal && linePos > ox + w) break;
				if (horizontal && linePos > oy + h + ch) break;

				if (horizontal)
					HBar(ctx, c, new RectangleF(ox, oy, w, h), linePos, mortar);
				else
					VBar(ctx, c, new RectangleF(ox, oy, w, h), linePos, mortar);

				// vertical joints for this course, offset by half a brick on odd rows
				float offset = (row % 2 == 1) ? bw / 2f : 0f;
				float from = (horizontal ? ox : oy) - bw;
				float to = (horizontal ? ox + w : oy + h) + bw;
				for (float p = MathF.Ceiling((from + offset) / bw) * bw; p <= to; p += bw)
				{
					if (horizontal)
						VBar(ctx, c, new RectangleF(ox, oy, w, h), p, mortar);
					else
						HBar(ctx, c, new RectangleF(ox, oy, w, h), p, mortar);
				}
			}
		}

		static void RenderWeave(IGUIContext ctx, Color4 c, HatchSpec spec, float ox, float oy, float w, float h)
		{
			float cell = spec.DotSpacing;
			if (cell <= 2f) cell = 8f;
			float gap = cell * 0.25f;
			float step = cell;

			int startI = (int) MathF.Floor(ox / step);
			for (int j = (int) MathF.Floor(oy / step); j * step < oy + h + step; j++)
			{
				for (int i = startI; i * step < ox + w + step; i++)
				{
					if (((i + j) & 1) == 0)
						ctx.Batcher.AddRectangle(new RectangleF(i * step, j * step, step - gap, MathF.Max(1f, (step - gap) * 0.55f)), c);
					else
						ctx.Batcher.AddRectangle(new RectangleF(i * step + MathF.Max(0f, (step - gap) * 0.45f), j * step, MathF.Max(1f, (step - gap) * 0.55f), step - gap), c);
				}
			}
		}

		static void RenderPlaid(IGUIContext ctx, Color4 c, HatchSpec spec, float ox, float oy, float w, float h)
		{
			float thin = MathF.Max(1f, spec.BarThickness * 0.5f);
			float thick = spec.BarThickness * 2f;
			float fineStep = MathF.Max(4f, spec.BarGap);
			float boldStep = fineStep * 3f;

			// fine cross-hatch
			RectangleF r = new RectangleF(ox, oy, w, h);
			for (float x = ox + fineStep / 2f; x < ox + w; x += fineStep)
				VBar(ctx, c, r, x, thin);
			for (float y = oy + fineStep / 2f; y < oy + h; y += fineStep)
				HBar(ctx, c, r, y, thin);
			// bold stripes
			for (float x = ox + boldStep; x < ox + w; x += boldStep * 2f)
				VBar(ctx, c, r, x, thick);
			for (float y = oy + boldStep; y < oy + h; y += boldStep * 2f)
				HBar(ctx, c, r, y, thick);
		}

		static void RenderDiamond(IGUIContext ctx, Color4 c, HatchSpec spec, float ox, float oy, float w, float h)
		{
			// rhombus lattice = both 45° families (slope +1 and -1), spacing + thickness from spec.
			float t = spec.BarThickness;
			float period = t + spec.BarGap;
			if (period <= 0f) period = 6f;
			RectangleF r = new RectangleF(ox, oy, w, h);
			float diagPeriod = period * Sqrt2;

			// "\" family (slope +1)
			{
				float lo = oy - ox - w - diagPeriod, hi = oy + h - ox + diagPeriod;
				for (float inr = MathF.Ceiling(lo / diagPeriod) * diagPeriod; inr < hi; inr += diagPeriod)
					DBar(ctx, c, r, 1f, inr + t / 2f, t, spec);
			}
			// "/" family (slope -1)
			{
				float lo = ox + oy - diagPeriod, hi = ox + w + oy + h + diagPeriod;
				for (float inr = MathF.Ceiling(lo / diagPeriod) * diagPeriod; inr < hi; inr += diagPeriod)
					DBar(ctx, c, r, -1f, inr + t / 2f, t, spec);
			}
		}

		static void RenderConfetti(IGUIContext ctx, Color4 c, HatchSpec spec, float ox, float oy, float w, float h)
		{
			// deterministic specks (stable across frames)
			uint seed = 0x9E3779B9u ^ (uint)(ox * 7 + oy * 13 + w) ^ ((uint)(int)h * 31u);
			float cell = spec.DotSpacing;
			if (cell <= 1f) cell = 12f;
			float rad = spec.DotRadius;

			int cols = (int) MathF.Ceiling(w / cell);
			int rows = (int) MathF.Ceiling(h / cell);
			for (int ridx = 0; ridx < rows; ridx++)
			{
				for (int cidx = 0; cidx < cols; cidx++)
				{
					uint v0 = MurmurMix(seed, ridx, cidx);
					if ((v0 % 100) < 18)                 // ~18% of cells get a speck
					{
						float fx = ((v0 >> 8) % 1000) / 1000f;
						float fy = ((v0 >> 16) % 1000) / 1000f;
						float fr = 0.4f + ((v0 >> 24) % 100f) / 100f * 0.9f;
						float x = ox + cidx * cell + fx * cell;
						float y = oy + ridx * cell + fy * cell;
						if (x < ox || x > ox + w || y < oy || y > oy + h)
							continue;
						ctx.Batcher.AddCircle(new Vector2(x, y), MathF.Max(0.6f, rad * fr), c, 10);
					}
				}
			}
		}

		static void RenderSphere(IGUIContext ctx, Color4 c, HatchSpec spec, float ox, float oy, float w, float h)
		{
			// dense staggered dot field
			float sp = spec.DotSpacing;
			if (sp <= 1f) sp = 5f;
			float rad = spec.DotRadius;

			int cols = (int) MathF.Ceiling(w / sp) + 1;
			int rows = (int) MathF.Ceiling(h / sp) + 1;
			for (int j = 0; j < rows; j++)
			{
				for (int i = 0; i < cols; i++)
				{
					float x = ox + (i + (j % 2 == 0 ? 0.5f : 0f)) * sp;
					float y = oy + j * sp;
					if (y < oy || y > oy + h)
						continue;
					if (x < ox - rad || x > ox + w + rad)
						continue;
					ctx.Batcher.AddCircle(new Vector2(x, y), rad, c, 10);
				}
			}
		}

		static uint MurmurMix(uint seed, int i, int j)
		{
			uint h = seed ^ (uint)(i * 0x9E3779B1) ^ (uint)(j * 0x85EBCA6B);
			h ^= h >> 16; h *= 0x85EBCA6B; h ^= h >> 13; h *= 0xC2B2AE35; h ^= h >> 16;
			return h;
		}

		static float Off(this HatchSpec s) => s.DashOff;

		// ---------------- spec table ----------------

		public static HatchSpec SpecFor(HatchStyle style)
		{
			var s = new HatchSpec();
			switch (style)
			{
				// ---- straight bars (H / V) ----
				case HatchStyle.Horizontal:                s.Direction = 0; s.BarThickness = 3f; s.BarGap = 4f; break;
				case HatchStyle.Vertical:                  s.Direction = 1; s.BarThickness = 3f; s.BarGap = 4f; break;

				// ---- plain diagonals ----
				case HatchStyle.ForwardDiagonal:           s.Direction = 3; s.BarThickness = 3f; s.BarGap = 4f; break;   // "/"
				case HatchStyle.BackwardDiagonal:          s.Direction = 2; s.BarThickness = 3f; s.BarGap = 4f; break;   // "\"

				// ---- cross-hatches ----
				case HatchStyle.Cross:                     s.Direction = 0; s.SecondDirection = true; s.BarThickness = 1f; s.BarGap = 3f; break;
				case HatchStyle.DiagonalCross:             s.Direction = 0; s.SecondDirection = true; s.BarThickness = 2f; s.BarGap = 5f; s.Direction = 3; break; // both diag
				case HatchStyle.Divot:                     s.Mode = 1; s.DotSpacing = 5f; s.DotRadius = 1.2f; s.Staggered = true; break;

				// ---- percents (dots) ----
				case HatchStyle.Percent05:                 Percent(s, 5f); break;
				case HatchStyle.Percent10:                 Percent(s, 10f); break;
				case HatchStyle.Percent20:                 Percent(s, 20f); break;
				case HatchStyle.Percent25:                 Percent(s, 25f); break;
				case HatchStyle.Percent30:                 Percent(s, 30f); break;
				case HatchStyle.Percent40:                 Percent(s, 40f); break;
				case HatchStyle.Percent50:                 Percent(s, 50f); break;
				case HatchStyle.Percent60:                 Percent(s, 60f); break;
				case HatchStyle.Percent70:                 Percent(s, 70f); break;
				case HatchStyle.Percent75:                 Percent(s, 75f); break;
				case HatchStyle.Percent80:                 Percent(s, 80f); break;
				case HatchStyle.Percent90:                 Percent(s, 90f); break;

				// ---- light / dark / narrow / wide ----
				case HatchStyle.LightDownwardDiagonal:     s.Direction = 2; s.BarThickness = 1f; s.BarGap = 5f; break;
				case HatchStyle.LightUpwardDiagonal:       s.Direction = 3; s.BarThickness = 1f; s.BarGap = 5f; break;
				case HatchStyle.DarkDownwardDiagonal:      s.Direction = 2; s.BarThickness = 3f; s.BarGap = 3f; break;
				case HatchStyle.DarkUpwardDiagonal:        s.Direction = 3; s.BarThickness = 3f; s.BarGap = 3f; break;
				case HatchStyle.WideDownwardDiagonal:      s.Direction = 2; s.BarThickness = 5f; s.BarGap = 10f; break;
				case HatchStyle.WideUpwardDiagonal:        s.Direction = 3; s.BarThickness = 5f; s.BarGap = 10f; break;
				case HatchStyle.LightVertical:             s.Direction = 1; s.BarThickness = 1f; s.BarGap = 5f; break;
				case HatchStyle.LightHorizontal:           s.Direction = 0; s.BarThickness = 1f; s.BarGap = 5f; break;
				case HatchStyle.NarrowVertical:            s.Direction = 1; s.BarThickness = 2f; s.BarGap = 2f; break;
				case HatchStyle.NarrowHorizontal:          s.Direction = 0; s.BarThickness = 2f; s.BarGap = 2f; break;
				case HatchStyle.DarkVertical:              s.Direction = 1; s.BarThickness = 3f; s.BarGap = 3f; break;
				case HatchStyle.DarkHorizontal:            s.Direction = 0; s.BarThickness = 3f; s.BarGap = 3f; break;

				// ---- dashed diagonals ----
				case HatchStyle.DashedDownwardDiagonal:    s.Direction = 2; s.BarThickness = 2f; s.BarGap = 3f; s.Dashed = true; s.DashOn = 3f; s.DashOff = 3f; break;
				case HatchStyle.DashedUpwardDiagonal:      s.Direction = 3; s.BarThickness = 2f; s.BarGap = 3f; s.Dashed = true; s.DashOn = 3f; s.DashOff = 3f; break;
				case HatchStyle.DashedHorizontal:          s.Direction = 0; s.BarThickness = 2f; s.BarGap = 3f; s.Dashed = true; s.DashOn = 3f; s.DashOff = 3f; break;
				case HatchStyle.DashedVertical:            s.Direction = 1; s.BarThickness = 2f; s.BarGap = 3f; s.Dashed = true; s.DashOn = 3f; s.DashOff = 3f; break;

				// ---- confetti ----
				case HatchStyle.SmallConfetti:             s.Mode = 10; s.DotSpacing = 9f; s.DotRadius = 1.1f; break;
				case HatchStyle.LargeConfetti:             s.Mode = 10; s.DotSpacing = 14f; s.DotRadius = 2.2f; break;

				// ---- organic ----
				case HatchStyle.ZigZag:                    s.Mode = 3; s.DotSpacing = 10f; s.BarThickness = 4f; s.BarGap = 6f; break;
				case HatchStyle.Wave:                      s.Mode = 4; s.DotSpacing = 18f; s.BarThickness = 4f; s.BarGap = 8f; break;

				// ---- bricks ----
				case HatchStyle.DiagonalBrick:             s.Mode = 6; s.BarThickness = 2f; s.BarGap = 8f; break;
				case HatchStyle.HorizontalBrick:          s.Mode = 5; s.BarThickness = 2f; s.BarGap = 8f; break;

				// ---- textile ----
				case HatchStyle.Weave:                     s.Mode = 7; s.DotSpacing = 8f; s.BarThickness = 2f; s.BarGap = 2f; break;
				case HatchStyle.Plaid:                     s.Mode = 8; s.BarThickness = 1.5f; s.BarGap = 5f; break;
				case HatchStyle.Trellis:                   s.Mode = 0; s.Direction = 0; s.SecondDirection = true; s.BarThickness = 2.5f; s.BarGap = 3.5f; break;

				// ---- dots / lattice ----
				case HatchStyle.DottedGrid:                s.Mode = 1; s.DotSpacing = 7f; s.DotRadius = 1.6f; break;
				case HatchStyle.DottedDiamond:            s.Mode = 1; s.DotSpacing = 6f; s.DotRadius = 1.6f; s.Staggered = true; break;
				case HatchStyle.Sphere:                    s.Mode = 11; s.DotSpacing = 5f; s.DotRadius = 1.4f; break;

				// ---- grids / checkerboard / diamonds ----
				case HatchStyle.SmallGrid:                s.Mode = 0; s.Direction = 0; s.SecondDirection = true; s.BarThickness = 1.2f; s.BarGap = 2.5f; break;
				case HatchStyle.SmallCheckerBoard:        s.Mode = 2; s.DotSpacing = 5f; s.BarThickness = 2f; break;
				case HatchStyle.LargeCheckerBoard:        s.Mode = 2; s.DotSpacing = 10f; s.BarThickness = 4f; break;
				case HatchStyle.Shingle:                  s.Mode = 5; s.BarThickness = 1.6f; s.BarGap = 6f; break;
				case HatchStyle.OutlinedDiamond:          s.Mode = 9; s.BarThickness = 1.2f; s.BarGap = 6f; break;
				case HatchStyle.SolidDiamond:             s.Mode = 9; s.BarThickness = 5f; s.BarGap = 6f; break;
			}
			return s;
		}

		static void Percent(HatchSpec s, float pct)
		{
			s.Mode = 1;
			s.DotSpacing = 10f;
			float cover = pct / 100f;
			if (cover < 0.02f) cover = 0.02f;
			if (cover > 0.95f) cover = 0.95f;
			s.DotRadius = MathF.Sqrt(cover) * (s.DotSpacing / 2f);
		}
	}
}
