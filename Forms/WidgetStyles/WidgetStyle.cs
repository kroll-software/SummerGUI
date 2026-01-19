using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using KS.Foundation;

namespace SummerGUI
{
	public interface IStyleModelElement
	{
		IWidgetStyle Style { get; }
	}

	public interface IWidgetStyle
	{		
		void PaintBackground (IGUIContext ctx, Widget widget);
		void DrawBorder (IGUIContext ctx, Widget widget);
		void FillRectangle (IGUIContext ctx, Widget widget);
		Pen ForeColorPen { get; set; }
		Brush ForeColorBrush { get; set; }
		Brush BackColorBrush { get; set; }
		Pen BorderColorPen { get; set; }

		[DpiScalable]
		float Border { get; set; }

		[DpiScalable]
		float BorderDistance { get; set; }

		void InitStyle ();
		IWidgetStyle Clone ();

		byte AlphaBack { get; set; }
		byte AlphaFore { get; set; }
		byte AlphaBorder { get; set; }
	}

	public class WidgetStyle : IWidgetStyle
	{	
		//public WidgetStyle() : this (Color.Empty, Theme.Colors.Base03, Color.Empty) {}
		public WidgetStyle()
		{
			InitStyle ();
		}

		public WidgetStyle(Color back, Color front, Color border)
		{
			BackColorBrush = new SolidBrush (back);
			ForeColorPen = new  Pen(front);
			ForeColorBrush = new SolidBrush (front);
			BorderColorPen = new Pen (border);

			if (border != Color.Empty)
				Border = 1;			
		}
		/*** ***/
			
		/// <summary>
		/// Override but don't call base class, because it sets empty defaults
		/// </summary>
		public virtual void InitStyle ()
		{
			SetBackColor (Color.Empty);
			SetForeColor (Theme.Colors.Base03);
			SetBorderColor (Color.Empty);
		}

		public virtual void SetBackColor(Color color1)
		{			
			BackColorBrush = new SolidBrush (color1);
		}

		public virtual void SetForeColor(Color color)
		{
			ForeColorPen = new Pen (color);
			ForeColorBrush = new SolidBrush (color);
		}

		public virtual void SetBorderColor(Color color)
		{
			BorderColorPen = new Pen (color);
			if (color == Color.Empty) {
				BorderColorPen.Width = 0;
				BorderDistance = 0;
			} else {
				BorderColorPen.Width = 1;	
				BorderDistance = -(1 / 2f);
			}
		}

		public Pen ForeColorPen { get; set; }
		public Brush ForeColorBrush { get; set; }
		public Brush BackColorBrush { get; set; }

		[DpiScalable]
		public float BorderDistance { get; set; }

		public Pen BorderColorPen { get; set; }

		[DpiScalable]
		public float Border
		{
			get{
				if (BorderColorPen != null)
					return BorderColorPen.Width;
				else
					return 0;
			}
			set{
				if (BorderColorPen != null)
					BorderColorPen.Width = value;
				if (value > 0 && BorderDistance == 0)
					BorderDistance = -(value / 2);
				//BorderDistance = - value;
				//BorderDistance = -value;
			}
		}

		public virtual void DrawBorder(IGUIContext ctx, Widget widget)
		{
			if (BorderColorPen != null && BorderColorPen.Width > 0 && BorderColorPen.Color != Color.Empty) {
				RectangleF rBorder = widget.Bounds;
				if (Math.Abs(BorderDistance) > float.Epsilon)
					rBorder.Inflate (BorderDistance, BorderDistance);
				//rBorder = rBorder.Ceil ();
				//rBorder.Offset (-0.5f, -0.5f);
				ctx.DrawRectangle (BorderColorPen, rBorder);
			}
		}

		public virtual void FillRectangle(IGUIContext ctx, Widget widget)
		{
			if (BackColorBrush.Color != Color.Empty)
				ctx.FillRectangle (BackColorBrush, widget.Bounds);
		}

		public virtual void PaintBackground(IGUIContext ctx, Widget widget)
		{
			if (widget.CanPaint) {
				FillRectangle (ctx, widget);				
				DrawBorder (ctx, widget);
			}
		}

		// *** used for animations ***

		/// <summary>
		/// Gets or sets the background color transparency.
		/// </summary>
		/// <value>The alpha back.</value>
		public byte AlphaBack
		{
			get{
				return BackColorBrush.Color.A;
			}
			set{
				BackColorBrush.Color = Color.FromArgb (Math.Max((byte)0, Math.Min(value, (byte)255)), BackColorBrush.Color);
			}
		}

		/// <summary>
		/// Gets or sets the foreground color transparency.
		/// </summary>
		/// <value>The alpha fore.</value>
		public byte AlphaFore
		{
			get{
				return ForeColorBrush.Color.A;
			}
			set{
				ForeColorBrush.Color = Color.FromArgb (Math.Max((byte)0, Math.Min(value, (byte)255)), ForeColorBrush.Color);
				ForeColorPen.Color = ForeColorBrush.Color;
			}
		}

		/// <summary>
		/// Gets or sets the border color transparency.
		/// </summary>
		/// <value>The alpha border.</value>
		public byte AlphaBorder
		{
			get{
				return BorderColorPen.Color.A;
			}
			set{
				BorderColorPen.Color = Color.FromArgb (Math.Max((byte)0, Math.Min(value, (byte)255)), BorderColorPen.Color);
			}
		}
			
		public IWidgetStyle Clone()
		{
			return MemberwiseClone () as IWidgetStyle;
		}
	}

	public class EmptyWidgetStyle : WidgetStyle 
	{		
		public override void InitStyle ()
		{	
			SetBackColor (Color.Empty);
			SetForeColor (Color.Empty);
			SetBorderColor (Color.Empty);
		}
	}

	public class EmptyGradientWidgetStyle : GradientWidgetStyle 
	{		
		public override void InitStyle ()
		{	
			SetBackColor (Color.Empty, Color.Empty);
			SetForeColor (Color.Empty);
			SetBorderColor (Color.Empty);
		}
	}
}

