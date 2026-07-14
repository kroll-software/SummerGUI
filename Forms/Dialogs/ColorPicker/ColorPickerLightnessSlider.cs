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
	public class ColorPickerLightnessSlider : ColorPickerSliderBase
    {
        private LinearGradientBrush topBrush;
        private LinearGradientBrush bottomBrush;
        private Color _baseColor = Color.Red;

        /// <summary>
        /// Die Basisfarbe (Sättigung + Hue), die in der Mitte des Sliders (bei 50% Lightness) liegt.
        /// </summary>
        public Color BaseColor
        {
            get => _baseColor;
            set
            {
                if (_baseColor != value)
                {
                    _baseColor = value;
                    UpdateGradients();
                    Invalidate();
                }
            }
        }

        public ColorPickerLightnessSlider(string name, Docking dock)
            : base(name, dock)
        {
            Tooltip = "Lightness";
            
            // Initialisierung der beiden Pinsel für die obere und untere Hälfte
            topBrush = new LinearGradientBrush(Color.White, _baseColor, GradientDirections.Vertical);
            bottomBrush = new LinearGradientBrush(_baseColor, Color.Black, GradientDirections.Vertical);
        }

        private void UpdateGradients()
        {
            // Wenn sich die Basisfarbe ändert, passen wir die Farbverläufe an
            topBrush.Color = Color.White;
            topBrush.GradientColor = _baseColor;

            bottomBrush.Color = _baseColor;
            bottomBrush.GradientColor = Color.Black;
        }

        public override void OnPaintBackground(IGUIContext ctx, RectangleF bounds)
        {
            base.OnPaintBackground(ctx, bounds);

            // 1. Hintergrund für den Gripper-Bereich (rechts) zeichnen            
            float barWidth = bounds.Width - GripperSize;

            RectangleF gripperBackgroundRect = new RectangleF(bounds.Left + barWidth, bounds.Top, GripperSize, bounds.Height);
            ctx.FillRectangle(Style.BackColorBrush, gripperBackgroundRect);

            // 2. Den eigentlichen Farbbalken (links) in zwei Hälften zeichnen
            float halfHeight = bounds.Height / 2f;
            RectangleF rectTop = new RectangleF(bounds.Left, bounds.Top, barWidth, halfHeight);
            RectangleF rectBottom = new RectangleF(bounds.Left, bounds.Top + halfHeight, barWidth, halfHeight);

            // Oben: Weiß -> BaseColor
            ctx.FillRectangle(topBrush, rectTop);
            // Unten: BaseColor -> Schwarz
            ctx.FillRectangle(bottomBrush, rectBottom);

            // 3. Einen feinen Rahmen um den gesamten Farbbalken zeichnen
            RectangleF fullBarRect = new RectangleF(bounds.Left, bounds.Top, barWidth, bounds.Height);
            ctx.DrawRectangle(Style.BorderColorPen, fullBarRect);
        }
    }
}