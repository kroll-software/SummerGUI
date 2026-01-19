using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using OpenTK.Input;
using KS.Foundation;

namespace SummerGUI
{	
	public class TooltipWidget : OverlayWidget
	{
		string m_Text;
		public string Text 
		{ 
			get {
				return m_Text;
			}
			set {
				if (m_Text != value) {
					m_Text = value;
					OnTextChanged ();
				}
			}
		}

		protected virtual void OnTextChanged()
		{
			//ResetCachedLayout ();
			if (Visible)
				Update(true);
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

		public override void OnClose ()
		{			
			AutoHideAction.Enabled = false;
			Visible = false;
			StartLocos = PointF.Empty;
			Text = null;
		}
			
		readonly DelayedAction AutoHideAction;

		public TooltipWidget (string text)
			: base(Guid.NewGuid().ToString(), new TooltipWidgetStyle())
		{
			OverlayMode = OverlayModes.Timer;
			Text = text;
			this.SetFontByTag (CommonFontTags.Default);

			Padding = new Padding (6, 2, 6, 4);
			//Padding = Padding.Empty;
			Margin = Padding.Empty;
			//MaxSize = new Size (240, Int32.MaxValue);
			Visible = false;
			Tooltip = null;
			Text = null;

			AutoHideAction = new DelayedAction (15000, StartAutoHide);
		}			

		public void OnShow ()
		{						
			bFadeOut = false;
			Style.AlphaBack = 240;
			Style.AlphaFore = 240;
			Style.AlphaBorder = 240;
			AutoHideAction.Start ();
			Visible = true;
			StartLocos = Locos;
		}			

		public PointF Locos { get; set; }
		public PointF StartLocos { get; set; }

		protected override void OnVisibleChanged ()
		{
			ResetCachedLayout ();
			// but we don't reset the parent here.
			// The Tooltip is very often shown and hidden.
			// it shouldnt force a complete layout all the time.
		}

		bool bFadeOut = false;
		private void StartAutoHide()
		{
			if (!Visible)
				return;
			bFadeOut = true;
			Invalidate (1);
		}

		public override SizeF PreferredSize (IGUIContext ctx, SizeF proposedSize)
		{
			if (CachedPreferredSize == SizeF.Empty) {
				if (Font == null || String.IsNullOrEmpty (Text)) {
					CachedPreferredSize = SizeF.Empty;
				} else {
					SizeF sz = Font.Measure (Text, MaxSize.Width, FontFormat.DefaultMultiLine);
					CachedPreferredSize = new SizeF (sz.Width + Padding.Width, sz.Height + Padding.Height);
				}
			}
			return CachedPreferredSize;
		}

		public override void OnLayout (IGUIContext ctx, RectangleF bounds)
		{		
			// intentionally left blank.
		}


		public override void OnPaint (IGUIContext ctx, RectangleF bounds)
		{						
			if (!Visible || Font == null || String.IsNullOrEmpty (Text))
				return;

			using (var clipChild = new ClipBoundClip (ctx, Bounds, true)) {
				ctx.DrawString (Text, Font, Style.ForeColorBrush, PaddingBounds, FontFormat.DefaultMultiLine);
			}

			// Fadeout animation
			if (bFadeOut) {	
				try {
					if (Style.AlphaBack >= 5) {
						Style.AlphaBack -= 5;
						Style.AlphaFore -= 5;
						Style.AlphaBorder -= 5;	
					}
				} catch (Exception ex) {
					ex.LogError ();
				} finally {
					if (Style.AlphaBack < 5)
						OnClose();				
					ctx.Invalidate (1);
				}
			}
		}

		protected override void CleanupManagedResources ()
		{			
			if (AutoHideAction != null)
				AutoHideAction.Dispose ();
			base.CleanupManagedResources ();
		}			
	}
}

