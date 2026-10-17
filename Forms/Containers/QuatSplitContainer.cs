using System;
using System.Drawing;
using KS.Foundation;
using SummerGUI.Splitting;

namespace SummerGUI
{
	/// <summary>
	/// Container mit vier Panels in einer Linie, getrennt durch drei Splitter:
	///   Vertical:   [Panel1 | S1 | Panel2 | S2 | Panel3 | S3 | Panel4]
	///   Horizontal: [Panel1 / S1 / Panel2 / S2 / Panel3 / S3 / Panel4]
	///
	/// Wird ein Panel ausgeblendet (PanelNCollapsed = true), verschwinden die
	/// direkt angrenzenden Splitter mit, und die übrigen Panels fließen
	/// auf - sie überlappen sich dabei nie (siehe SplitPaneLayout).
	///
	/// (Klassename entspricht jetzt dem Dateinamen; war zuvor "QuatSplitter".
	/// Der alte OnLayout-Code layoutete nie Panel4 und mischte horizontalen und
	/// vertikalen Splitter in einer Zeile, was zu null-/negativen Breiten führte.)
	/// </summary>
	public class QuatSplitContainer : Container
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

		private bool m_Panel4Collapsed;
		public bool Panel4Collapsed {
			get {
				return m_Panel4Collapsed;
			}
			set {

				if (m_Panel4Collapsed != value) {
					m_Panel4Collapsed = value;
					Panel4.Visible = !value;
					Invalidate ();
				}
			}
		}

		public ScrollableContainer Panel1 { get; private set; }
		public SplitterBase Splitter1 { get; private set; }
		public ScrollableContainer Panel2 { get; private set; }
		public SplitterBase Splitter2 { get; private set; }
		public ScrollableContainer Panel3 { get; private set; }
		public SplitterBase Splitter3 { get; private set; }
		public ScrollableContainer Panel4 { get; private set; }

		public SplitOrientation Orientation { get; private set; }

		public QuatSplitContainer (string name, SplitOrientation orientation, Docking dock, IWidgetStyle style)
				: base(name, dock, style)
		{
			Orientation = orientation;

			Panel1 = new ScrollableContainer ("panel1", Docking.Fill, new EmptyWidgetStyle ());
			Panel2 = new ScrollableContainer ("panel2", Docking.Fill, new EmptyWidgetStyle ());
			Panel3 = new ScrollableContainer ("panel3", Docking.Fill, new EmptyWidgetStyle ());
			Panel4 = new ScrollableContainer ("panel4", Docking.Fill, new EmptyWidgetStyle ());

			if (orientation == SplitOrientation.Horizontal) {
				Splitter1 = new HorizontalSplitter ("splitter1", new SplitContainerTransparentSplitterStyle ());
				Splitter2 = new HorizontalSplitter ("splitter2", new SplitContainerTransparentSplitterStyle ());
				Splitter3 = new HorizontalSplitter ("splitter3", new SplitContainerTransparentSplitterStyle ());
			} else {
				Splitter1 = new VerticalSplitter ("splitter1", new SplitContainerTransparentSplitterStyle ());
				Splitter2 = new VerticalSplitter ("splitter2", new SplitContainerTransparentSplitterStyle ());
				Splitter3 = new VerticalSplitter ("splitter3", new SplitContainerTransparentSplitterStyle ());
			}

			Children.Add (Panel1);
			Children.Add (Splitter1);
			Children.Add (Panel2);
			Children.Add (Splitter2);
			Children.Add (Panel3);
			Children.Add (Splitter3);
			Children.Add (Panel4);

			// Gleicher Koordinatenraum (volle Container-Breite/-Höhe),
			// Positionen der Panel-Reihenfolge nach sortiert: 1/4, 2/4, 3/4.
			Splitter1.Distance = 0.25f;
			Splitter2.Distance = 0.5f;
			Splitter3.Distance = 0.75f;
		}

		public override void Focus ()
		{
			if (!Panel1Collapsed)
				Panel1.Focus ();
			else if (!Panel2Collapsed)
				Panel2.Focus ();
			else if (!Panel3Collapsed)
				Panel3.Focus ();
			else if (!Panel4Collapsed)
				Panel4.Focus ();
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
				new ScrollableContainer[] { Panel1, Panel2, Panel3, Panel4 },
				new SplitterBase[] { Splitter1, Splitter2, Splitter3 });
		}

		protected override void LayoutChildren (IGUIContext ctx, RectangleF bounds)
		{
			// wird in OnLayout explizit ge-layoutet
		}
	}
}
