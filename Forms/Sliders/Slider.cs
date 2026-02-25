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
			SetBackColor (Theme.Colors.Base0);
			SetForeColor (Theme.Colors.Base2);
			SetBorderColor (Theme.Colors.Base00);
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
			Styles.SetStyle (new CircleSliderDisabledWidgetStyle (), WidgetStates.Disabled);

			DefaultWidth = 16;
			LineWidth = 4;
			GripperWidth = 12;
		}

		PointF DragStartPoint = PointF.Empty;
		public override void OnMouseDown (MouseButtonEventArgs e)
		{
			base.OnMouseDown (e);
			if (e.Button == MouseButton.Left) {
				DragStartPoint = new PointF (e.X, e.Y);
				if (Orientation == SliderOrientation.Horizontal)
					SetValidValue((e.X - (Bounds.Left + GripperWidth / 2)) / (Bounds.Width - GripperWidth));
				else
					SetValidValue(1.0f - ((e.Y - (Bounds.Top + GripperWidth / 2)) / (Bounds.Height - GripperWidth)));
			}
		}

		public override void OnMouseMove(MouseMoveEventArgs e)
		{
			base.OnMouseMove(e);
			
			if (DragStartPoint != Point.Empty) {
				if (Orientation == SliderOrientation.Horizontal)
				{
					if (ModifierKeys.ControlPressed)
						// Feineinstellung via Delta
						SetValidValue(Value + (e.DeltaX / 1000f)); 
					else
						// Absolute Position: (MausX - WidgetLinks) / WidgetBreite
						SetValidValue((e.X - (Bounds.Left + GripperWidth / 2)) / (Bounds.Width - GripperWidth));
				}
				else
				{
					if (ModifierKeys.ControlPressed)
						// Bei vertikalen Slidern ist 'oben' meist 1.0 und 'unten' 0.0
						SetValidValue(Value - (e.DeltaY / 1000f));
					else
						// Invertiert, falls 0 unten sein soll:
						SetValidValue(1.0f - ((e.Y - (Bounds.Top + GripperWidth / 2)) / (Bounds.Height - GripperWidth)));
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
			if (Orientation == SliderOrientation.Horizontal)
			{
				float center = bounds.Height / 2f;
				RectangleF rback = new RectangleF(bounds.Left, bounds.Top + center - (LineWidth / 2), bounds.Width, LineWidth);
				ctx.FillRectangle(Style.BackColorBrush, rback);

				RectangleF rval = new RectangleF(rback.Left, rback.Top, bounds.Width * Value, rback.Height);
				ctx.FillRectangle(Style.ForeColorBrush, rval);
			}
			else
			{
				float center = bounds.Width / 2f;
				RectangleF rback = new RectangleF(bounds.Left + center - (LineWidth / 2), bounds.Top, LineWidth, bounds.Height);
				ctx.FillRectangle(Style.BackColorBrush, rback);

				RectangleF rval = new RectangleF(rback.Left, rback.Bottom - (rback.Height * Value), LineWidth, rback.Height * Value);
				ctx.FillRectangle(Style.ForeColorBrush, rval);
			}
		}

		protected virtual void DrawGripper(IGUIContext ctx, RectangleF bounds)
		{
			float centerX;
			float centerY;

			if (Orientation == SliderOrientation.Horizontal)
			{
				centerX = bounds.Left + (GripperWidth / 2) + ((bounds.Width - GripperWidth)  * Value);
				centerY = bounds.Top + (bounds.Height / 2);
			}
			else
			{
				centerX = bounds.Left + (bounds.Width / 2);
				centerY = bounds.Bottom - (GripperWidth / 2) - ((bounds.Height - GripperWidth) * Value);				
			}
			ctx.FillCircle(Style.ForeColorBrush, centerX, centerY, GripperWidth / 2, ScaleFactor);
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

