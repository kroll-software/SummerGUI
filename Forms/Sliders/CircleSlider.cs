using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Drawing;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Input;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using KS.Foundation;

namespace SummerGUI
{
	public class CircleSliderWidgetStyle : ProgressBarWidgetStyle
	{
		public override void InitStyle ()
		{
			SetBackColor (Theme.Colors.Base03);
			SetForeColor (Theme.Colors.Base3);
			SetBorderColor (Theme.Colors.Base02);
		}

		public override void PaintBackground (IGUIContext ctx, Widget widget)
		{
			// intentionally left blank.
		}
	}

	public class CircleSliderDisabledWidgetStyle : CircleSliderWidgetStyle
	{
		public override void InitStyle ()
		{
			SetBackColor (Theme.Colors.Base0);
			SetForeColor (Theme.Colors.Base2);
			SetBorderColor (Theme.Colors.Base00);
		}
	}

	public class CircleSlider : SliderBase
	{		
		
		float m_Radius;
		[DpiScalable]
		public float Radius 
		{ 
			get { 
				return m_Radius;
			}
			set {
				if (m_Radius != value) {
					m_Radius = value;
					OnRadiusChanged ();
				}
			}
		}

		protected virtual void OnRadiusChanged()
		{
			ResetCachedLayout ();
		}

		public ColorContexts ColorContext { get; set; }
		public Color CustomColor { get; set; }

		IGUIFont m_Font;
		public IGUIFont Font 
		{ 
			get {
				return m_Font;
			}
			set {
				if (m_Font != value) {
					m_Font = value;
					OnFontChanged ();
				}
			}
		}

		protected virtual void OnFontChanged()
		{
			ResetCachedLayout ();
		}

		public CircleSlider (string name, ColorContexts context)
			: base(name, Docking.Fill, new CircleSliderWidgetStyle())
		{
			ColorContext = context;
			Styles.SetStyle (new CircleSliderDisabledWidgetStyle(), WidgetStates.Disabled);

			Font = FontManager.Manager.DefaultFont;
			Radius = 48;
			//Padding = new Padding (3);
		}			

		PointF DragStartPoint = PointF.Empty;
		public override void OnMouseDown (MouseButtonEventArgs e)
		{
			base.OnMouseDown (e);
			if (e.Button == MouseButton.Left) {
				DragStartPoint = new PointF (e.X, e.Y);
			}
		}

		public override void OnMouseMove(MouseMoveEventArgs e)
		{
			base.OnMouseMove(e);
			
			// Wir reagieren nur, wenn der Drag aktiv ist
			if (DragStartPoint != PointF.Empty && e.DeltaY != 0) 
			{
				float range = MaxValue - MinValue;

				if (ModifierKeys.ControlPressed)
				{
					// Feineinstellung: 1/1000 der Range pro Pixel
					SetValidValue(Value - (e.DeltaY * (range / 1000f)));
				}
				else
				{
					// Deine Logik: Die Range wird über die Strecke des halben Umfangs verteilt.
					// DeltaY abziehen, da "Maus runter" meist "Wert kleiner" bedeutet.
					float divisor = Radius * 3.1415f; 
					if (divisor > 0)
					{
						float deltaValue = (e.DeltaY / divisor) * range;
						SetValidValue(Value - deltaValue);
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
				CachedPreferredSize = new SizeF (Radius * 2f + Padding.Width, Radius * 2f + Padding.Height);
			}
			return CachedPreferredSize;
		}

		public override Widget HitTest (float x, float y)
		{						
			return ((new PointF (Bounds.Left + Width / 2, Bounds.Top + Height / 2))
				.Distance (new PointF (x, y)) <= Radius) ? this : null;
		}

		public override void OnPaint(IGUIContext ctx, RectangleF bounds)
		{
			base.OnPaint(ctx, bounds);

			bounds.Inflate(-Padding.Width, -Padding.Height);
			bounds.Offset(Padding.Left, Padding.Top);

			PointF CenterPoint = new PointF(
				bounds.Left + (bounds.Width / 2f),
				bounds.Top + (bounds.Height / 2f));

			// 1. Normalisierten Wert berechnen (0.0 bis 1.0)			
			float normalized = NormalizedValue;
			float sweepAngle = 360f * normalized;

			float pieRadius = Radius - Math.Max(2, (Radius / 10f));

			// --- Hintergrund & Glättung ---
			// Tipp für Smoothness: Zeichne den Hintergrundkreis minimal größer 
			// als den Pie, um "Blitzer" an den Rändern zu vermeiden.
			using (Pen pen = new Pen(Color.Gray, 1f)) {
				ctx.DrawCircle(pen, CenterPoint.X, CenterPoint.Y, Radius);
			}               

			using (Brush brush = new SolidBrush(Style.BorderColorPen.Color)) {
				ctx.FillCircle(brush, CenterPoint.X, CenterPoint.Y, Radius);
			}

			// --- Daten-Farbe ---
			Color dataColor;
			if (!Enabled)
				dataColor = Theme.Colors.Base1;
			else if (CustomColor != Color.Empty)
				dataColor = CustomColor;
			else
				dataColor = Theme.GetContextColor(ColorContext);

			// --- Der Fortschritts-Pie ---
			using (Brush dataBrush = new SolidBrush(dataColor)) {
				ctx.FillPie(dataBrush,
					CenterPoint.X, 
					CenterPoint.Y,
					pieRadius, 
					pieRadius,				
					0, // Start bei 12 Uhr (oben)
					sweepAngle);
			}               
				
			// Das "Loch" in der Mitte für den Ring-Look
			ctx.FillCircle(Style.BackColorBrush, CenterPoint.X, CenterPoint.Y, Radius * 0.5f);         

			// --- Text-Anzeige ---
			DrawLabel(ctx, bounds);			
		}

		protected virtual void DrawLabel(IGUIContext ctx, RectangleF bounds)
		{
			string percentText = (NormalizedValue * 100f).ToString("n0") + "%";
			ctx.DrawString(percentText, Font, Style.ForeColorBrush,
				bounds, FontFormat.DefaultSingleLineCentered);
		}

		protected override void CleanupManagedResources ()
		{
			m_Font = null;
			base.CleanupManagedResources ();
		}
	}
}

