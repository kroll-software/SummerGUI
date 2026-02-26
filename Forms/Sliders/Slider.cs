using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using KS.Foundation;

namespace SummerGUI
{
	public class SliderWidgetStyle : WidgetStyle
	{
		public override void InitStyle ()
		{
			SetBackColor (Theme.Colors.HighLightBlueTransparent);
			SetForeColor (Theme.Colors.HighLightBlue);
			SetBorderColor (Theme.Colors.Base02);
		}

		public override void PaintBackground (IGUIContext ctx, Widget widget)
		{
			// intentionally left blank.
		}
	}

	public class SliderDisabledWidgetStyle : SliderWidgetStyle
	{
		public override void InitStyle ()
		{
			SetBackColor (Theme.Colors.Base2);
			SetForeColor (Theme.Colors.Base1);
			SetBorderColor (Theme.Colors.Base02);
		}
	}

	public enum SliderOrientation
	{
		Horizontal,
		Vertical,			
	}

	public class Slider : SliderBase
	{		
		public ColorContexts ColorContext { get; set; }
		public Color CustomColor { get; set; }

		SliderOrientation Orientation { get; set; }

		public float DefaultWidth { get; set; }
		public float LineWidth { get; set; }
		public float GripperWidth { get; set; }

		public Slider (string name, SliderOrientation orientation, ColorContexts context = ColorContexts.Default)
			: base(name, Docking.Fill, new SliderWidgetStyle())
		{
			Orientation = orientation;
			ColorContext = context;
			Styles.SetStyle (new SliderDisabledWidgetStyle(), WidgetStates.Disabled);

			DefaultWidth = 16;
			LineWidth = 4;
			GripperWidth = 12;
		}

		PointF DragStartPoint = PointF.Empty;
		public override void OnMouseDown(MouseButtonEventArgs e)
		{
			base.OnMouseDown(e);
			if (e.Button == MouseButton.Left) 
			{
				DragStartPoint = new PointF(e.X, e.Y);
				
				float ratio;
				float range = MaxValue - MinValue;

				if (Orientation == SliderOrientation.Horizontal)
				{
					// 1. Berechne die relative Position (0.0 bis 1.0) innerhalb des nutzbaren Bereichs
					float usableWidth = Bounds.Width - GripperWidth;
					if (usableWidth <= 0) ratio = 0;
					else ratio = (e.X - (Bounds.Left + GripperWidth / 2f)) / usableWidth;
				}
				else
				{
					// 2. Vertikal (invertiert, da 0 meist oben ist)
					float usableHeight = Bounds.Height - GripperWidth;
					if (usableHeight <= 0) ratio = 0;
					else ratio = 1.0f - ((e.Y - (Bounds.Top + GripperWidth / 2f)) / usableHeight);
				}

				// 3. Den Ratio-Wert auf den Bereich MinValue...MaxValue umrechnen
				float newValue = MinValue + (ratio * range);
				
				SetValidValue(newValue);
				Invalidate(2); // Neu zeichnen, damit der Gripper direkt springt
			}
		}

		public override void OnMouseMove(MouseMoveEventArgs e)
		{
			base.OnMouseMove(e);
			
			// Prüfen, ob wir uns im Drag-Modus befinden
			if (DragStartPoint != PointF.Empty) 
			{
				float range = MaxValue - MinValue;
				
				if (Orientation == SliderOrientation.Horizontal)
				{
					if (ModifierKeys.ControlPressed)
					{
						// Feineinstellung: Delta skaliert mit dem Wertebereich
						// (1/1000 des Bereichs pro Pixel Bewegung)
						SetValidValue(Value + (e.DeltaX * (range / 1000f)));
					}
					else
					{
						// Absolute Position: Ratio berechnen und auf Range mappen
						float usableWidth = Bounds.Width - GripperWidth;
						float ratio = (usableWidth <= 0) ? 0 : (e.X - (Bounds.Left + GripperWidth / 2f)) / usableWidth;
						SetValidValue(MinValue + (ratio * range));
					}
				}
				else
				{
					if (ModifierKeys.ControlPressed)
					{
						// Vertikal: DeltaY ist bei OpenTK nach unten positiv, 
						// Slider-Werte steigen meist nach oben -> Minus verwenden
						SetValidValue(Value - (e.DeltaY * (range / 1000f)));
					}
					else
					{
						// Absolute Position Vertikal (Invertiert)
						float usableHeight = Bounds.Height - GripperWidth;
						float ratio = (usableHeight <= 0) ? 0 : 1.0f - ((e.Y - (Bounds.Top + GripperWidth / 2f)) / usableHeight);
						SetValidValue(MinValue + (ratio * range));
					}
				}
								
				Invalidate(2);
			}
		}

