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
    public class ColorPickerSliderStyle : WidgetStyle
    {
        public override void InitStyle ()
		{
			SetBackColor (Color.White);
			SetForeColor (Color.Black);
			SetBorderColor (Color.SlateGray);
		}
    }

	public abstract class ColorPickerSliderBase : Container
    {
        private bool isDragging = false;

        public int Value { get; set; } = 0;
        public int MinValue { get; set; } = 0;
        public int MaxValue { get; set; } = 255; // Standard für RGB/Alpha, für Hue einfach auf 360 setzen

        public event EventHandler<int> ValueChanged;        

        [DpiScalable]
        public float GripperSize { get; set; }

        public ColorPickerSliderBase(string name, Docking dock)
            : base(name, dock, new ColorPickerSliderStyle())
        {            
            GripperSize = 8;
        }

        public override SizeF PreferredSize(IGUIContext ctx, SizeF proposedSize)
        {
            // Breite von 18 Pixeln (inkl. Platz für den Gripper rechts)
            return this.ClampMinMax(new SizeF(18, proposedSize.Height));
        }

        private void UpdateValueFromMouse(float mouseY)
        {
            // Wir ziehen die halbe Gripper-Größe ab, damit der Gripper an den 
            // extremen Rändern (ganz oben / ganz unten) nicht aus dem Widget heraussteht.
            float trackHeight = Bounds.Height - GripperSize;
            if (trackHeight <= 0) return;

            float relativeY = (mouseY - Bounds.Y - (GripperSize / 2f)) / trackHeight;
            
            // Clamping auf 0.0 bis 1.0
            relativeY = Math.Clamp(relativeY, 0f, 1f);

            // Wert berechnen
            int range = MaxValue - MinValue;
            int newValue = MinValue + (int)Math.Round(relativeY * range);

            if (newValue != Value)
            {
                Value = newValue;
                ValueChanged?.Invoke(this, Value);
                Invalidate(); // Neu zeichnen triggern
            }
        }

        public override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Button1) // Links-Klick
            {
                isDragging = true;
                UpdateValueFromMouse(e.Y);
            }
        }

        public override void OnMouseMove(MouseMoveEventArgs e)
        {
            base.OnMouseMove(e);
            if (isDragging)
            {
                UpdateValueFromMouse(e.Y);
            }
        }

        public override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Button1)
            {
                isDragging = false;
            }
        }

        protected void DrawGripper(IGUIContext ctx, RectangleF bounds)
        {
            float trackHeight = bounds.Height - GripperSize;
            if (trackHeight <= 0) return;

            // 1. Berechne die prozentuale Position des aktuellen Werts (0.0 bis 1.0)
            float pct = (float)(Value - MinValue) / (MaxValue - MinValue);
            pct = Math.Clamp(pct, 0f, 1f);

            // 2. Berechne das Y-Zentrum des Grippers
            float yCenter = bounds.Top + (GripperSize / 2f) + (pct * trackHeight);
            float size = GripperSize * this.ScaleFactor;

            // 3. Dreieckspunkte berechnen (Spitze zeigt nach links)
            // Die Spitze (p1) zeigt nach links ins Widget hinein. 
            // Die Basis (p2, p3) liegt flach auf dem rechten Rand des Widgets auf.
            PointF p1 = new PointF(bounds.Right - size, yCenter);                 // Spitze links
            PointF p2 = new PointF(bounds.Right, yCenter - (size / 2f));         // Ecke oben rechts
            PointF p3 = new PointF(bounds.Right, yCenter + (size / 2f));         // Ecke unten rechts

            PointF[] points = [p1, p2, p3];

            // Mit der High-Level GDI-API deines Contexts zeichnen
            ctx.FillPolygon(Style.ForeColorBrush, points);
        }

        public override void OnPaint(IGUIContext ctx, RectangleF bounds)
        {
            base.OnPaint(ctx, bounds);
            DrawGripper(ctx, bounds);
        }
    }
}