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
using KS.Foundation;

namespace SummerGUI
{
	public class ProgressBarWidgetStyle : WidgetStyle
	{		
		public override void InitStyle ()
		{
			SetBackColor (Theme.CurrentTheme.ProgressBar.ProgressBackColor);
			SetForeColor (Theme.CurrentTheme.ProgressBar.ProgressForeColor);
			SetBorderColor (Theme.CurrentTheme.ProgressBar.ProgressBackColor);
		}

		public override void PaintBackground (IGUIContext ctx, Widget widget)
		{			
			ctx.FillRectangle (BackColorBrush, widget.PaddingBounds);
			this.DrawBorder (ctx, widget);
			//ctx.DrawRectangle (BorderColorPen, widget.MarginBounds);
		}
	}

	public class ProgressBar : Widget
	{
		private float m_Value = 0;

		/// <summary>
		/// Gets or sets the value.
		/// </summary>
		/// <value>The value between 0 and 1.</value>
		public float Value
		{
			get {
				return m_Value;
			}
			set {
				value = Math.Max (0, Math.Min (1, value));
				if (Math.Abs (value - m_Value) > 0.001) {
					m_Value = value;
					this.Invalidate ();

					if (infiniteDirection != 0) {
						if ((infiniteDirection > 0 && m_Value >= (1 - 0.001)) || (infiniteDirection < 0 && m_Value <= (0.251)))
							RestartInfinite ();							
					}
				}
			}
		}

		int infiniteDirection = 0;
		public void StartInfinite()
		{
			if (infiniteDirection == 0) {
				infiniteDirection = 1;
				ParentWindow.Animator.AddAnimation (this, "Value", 0.25, 1, 5);
			}
		}

		public void StopInfinite()
		{			
			infiniteDirection = 0;
			ParentWindow.Animator.Cancel (this);
			Value = 0;
			Invalidate ();
		}

		private void RestartInfinite()
		{
			if (infiniteDirection > 0) {
				infiniteDirection = -1;
				ParentWindow.Animator.AddAnimation (this, "Value", 1, 0.25, 5);
			} else if (infiniteDirection < 0) {
				infiniteDirection = 1;
				ParentWindow.Animator.AddAnimation (this, "Value", 0.25, 1, 5);
			} else {
				this.LogWarning ("Invalid infinite ProgressBar state.");
			}
		}

		bool m_Autosize;
		public bool Autosize 
		{ 
			get {
				return m_Autosize;
			}
			set {
				if (m_Autosize != value) {
					m_Autosize = value;
					OnAutosizeChanged ();
				}
			}
		}

		protected virtual void OnAutosizeChanged()
		{
			ResetCachedLayout ();
		}

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

		public Brush ProgressColorBrush { get; set; }

		public ProgressBar (string name)
			: base(name, Docking.Fill, new ProgressBarWidgetStyle())
		{			
			// ToDo: DPI-Scaling
			Padding = new Padding (6);
			ProgressPadding = 3;
			Autosize = true;
			Font = FontManager.Manager.DefaultFont;
			ProgressColorBrush = new SolidBrush (Theme.Colors.Orange);
			InvalidateOnHeartBeat = true;
		}			

		public override SizeF PreferredSize (IGUIContext ctx, SizeF proposedSize)
		{
			if (CachedPreferredSize == SizeF.Empty) {
				if (!Autosize || Font == null)
					CachedPreferredSize = base.PreferredSize (ctx, proposedSize);
			
				CachedPreferredSize = new SizeF (proposedSize.Width, Font.TextBoxHeight + Padding.Height);
			}
			return CachedPreferredSize;
		}

		public virtual string Text
		{
			get{
				return (Value * 100f).ToString ("n1") + " %";
			}
		}

		float m_ProgressPadding;
		[DpiScalable]
		public float ProgressPadding 
		{ 
			get {
				return m_ProgressPadding;
			}
			set {
				if (m_ProgressPadding != value) {
					m_ProgressPadding = value;
					OnProgressPaddingChanged ();
				}
			}
		}

		protected virtual void OnProgressPaddingChanged()
		{
			ResetCachedLayout ();
		}

		public override void OnPaint (IGUIContext ctx, RectangleF bounds)
		{
			base.OnPaint (ctx, bounds);

			// paint bar
			RectangleF rb = PaddingBounds;

			if (ProgressColorBrush != null && Value > 0.01f) {
				if (ProgressPadding > 0)
					rb.Inflate (-ProgressPadding, -ProgressPadding);				
				RectangleF rbp = rb;
				float delta = 0;
				if (infiniteDirection != 0) {
					delta = rb.Width * 0.25f;
					rbp = rb;
					rbp.Width = (int)(rbp.Width * Value + 0.5f);
					float left = rbp.Right - delta;
					rbp = new RectangleF (left, rbp.Top, rbp.Right - left, rbp.Height);
				} else {			
					rbp = rb;
					rbp.Width = (int)(rbp.Width * Value + 0.5f);
				}
				if (rbp.Width > 0 && rbp.Height > 0)
					ctx.FillRectangle (ProgressColorBrush, rbp);
			}

			// paint progress-text
			if (Font != null && infiniteDirection == 0)
				ctx.DrawString(Text, Font, Style.ForeColorBrush, rb, FontFormat.DefaultSingleLineCentered);
		}

		protected override void CleanupManagedResources ()
		{
			Font = null;
			base.CleanupManagedResources ();
		}
	}
}