		public override void OnMouseUp (MouseButtonEventArgs e)
		{			
			base.OnMouseUp (e);
			DragStartPoint = Point.Empty;
		}
			
		public override SizeF PreferredSize (IGUIContext ctx, SizeF proposedSize)
		{
			if (CachedPreferredSize == SizeF.Empty) {

				if (Orientation == SliderOrientation.Horizontal)
				{
					CachedPreferredSize = new SizeF (proposedSize.Width, DefaultWidth + Padding.Width);
				}
				else
				{
					CachedPreferredSize = new SizeF (DefaultWidth + Padding.Width, proposedSize.Height);
				}				
			}
			return CachedPreferredSize;
		}

        public override void OnPaint(IGUIContext ctx, RectangleF bounds)
        {
            base.OnPaint(ctx, bounds);

			DrawSliderLine(ctx, bounds);
			DrawTicks(ctx, bounds);
			DrawGripper(ctx, bounds);
        }		

		protected virtual void DrawSliderLine(IGUIContext ctx, RectangleF bounds)
		{
			float norm = NormalizedValue; // Einmal berechnen für Performance

			if (Orientation == SliderOrientation.Horizontal)
			{
				float center = bounds.Height / 2f;
				RectangleF rback = new RectangleF(bounds.Left, bounds.Top + center - (LineWidth / 2), bounds.Width, LineWidth);
				ctx.FillRectangle(Style.BackColorBrush, rback);

				// Breite basiert jetzt auf dem normalisierten Wert
				RectangleF rval = new RectangleF(rback.Left, rback.Top, bounds.Width * norm, rback.Height);
				ctx.FillRectangle(Style.ForeColorBrush, rval);
			}
			else
			{
				float center = bounds.Width / 2f;
				RectangleF rback = new RectangleF(bounds.Left + center - (LineWidth / 2), bounds.Top, LineWidth, bounds.Height);
				ctx.FillRectangle(Style.BackColorBrush, rback);

				// Höhe und Position basieren auf dem normalisierten Wert
				float fillHeight = rback.Height * norm;
				RectangleF rval = new RectangleF(rback.Left, rback.Bottom - fillHeight, LineWidth, fillHeight);
				ctx.FillRectangle(Style.ForeColorBrush, rval);
			}
		}

		protected virtual void DrawGripper(IGUIContext ctx, RectangleF bounds)
		{
			float centerX;
			float centerY;
			float norm = NormalizedValue;

			if (Orientation == SliderOrientation.Horizontal)
			{
				// Der Gripper bewegt sich innerhalb der verfügbaren Breite minus Gripper-Breite
				centerX = bounds.Left + (GripperWidth / 2) + ((bounds.Width - GripperWidth) * norm);
				centerY = bounds.Top + (bounds.Height / 2);
			}
			else
			{
				centerX = bounds.Left + (bounds.Width / 2);
				// Unten ist MinValue (norm=0), Oben ist MaxValue (norm=1)
				centerY = bounds.Bottom - (GripperWidth / 2) - ((bounds.Height - GripperWidth) * norm);                
			}
			ctx.FillCircle(Style.ForeColorBrush, centerX, centerY, (GripperWidth / 2) * ScaleFactor);
		}

		protected virtual void DrawTicks(IGUIContext ctx, RectangleF bounds)
		{
			if (Orientation == SliderOrientation.Horizontal)
			{
				
			}
			else
			{
				
			}
		}
	}	
}

