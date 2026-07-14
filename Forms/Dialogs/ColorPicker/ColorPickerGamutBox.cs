using System;
using System.Linq;
using System.Drawing;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using KS.Foundation;
using OpenTK.Windowing.Common;

namespace SummerGUI.ColorPicker
{
	public class ColorPickerGamutBox : Widget
    {
		Pen CirclePen;
		private Color4 _selectedColor;
		// Lokaler Zustand für Hue (0-1) und Saturation (0-1)
		private float _hue = 0.5f;
		private float _saturation = 0.5f;

		/// <summary>
		/// Der aktuell gewählte Farbwert (RGB) inkl. Alpha der Gamut Box
		/// </summary>
		public Color SelectedColor
		{
			get => _selectedColor.ToColor();
			set
			{
				if (_selectedColor != value)
				{
					_selectedColor = value;
					// Wenn die Farbe von außen gesetzt wird (z.B. über Hex-Eingabe),
					// müssen wir sie zurück in HSL rechnen, um den Cursor korrekt zu positionieren.
					RgbToHsl(_selectedColor, out _hue, out _saturation, out _);
					Invalidate(); // UI neu zeichnen
				}
			}
		}

		public event EventHandler<Color> ColorChanged;

        public ColorPickerGamutBox (string name, Docking dock, IWidgetStyle style)
			: base(name, dock, style)
		{		
			TabIndex = -1;
			CirclePen = new Pen(Color.Black, 1.5f);
			UpdateColorFromHsl();
		}

		private void UpdateColorFromHsl()
		{
			// Lightness steht in der Gamut Box fest auf 0.5f für maximale Farbkraft
			_selectedColor = HslToRgb(_hue, _saturation, 0.5f);			
			ColorChanged?.Invoke(this, _selectedColor.ToColor());			
			Invalidate();
		}

		private void HandleMouseInteraction(float mouseX, float mouseY)
		{
			if (Bounds.Width <= 0 || Bounds.Height <= 0) return;

			// Relative Position im Widget berechnen (0.0 bis 1.0)
			float rx = (mouseX - Bounds.X) / Bounds.Width;
			float ry = (mouseY - Bounds.Y) / Bounds.Height;

			// Clamping, falls die Maus beim Drücken das Widget verlässt
			rx = Math.Clamp(rx, 0f, 1f);
			ry = Math.Clamp(ry, 0f, 1f);

			// Zuweisung an HSL: X ist Hue, Y ist Sättigung (invertiert!)
			_hue = rx;
			_saturation = 1f - ry; // Oben = 1.0 (Volle Farbe), Unten = 0.0 (Graustufen)

			UpdateColorFromHsl();
		}

		bool m_leftButtonPressed = false;

		public override void OnMouseDown(MouseButtonEventArgs e)
		{
			base.OnMouseDown(e);
			if (e.Button == MouseButton.Left)
			{
				m_leftButtonPressed = true;
				HandleMouseInteraction(e.X, e.Y);
			}
		}        

        public override void OnMouseMove(MouseMoveEventArgs e)
        {
            base.OnMouseMove(e);
			if (m_leftButtonPressed) 
			{
				HandleMouseInteraction(e.X, e.Y);
			}
        }

		public override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);
			m_leftButtonPressed = false;
			
