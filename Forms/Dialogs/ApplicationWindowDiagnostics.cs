using System;
using System.Linq;
using KS.Foundation;

namespace SummerGUI
{
	public class ApplicationWindowDiagnostics : DisposableObject
	{
		public class ApplicationWindowDiagnosticStyle : StatusBarStyle
		{
			public DiagnoseModes DiagnoseMode { get; set; }

			public override void InitStyle ()
			{
				base.InitStyle ();
				if (DiagnoseMode == DiagnoseModes.RelativeFrameDelay)
					SetForeColor (Theme.Colors.Base0);
				else
					SetForeColor (Theme.Colors.Magenta);
			}
		}

		public enum DiagnoseModes
		{
			RelativeFrameDelay,
			AbsoluteInterval
		}
			
		public ApplicationWindow Owner { get; private set; }
		public StatusTextPanel PanelLayout  { get; private set; }
		public StatusTextPanel PanelPaint { get; private set; }

		readonly FramePerformanceMeter LayoutMeter;
		readonly FramePerformanceMeter PaintMeter;

		int pulseLayoutFlag;
		public void PulseLayout()
		{
			if (!IsDisposed && Owner != null) {
				long value = LayoutMeter.Pulse ();
				pulseLayoutFlag--;
				if (pulseLayoutFlag < 0) {
					pulseLayoutFlag = 60;
					if (DiagnoseMode == DiagnoseModes.RelativeFrameDelay && Owner.LayoutFrameRate > 0)
						value = Math.Max (0, (value - (1000f / Owner.LayoutFrameRate)).Ceil ());
					PanelLayout.Text = String.Format ("Layout: {0} ms", value);
				}
			}
		}

		int pulsePaintFlag;
		public void PulsePaint()
		{
			if (!IsDisposed && Owner != null) {
				long value = PaintMeter.Pulse ();
				pulsePaintFlag--;
				if (pulsePaintFlag < 0) {
					pulsePaintFlag = 60;
					if (DiagnoseMode == DiagnoseModes.RelativeFrameDelay && Owner.PaintFrameRate > 0)
						value = Math.Max (0, (value - (1000f / Owner.PaintFrameRate)).Ceil ());
					PanelPaint.Text = String.Format ("Paint: {0} ms", value);
				}
			}
		}

		private DiagnoseModes m_DiagnoseMode;
		public DiagnoseModes DiagnoseMode 
		{ 
			get {
				return m_DiagnoseMode;
			}
			set {				
				m_DiagnoseMode = value;
				style.DiagnoseMode = value;
				style.InitStyle ();
				if (Owner != null)
					Owner.Invalidate ();
			}
		}

		ApplicationWindowDiagnosticStyle style;

		public ApplicationWindowDiagnostics (ApplicationWindow owner, int nFrames = 5)
		{
			Owner = owner;

			PanelPaint = new StatusTextPanel ("diagnostics_paint", Docking.Right);
			PanelLayout = new StatusTextPanel ("diagnostics_layout", Docking.Right);

			style = new ApplicationWindowDiagnosticStyle ();
			PanelPaint.Styles.SetStyle (style, WidgetStates.Default);
			PanelLayout.Styles.SetStyle (style, WidgetStates.Default);

			PanelPaint.Click += Panel_Click;
			PanelLayout.Click += Panel_Click;

			//PanelLayout.Style.BackColorBrush.Color = SolarizedColors.Green;
			//PanelPaint.Style.BackColorBrush.Color = SolarizedColors.Cyan;

			Owner.StatusBar.AddChild (PanelPaint);
			Owner.StatusBar.AddChild (PanelLayout);

			LayoutMeter = new FramePerformanceMeter (nFrames);
			PaintMeter = new FramePerformanceMeter (nFrames);

			PanelPaint.InvalidateOnHeartBeat = true;
			PanelLayout.InvalidateOnHeartBeat = true;

			//Owner.Controls.SubscribeHeartbeat (PanelPaint);
			//Owner.Controls.SubscribeHeartbeat (PanelLayout);
		}

		void Panel_Click (object sender, MouseButtonEventArgs e)
		{
			if (DiagnoseMode == DiagnoseModes.AbsoluteInterval)
				DiagnoseMode = DiagnoseModes.RelativeFrameDelay;
			else
				DiagnoseMode = DiagnoseModes.AbsoluteInterval;
		}

		protected override void CleanupUnmanagedResources ()
		{
			if (Owner != null && Owner.Controls != null) {
				Owner.Controls.UnsubscribeHeartbeat (PanelPaint);
				Owner.Controls.UnsubscribeHeartbeat (PanelLayout);
				Owner = null;
			}
			base.CleanupUnmanagedResources ();
		}
	}
}

