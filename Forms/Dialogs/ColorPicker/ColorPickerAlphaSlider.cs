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
	public class ColorPickerAlphaSlider : ColorPickerSliderBase
    {
        private LinearGradientBrush brush;
        private Color _baseColor = Color.Red;

        /// <summary>
        /// Die aktuelle Basisfarbe (inkl. Lightness und Sättigung), die für den Alpha-Verlauf verwendet wird.
        /// </summary>
        public Color BaseColor
        {
            get => _baseColor;
            set
            {
                if (_baseColor != value)
                {
                    _baseColor = value;
                    UpdateGradient();
                    Invalidate();
                }
            }
        }

        public ColorPickerAlphaSlider(string name, Docking dock)
            : base(name, dock)
        {
            Tooltip = "Opacity (Alpha)";

            // Wir starten mit einem Verlauf von Transparent-Rot zu Voll-Rot
            brush = new LinearGradientBrush(
                Color.FromArgb(0, _baseColor), 
                Color.FromArgb(255, _baseColor), 
                GradientDirections.Vertical
            );

            // Das Schachbrettmuster als Kachel-Hintergrund für das gesamte Widget setzen
            string filePath = "Assets/Images/img_bg_transparent.gif".FixedExpandedPath();        
            TextureImage image = TextureImage.FromFile(filePath);
            var backgroundsettings = new BackgroundImageSettings(image);
            backgroundsettings.SizeMode = ImageSizeModes.TileAll;            
            this.BackgroundImage = backgroundsettings;
        }

        private void UpdateGradient()
        {
            // Wir aktualisieren den Pinsel mit der neuen Farbe.
            // Wichtig: StartColor hat Alpha = 0 (transparent), EndColor hat Alpha = 255 (voll deckend).
            brush.Color = Color.FromArgb(0, _baseColor);
            brush.GradientColor = Color.FromArgb(255, _baseColor);
        }

        public override void OnPaintBackground(IGUIContext ctx, RectangleF bounds)
        {
            // 1. Zuerst zeichnet die Basisklasse das Schachbrettmuster im Hintergrund
            base.OnPaintBackground(ctx, bounds);

            float actualGripperAreaWidth = GripperSize * this.ScaleFactor;
            float barWidth = bounds.Width - actualGripperAreaWidth;

            // 2. Den Gripper-Bereich rechts mit der Standard-Hintergrundfarbe überdecken,
            // damit dort kein Schachbrett oder Alpha-Balken durchscheint.
            RectangleF gripperBackgroundRect = new RectangleF(bounds.Left + barWidth, bounds.Top, actualGripperAreaWidth, bounds.Height);
            ctx.FillRectangle(Style.BackColorBrush, gripperBackgroundRect);

            // 3. Den Alpha-Verlauf (links) zeichnen.
            // Durch OpenGL-Mischung wird das darunterliegende Schachbrett oben perfekt sichtbar sein,
            // während es nach unten hin weich in der Farbe verschwindet.
            RectangleF barRect = new RectangleF(bounds.Left, bounds.Top, barWidth, bounds.Height);
            ctx.FillRectangle(brush, barRect);

            // 4. Feiner Rahmen um den Farbbalken
            ctx.DrawRectangle(Style.BorderColorPen, barRect);
        }
    }

}