			if (e.Button == MouseButton.Left)
			{
				HandleMouseInteraction(e.X, e.Y);
			}			
        }

		public static Color4 HslToRgb(float h, float s, float l, float alpha = 1.0f)
		{
			float r, g, b;

			if (s == 0f)
			{
				// Graustufe, wenn keine Sättigung vorhanden ist
				r = g = b = l; 
			}
			else
			{
				float q = l < 0.5f ? l * (1f + s) : l + s - (l * s);
				float p = 2f * l - q;

				r = HueToRgb(p, q, h + 1f / 3f);
				g = HueToRgb(p, q, h);
				b = HueToRgb(p, q, h - 1f / 3f);
			}

			return new Color4(r, g, b, alpha);
		}

		private static void RgbToHsl(Color4 rgb, out float h, out float s, out float l)
		{
			float r = rgb.R;
			float g = rgb.G;
			float b = rgb.B;

			float min = Math.Min(r, Math.Min(g, b));
			float max = Math.Max(r, Math.Max(g, b));
			float delta = max - min;

			l = (max + min) / 2f;

			if (delta == 0f)
			{
				h = 0f;
				s = 0f;
			}
			else
			{
				s = l <= 0.5f ? delta / (max + min) : delta / (2f - max - min);

				if (r == max)
					h = (g - b) / delta + (g < b ? 6f : 0f);
				else if (g == max)
					h = (b - r) / delta + 2f;
				else
					h = (r - g) / delta + 4f;

				h /= 6f;
			}
		}

		private static float HueToRgb(float p, float q, float t)
		{
			if (t < 0f) t += 1f;
			if (t > 1f) t -= 1f;
			
			if (t < 1f / 6f) return p + (q - p) * 6f * t;
			if (t < 1f / 2f) return q;
			if (t < 2f / 3f) return p + (q - p) * (2f / 3f - t) * 6f;
			
			return p;
		}

        public override void OnPaintBackground(IGUIContext ctx, RectangleF bounds)
        {
            base.OnPaintBackground(ctx, bounds);

			int segmentsX = 32;
			int segmentsY = 16;

			float stepX = bounds.Width / segmentsX;
			float stepY = bounds.Height / segmentsY;

			for (int x = 0; x < segmentsX; x++)
			{
				for (int y = 0; y < segmentsY; y++)
				{
					// 1. Die physikalischen Bildschirm-Koordinaten für die Ecken berechnen
					var p1 = new Vector2(bounds.X + x * stepX,       bounds.Y + y * stepY);
					var p2 = new Vector2(bounds.X + (x + 1) * stepX, bounds.Y + y * stepY);
					var p3 = new Vector2(bounds.X + (x + 1) * stepX, bounds.Y + (y + 1) * stepY);
					var p4 = new Vector2(bounds.X + x * stepX,       bounds.Y + (y + 1) * stepY);

					// 2. Sättigung für das HSL-Mapping invertieren (1.0f - ...)
					// Dadurch ist die maximale Sättigung oben (y=0) und Grau unten (y=segmentsY)
					float satCurrent = 1.0f - ((float)y / segmentsY);
					float satNext    = 1.0f - ((float)(y + 1) / segmentsY);

					// 3. HSL für die Ecken mit den korrigierten Sättigungen füttern
					Color4 c1 = HslToRgb((float)x / segmentsX,       satCurrent, 0.5f); // Oben links
					Color4 c2 = HslToRgb((float)(x + 1) / segmentsX, satCurrent, 0.5f); // Oben rechts
					Color4 c3 = HslToRgb((float)(x + 1) / segmentsX, satNext,    0.5f); // Unten rechts
					Color4 c4 = HslToRgb((float)x / segmentsX,       satNext,    0.5f); // Unten links

					// Ab in deinen erweiterten Batcher
					ctx.Batcher.AddQuad(p1, p2, p3, p4, c1, c2, c3, c4);
				}
			}
        }        

        public override void OnPaint(IGUIContext ctx, RectangleF bounds)
        {
            base.OnPaint(ctx, bounds);

			// 2. Auswahl-Cursor berechnen und zeichnen
			float cx = bounds.X + (_hue * bounds.Width);
			float cy = bounds.Y + ((1f - _saturation) * bounds.Height); // Zurück-Invertierung für die UI-Koordinate
			float radius = 6f;

			// Tipp: Erst einen schwarzen Kreis zeichnen für Kontrast auf hellen Farben, 
			// dann den weißen Kreis direkt drüber.
			CirclePen.Color = Color.Black;
			ctx.DrawCircle(CirclePen, cx, cy, radius + 1f, this.ScaleFactor);
			
			CirclePen.Color = Color.White;
			ctx.DrawCircle(CirclePen, cx, cy, radius, this.ScaleFactor);
        }
    }
}