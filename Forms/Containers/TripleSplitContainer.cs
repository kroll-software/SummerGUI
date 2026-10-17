using System;
using System.Drawing;
using KS.Foundation;
using SummerGUI.Splitting;

namespace SummerGUI
{
	/// <summary>
	/// Container mit drei Panels, getrennt durch zwei Splitter:
	///   Vertical:   [Panel1 | Splitter1 | Panel2 | Splitter2 | Panel3]
	///   Horizontal: [Panel1 / Splitter1 / Panel2 / Splitter2 / Panel3]
	///
	/// Wird ein Panel ausgeblendet (PanelNCollapsed = true), verschwinden die
	/// direkt angrenzenden Splitter mit, und die übrigen Panels fließen
	/// auf - sie überlappen sich dabei nie (siehe SplitPaneLayout).
	/// </summary>
	public class TripleSplitContainer : Container
	{
		private bool m_Panel1Collapsed;
		public bool Panel1Collapsed {
			get {
				return m_Panel1Collapsed;
			}
			set {

				if (m_Panel1Collapsed != value) {
					m_Panel1Collapsed = value;
					Panel1.Visible = !value;
					Invalidate ();
				}
			}
		}

		private bool m_Panel2Collapsed;
		public bool Panel2Collapsed {
			get {
				return m_Panel2Collapsed;
			}
			set {

				if (m_Panel2Collapsed != value) {
					m_Panel2Collapsed = value;
					Panel2.Visible = !value;
					Invalidate ();
				}
			}
		}

		private bool m_Panel3Collapsed;
		public bool Panel3Collapsed {
			get {
				return m_Panel3Collapsed;
			}
			set {

				if (m_Panel3Collapsed != value) {
					m_Panel3Collapsed = value;
					Panel3.Visible = !value;
					Invalidate ();
				}
			}
		}

		public ScrollableContainer Panel1 { get; private set; }
		public SplitterBase Splitter1 { get; private set; }
		public ScrollableContainer Panel2 { get; private set; }
		public SplitterBase Splitter2 { get; private set; }
		public ScrollableContainer Panel3 { get; private set; }

		public SplitOrientation Orientation { get; private set; }

		public TripleSplitContainer (string name, SplitOrientation orientation, Docking dock, IWidgetStyle style)
				: base(name, dock, style)
		{
			Orientation = orientation;

			Panel1 = new ScrollableContainer ("panel1", Docking.Fill, new EmptyWidgetStyle ());
			Panel2 = new ScrollableContainer ("panel2", Docking.Fill, new EmptyWidgetStyle ());
			Panel3 = new ScrollableContainer ("panel3", Docking.Fill, new EmptyWidgetStyle ());

			if (orientation == SplitOrientation.Horizontal) {
				Splitter1 = new HorizontalSplitter ("splitter1", new SplitContainerTransparentSplitterStyle ());
				Splitter2 = new HorizontalSplitter ("splitter2", new SplitContainerTransparentSplitterStyle ());
			} else {
				Splitter1 = new VerticalSplitter ("splitter1", new SplitContainerTransparentSplitterStyle ());
				Splitter2 = new VerticalSplitter ("splitter2", new SplitContainerTransparentSplitterStyle ());
			}

			Children.Add (Panel1);
			Children.Add (Splitter1);
			Children.Add (Panel2);
			Children.Add (Splitter2);
			Children.Add (Panel3);

			// Beide Splitter sind Anker im selben Koordinatenraum (volle Container-Breiten/Höhe),
			// daher müssen ihre Positionen der Panel-Reihenfolge nach sortiert sein:
			// 1/3 beim ersten Splitter, 2/3 beim zweiten.
			// FIX: beide Splitter standen bisher auf 0.334f, wodurch Panel2 auf (nahezu)
			// null Breite kollabierte und Panel3 die rechte Hälfte des Containers einnahm.
			Splitter1.Distance = 0.334f;
			Splitter2.Distance = 0.667f;
		}

		public override void Focus ()
		{
			if (!Panel1Collapsed)
				Panel1.Focus ();
			else if (!Panel2Collapsed)
				Panel2.Focus ();
			else if (!Panel3Collapsed)
				Panel3.Focus ();
		}

		public override void OnLayout (IGUIContext ctx, RectangleF bounds)
		{
			base.OnLayout (ctx, bounds);

			// Gemeinsames Tiling: Anker-Splitter (beide Nachbarn sichtbar) bilden harte
			// Grenzen, ausgeblendete Panels hinterlassen Lücken, die die Nachbarn
			// gleichmäßig teilen - in beiden Orientierungen, ohne Überlappung.
			SplitPaneLayout.Tile (
				ctx, bounds,
				Orientation == SplitOrientation.Vertical,
				new ScrollableContainer[] { Panel1, Panel2, Panel3 },
				new SplitterBase[] { Splitter1, Splitter2 });
		}

		protected override void LayoutChildren (IGUIContext ctx, RectangleF bounds)
		{
			// wird in OnLayout explizit ge-layoutet
		}
	}
}
