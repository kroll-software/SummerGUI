using System;
using System.Collections.Generic;
using System.Drawing;
using SummerGUI.Splitting;

namespace SummerGUI
{
	/// <summary>
	/// Gemeinsames Layout-Verfahren für lineare Split-Container
	/// (TripleSplitContainer, QuatSplitContainer).
	///
	/// Die Panels stehen in einer Linie (Spalte bei Vertical, Zeile bei Horizontal).
	/// Ein Splitter i (zwischen Panel i und Panel i+1) ist ein harter Anker, wenn
	/// beide Nachbar-Panels sichtbar sind; seine Position ergibt sich aus Distance
	/// (Ratio &lt; 1.0 oder absolute Pixel ab 1.0, siehe SplitterBase).
	///
	/// Ist ein Panel ausgeblendet, verschwinden seine direkten Anker mit; die
	/// übrigen Panels teilen sich den freigewordenen Abschnitt zu gleichen Teilen
	/// in ihrer Reihenfolge, so dass sich Panels nie überlappen.
	///
	/// Voraussetzung: die Distance-Werte der Splitter sind der Panel-Reihenfolge
	/// nach sortiert (wie in den Standard-Konstruktor-Werten).
	/// </summary>
	internal static class SplitPaneLayout
	{
		public static void Tile (
			IGUIContext ctx,
			RectangleF bounds,
			bool vertical,
			ScrollableContainer[] panels,
			SplitterBase[] splitters)
		{
			// 1) Anker festlegen: Splitter ist sichtbar, wenn beide Nachbar-Panels sichtbar sind
			for (int i = 0; i < splitters.Length; i++)
				splitters [i].Visible = panels [i].Visible && panels [i + 1].Visible;

			// 2) sichtbare Anker innerhalb der Container-Bounds auslayouten
			for (int i = 0; i < splitters.Length; i++)
				if (splitters [i].Visible)
					splitters [i].OnLayout (ctx, bounds);

			// 3) sichtbare Panels in Reihenfolge auf Zellen mappen:
			//    Zelle eines Panels = Abschnitt zwischen
			//      - dem nächstgelegenen Anker links (bzw. oberhalb) vom Panel (nach Panel-Index)
			//      - bzw. der Container-Kante.
			//    Mehrere Panels, die dieselbe Zelle beanspruchen (weil dazwischengeliegene
			//    Panels ausgeblendet sind), teilen die Zelle zu gleichen Teilen.

			int groupL = -1;
			int groupR = -1;
			var group = new List<int> ();

			void CloseGroup ()
			{
				if (group.Count == 0)
					return;

				float start = groupL >= 0
					? (vertical ? splitters [groupL].Right : splitters [groupL].Bottom)
					: (vertical ? bounds.Left : bounds.Top);
				float end = groupR >= 0
					? (vertical ? splitters [groupR].Left : splitters [groupR].Top)
					: (vertical ? bounds.Right : bounds.Bottom);

				float share = (end - start) / group.Count;
				for (int j = 0; j < group.Count; j++) {
					float x1 = start + j * share;
					float x2 = x1 + share;
					RectangleF pane = vertical
						? new RectangleF (x1, bounds.Top, x2 - x1, bounds.Height)
						: new RectangleF (bounds.Left, x1, bounds.Width, x2 - x1);
					panels [group [j]].OnLayout (ctx, pane);
				}
				group.Clear ();
			}

			for (int p = 0; p < panels.Length; p++) {
				if (!panels [p].Visible)
					continue;

				int l = -1;      // letzter Anker links (bzw. oberhalb) vom Panel
				int r = -1;      // erster Anker rechts (bzw. unterhalb) vom Panel
				for (int i = 0; i < splitters.Length; i++) {
					if (!splitters [i].Visible)
						continue;
					if (i < p)
						l = i;
					else if (r == -1)
						r = i;
				}

				if (l != groupL || r != groupR) {
					CloseGroup ();
					groupL = l;
					groupR = r;
				}
				group.Add (p);
			}
			CloseGroup ();
		}
	}
}
