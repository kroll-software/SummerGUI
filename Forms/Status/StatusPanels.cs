using System;
using System.Drawing;

namespace SummerGUI
{
	// ========================================================
	// IStatusPanel — Layout-Steuerung für Statusbalken-Panels
	//
	// Panels, die dieses Interface implementieren, können ihre
	// Größe im Statusbalken steuern.  Panels, die das Interface
	// NICHT implementieren, werden immer shrink-to-content
	// angeordnet (Standardverhalten).
	//
	// Position wird in dieser Reihenfolge bestimmt:
	//   [1] DefaultPanel  — immer vorhanden, LINKS, erste Position
	//   [2] Left-Panels   — Docking.Left, ChildCollection-Reihenfolge
	//   [3] Right-Panels  — Docking.Right, ChildCollection-Reihenfolge
	//
	public interface IStatusPanel
	{
		// Groesse des Panels im Statusbalken.
		//   0      = shrink-to-content          (VOREINSTELLUNG)
		//   (0..1] = BRUCHTEIL des Restplatzes  (0.5 = 50 %),
		//            nachdem fixe Panels abgezogen sind — wächst,
		//            kein Obergrenzen-Limit
		//   > 1    = FESTE Pixelbreite
		//
		// Negativen Werte sind NICHT erlaubt.
		float PanelWidth { get; set; }

		// true  = Panel wächst auf vollen Restplatz
		//         (oder teilt mit anderen Fill-Panels nach PanelWidth)
		// false = nur PanelWidth wirkt (0 = content, (0,1] = %, >1 = px)
		bool Fill { get; set; }
	}

	// ========================================================
	// StatusTextPanel — Textanzeige im Statusbalken
	//

	public class StatusTextPanel : TextWidget, IStatusPanel
	{
		float _panelWidth;
		bool  _fill;

		public StatusTextPanel (string name, Docking dock, string text = null)
			: base (name, dock, new StatusPanelStyle(), text, null)
		{
			Format = FontFormat.DefaultSingleLine;
			Margin = Padding.Empty;
			InvalidateOnHeartBeat = true;
		}

		public float PanelWidth
		{
			get  { return _panelWidth; }
			set
			{
				if (value < 0)
					throw new ArgumentOutOfRangeException (nameof (value), "PanelWidth darf nicht negativ sein.");
				if (_panelWidth != value) {
					_panelWidth = value;
					Update (true);
				}
			}
		}

		public bool Fill
		{
			get { return _fill; }
			set
			{
				if (_fill != value) {
					_fill = value;
					Update (true);
				}
			}
		}

		public override void OnResize (IGUIContext ctx)
		{
			base.OnResize (ctx);
			Invalidate ();
		}
	}

	// ========================================================
	// StatusProgressPanel — Fortschrittsanzeige im Statusbalken
	//

	public class StatusProgressPanel : ProgressBar, IStatusPanel
	{
		float _panelWidth;
		bool  _fill;

		public StatusProgressPanel (string name)
			: base (name)
		{
			ProgressPadding   = 3;
			Padding           = Padding.Empty;
			Margin            = Padding.Empty;
			MaxSize           = new SizeF (140, float.MaxValue);
			Dock              = Docking.Right;
		}

		public float PanelWidth
		{
			get  { return _panelWidth; }
			set
			{
				if (value < 0)
					throw new ArgumentOutOfRangeException (nameof (value), "PanelWidth darf nicht negativ sein.");
				if (_panelWidth != value) {
					_panelWidth = value;
					Update (true);
				}
			}
		}

		public bool Fill
		{
			get { return _fill; }
			set
			{
				if (_fill != value) {
					_fill = value;
					Update (true);
				}
			}
		}

		public override SizeF PreferredSize (IGUIContext ctx, SizeF proposedSize)
		{
			if (Font == null)
				return new SizeF (140, 16);
			return new SizeF (140, Font.Height);
		}
	}
}
