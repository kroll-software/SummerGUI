using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using KS.Foundation;

namespace SummerGUI
{
	public class DefaultCaptionPanelStyle : GradientWidgetStyle
	{
		public override void InitStyle ()
		{
			SetBackColor (Theme.Colors.Base01, Theme.Colors.Base02);
			SetForeColor (Theme.Colors.Base3);
			SetBorderColor (Color.Empty);
		}
	}

	public class ActiveCaptionPanelStyle : GradientWidgetStyle
	{
		public override void InitStyle ()
		{
			SetBackColor (Theme.Colors.LightHighLightButton, Theme.Colors.HighLightButton);
			SetForeColor (Theme.Colors.Base03);
			SetBorderColor (Color.Empty);
		}
	}

	public class FlatCaptionPanelStyle : GradientWidgetStyle
	{
		public override void InitStyle ()
		{
			SetBackColor (Theme.Colors.Base02, Theme.Colors.Base02);
			SetForeColor (Theme.Colors.Base2);
			SetBorderColor (Color.Empty);
		}
	}

	public class PanelCaption : TextWidget
	{
		bool m_CanCollapse;
		public bool CanCollapse 
		{ 
			get {
				return m_CanCollapse;
			}
			set {
				if (m_CanCollapse != value) {
					m_CanCollapse = value;

					/***
					if (m_CanCollapse)
						Padding = new Padding (6, 6, 24, 6);
					else
						Padding = new Padding (6);					
					***/
				}
			}
		}

		bool m_Collapsed;
		public bool Collapsed 
		{ 
			get {
				return m_CanCollapse && m_Collapsed;
			}
			set {				
				m_Collapsed = value;
			}
		}

		public bool IsActive { get; set; }

		public PanelCaption (string name, string text) : this (name, text, new DefaultCaptionPanelStyle()) {}
		public PanelCaption (string name, string text, IWidgetStyle style) 
			: base(name, Docking.Top, style, text, null)
		{
			Styles.SetStyle (new ActiveCaptionPanelStyle (), WidgetStates.Active);
			Format = FontFormat.DefaultSingleLine;			
			Padding = new Padding (6);
			ZIndex = 1000;
			TextOffsetY = -2;
		}

		public override void UpdateStyle()
		{
			if (IsActive)
				WidgetState = WidgetStates.Active;
			else
				WidgetState = WidgetStates.Default;			
		}

        public override void OnMouseEnter(IGUIContext ctx)
        {
            //base.OnMouseEnter(ctx);
        }

		public override void OnPaint (IGUIContext ctx, RectangleF bounds)
		{
			Padding oldPadding = Padding;
			if (CanCollapse)
				Padding = new Padding(Padding.Left, Padding.Top, Padding.Right + 18 * ScaleFactor, Padding.Bottom);

			base.OnPaint (ctx, bounds);
			Padding = oldPadding;

			if (!m_CanCollapse || IconFont == null)
				return;
			
			RectangleF iconRect = PaddingBounds;
			iconRect.Y += TextOffsetY;

			char icon;
			if (m_Collapsed)
				icon = (char)FontAwesomeIcons.fa_caret_right;
			else
				icon = (char)FontAwesomeIcons.fa_caret_down;
			
			ctx.DrawString (icon.ToString(), IconFont, Style.ForeColorBrush, iconRect, FontFormat.DefaultIconFontFormatFar);
		}

		public event EventHandler<EventArgs> CollapsedChanged;
		public void OnCollapsedChanged()
		{
			if (CollapsedChanged != null)
				CollapsedChanged (this, EventArgs.Empty);
		}

		public override void OnClick (MouseButtonEventArgs e)
		{
			base.OnClick (e);
			if (m_CanCollapse) {
				m_Collapsed = !m_Collapsed;
				OnCollapsedChanged ();
			}
		}
	}
}

