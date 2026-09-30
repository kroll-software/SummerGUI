using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using OpenTK.Graphics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using KS.Foundation;
using SummerGUI;

namespace SummerGUI
{
	/// <summary>
	/// Simple ListView (WinForms-parität, Cross-Plattform, ThreadSafe).
	/// List-Mode zuerst. Zeigt Item-Text, SubItems (spaltenweise), Icons (ImageList oder FontAwesome),
	/// Zeilen-Linien, ColumnHeader-Leiste. Scrollt in einem inneren <see cref="ScrollableContainer"/>.
	/// </summary>
	/// <remarks>
	/// ThreadSafe: <see cref="ListViewItemCollection"/> und <see cref="ListViewColumnCollection"/>
	/// sind <see cref="BinarySortedList{T}"/>-basiert (lock-geschützt).
	/// Icons können über <see cref="ListView.Images"/> (ImageList) oder
	/// <see cref="ListView.IconFont"/> (FontAwesome-Glyph) geliefert werden.
	/// </remarks>
	public class ListView : Container
	{
		#region Fields

		#endregion

		#region Ctors

		public ListView ()
			: this("ListView", Docking.Fill, new ListViewWidgetStyle())
		{
		}

		public ListView (string name)
			: this(name, Docking.Fill, new ListViewWidgetStyle())
		{
		}

		protected ListView (string name, Docking dock, IWidgetStyle style)
			: base (name, dock, style)
		{
			Columns = new ListViewColumnCollection();
			Items   = new ListViewItemCollection();

			Header = new ColumnHeader ("Header", this);
			Body   = new ListViewBody  ("Body",   this);

			AddChild (Header);
			AddChild (Body);

			Header.Dock = Docking.Top;
			Body.Dock   = Docking.Fill;

			m_IconFont   = FontManager.Manager [CommonFontTags.MediumIcons];
			m_IconSize   = 16f;

			CellPadding  = new Padding (4, 4, 4, 4);
			IconPadding  = new Padding (4, 4, 4, 4);
		}

		#endregion

		#region Data

		/// <summary>Spalten-Definitionen (Titel, Breite, Order).</summary>
		public ListViewColumnCollection Columns { get; private set; }

		/// <summary>
		/// Gemeinsames Spalten-Layout (X + Width jeder Spalte). WIRD VON HEADER UND BODY BENUTZT,
		/// damit die Spaltenbreiten exakt übereinstimmen. Jede Spalte hält ihre <see cref="ListViewColumn.Width"/>
		/// (WinForms/DataGridView-Semantik: keine Auto-Stretch der letzten Spalte).
		/// </summary>
		public ColumnLayout GetLayout()
		{
			return ColumnLayout.Build (Columns);
		}

		/// <summary>Zeilen-Daten (Text, SubItems, ImageKey, Icon-Glyph).</summary>
		public ListViewItemCollection Items { get; private set; }

		public ColumnHeader  Header { get; private set; }
		public ListViewBody  Body   { get; private set; }

		#endregion

		#region Icons

		ImageList m_Images;
		/// <summary>ImageList-Quelle. Optional. Keys über <see cref="ListViewItem.ImageKey"/>.</summary>
		public ImageList Images
		{
			get { return m_Images; }
			set
			{
				m_Images = value;
				Invalidate();
			}
		}

		IGUIFont m_IconFont = null;
		/// <summary>FontAwesome-Icon-Font. Optional (default: MediumIcons).</summary>
		public IGUIFont IconFont
		{
			get { return m_IconFont; }
			set
			{
				m_IconFont = value;
				Invalidate();
			}
		}

		float m_IconSize = 16f;
		/// <summary>Icon-Größe in px. Standard: 16.</summary>
		[DpiScalable]
		public float IconSize
		{
			get { return m_IconSize; }
			set
			{
				if (m_IconSize != value)
				{
					m_IconSize = value;
					Invalidate();
				}
			}
		}

		#endregion

		#region Cell Padding

		Padding m_CellPadding = Padding.Empty;
		/// <summary>
		/// Innenabstand jeder Zelle (Header + Body) — entspricht DataGridView.CellPadding.
		/// Standard: (4,4,4,4). Berücksichtigt beim Text-Layout von Header-Tabellenkopf und Body-Zellen.
		/// </summary>
		[DpiScalable]
		public Padding CellPadding
		{
			get { return m_CellPadding; }
			set
			{
				if (m_CellPadding != value)
				{
					m_CellPadding = value;
					Invalidate();
				}
			}
		}

		#endregion

		#region Icon Padding

		Padding m_IconPadding = Padding.Empty;
		/// <summary>
		/// Abstand um jedes Icon (Body, Spalte 0) — Standard (4,4,4,4).
		/// Top/Bottom: Icons in aufeinanderfolgenden Zeilen berühren sich nicht.
		/// <see cref="IconPadding.Right"/> bestimmt den Abstand vom Icon zum Folgetext.
		/// </summary>
		[DpiScalable]
		public Padding IconPadding
		{
			get { return m_IconPadding; }
			set
			{
				if (m_IconPadding != value)
				{
					m_IconPadding = value;
					Invalidate();
				}
			}
		}

		#endregion

		#region LargeIcon

		float m_LargeIconSize = 48f;
		/// <summary>
		/// Icon-Größe in <see cref="ListViewView.LargeIcon"/>-Mode. Standard: 48.
		/// (In List-Mode gilt <see cref="IconSize"/>.)
		/// </summary>
		[DpiScalable]
		public float LargeIconSize
		{
			get { return m_LargeIconSize; }
			set
			{
				float v = Math.Max (16f, value);
				if (Math.Abs (m_LargeIconSize - v) > 0.001f)
				{
					m_LargeIconSize = v;
					Invalidate();
				}
			}
		}

		#endregion

				#region Horizontally (shared)

					float m_ScrollX = 0f;
					/// <summary>
					/// Gemeinsame horizontale Scroll-Position (px) — GEMEINSAM für Header UND Body,
					/// damit beide bei h-Scrollung exakt in Phase bleiben (WinForms: HorizontalScroll).
					/// Pflege: <see cref="ListViewBody"/> über seine HScrollBar.
					/// </summary>
					public float ScrollX
					{
					get { return m_ScrollX; }
					set
					{
					float v = Math.Max (0f, value);
					if (Math.Abs (m_ScrollX - v) < 0.0001f)
					return;
					m_ScrollX = v;
					Body?.Invalidate();
					Header?.Invalidate();
					}
					}

					#endregion

		#region Selection

		int m_SelectedIndex = -1;
		/// <summary>Ausgewählte Zeile oder -1 (keine).</summary>
		public int SelectedIndex
		{
			get { return m_SelectedIndex; }
			set
			{
				if (value >= 0 && value >= Items.Count)
					value = Items.Count > 0 ? Items.Count - 1 : -1;

				if (m_SelectedIndex != value) {
					m_SelectedIndex = value;
					OnSelectionChanged();
					Invalidate();
				}
			}
		}

		public ListViewItem SelectedItem
		{
			get
			{
				if (m_SelectedIndex < 0 || m_SelectedIndex >= Items.Count)
					return null;
				return Items [m_SelectedIndex];
			}
		}

		public event EventHandler SelectionChanged;
		protected virtual void OnSelectionChanged()
		{
			if (SelectionChanged != null)
				SelectionChanged (this, EventArgs.Empty);
		}

		/// <summary>
		/// Firet, wenn der Benutzer die aktuelle Zeile per <b>Doppelklick</b> oder <b>Enter-Taste</b> aktiviert
		/// (WinForms-Parität: ItemsSelected / double-click commit). SelectedIndex bleibt unverändert —
		/// das Event signalisiert nur "bestätige diese Zeile".
		/// </summary>
		public event EventHandler ItemActivated;
		/// <summary>Raust das Event. Wird auch von <see cref="ListViewBody"/> (Doppelklick / Enter) aufgerufen.</summary>
		public virtual void OnItemActivated()
		{
			if (ItemActivated != null)
				ItemActivated (this, EventArgs.Empty);
		}

		#endregion

		#region View

		ListViewView m_View = ListViewView.Details;
		/// <summary>
		/// View-Modus: <see cref="ListViewView.Details"/> (default: Spaltentabelle mit Header)
		/// oder <see cref="ListViewView.LargeIcon"/> (große Icons mit 2-zeiligem, zentriertem
		/// Text darunter — Text aus der ersten Spalte, analog WinForms.LargeIcon).
		/// Bei <see cref="ListViewView.LargeIcon"/>: Header unsichtbar, nur vertikales Scrollen,
		/// Spalten 1..n werden nicht gezeichnet.
		/// </summary>
		public ListViewView View
		{
			get { return m_View; }
			set
			{
				if (m_View != value) {
					m_View = value;
					if (Body != null) {
						Body.ClearLiCache ();
						Body.ScrollBars = (value == ListViewView.LargeIcon) ? ScrollBars.Vertical : ScrollBars.Both;
						Body.VScrollBar?.Value = 0f;
					}
					if (Header != null)
						Header.Visible = (value == ListViewView.Details);
					ResetCachedLayout();
					Invalidate();
				}
			}
		}

		#region LargeIcon — Geometrie & Text

		internal const int    LI_MAX_LINES   = 2; // Max. Text-Zeilen pro LargeIcon-Item
		internal const float  LI_TEXT_GAP    = 4f; // Abstand Icon → Text
		internal const float  LI_CELL_MARGIN = 8f; // Abstand Zelle → Zelle

		/// <summary>LargeIcon-Grid-Geometrie (Dokument-Koordinaten, ohne Clip).</summary>
		public struct LargeIconGeom
		{
			public int   ColsPerRow;
			public int   Rows;
			public float CellW;
			public float CellH;
			public float TextW;
			public float TextLineH;

			/// <summary>Ursprung (x,y) der Zelle <paramref name="index"/> im Dokument.</summary>
			public (float x, float y) Origin (int index)
			{
				int r = index / ColsPerRow;
				int c = index % ColsPerRow;
				return (c * (CellW + LI_CELL_MARGIN), r * (CellH + LI_CELL_MARGIN));
			}

			/// <summary>Index der Zelle unter dem Dokument-Punkt, oder -1.</summary>
			public int IndexAt (float x, float y)
			{
				if (x < 0f || y < 0f)
					return -1;
				int c = (int)((x + LI_CELL_MARGIN * 0.5f) / (CellW + LI_CELL_MARGIN));
				int r = (int)((y + LI_CELL_MARGIN * 0.5f) / (CellH + LI_CELL_MARGIN));
				return r * ColsPerRow + c;
			}
		}

		/// <summary>
		/// Rechnet das LargeIcon-Grid für die gegebene Viewport-Breite:
		/// gleich breite Zellen, so viele Spalten wie reinpassen, 2 Text-Zeilen unter dem Icon.
		/// </summary>
		public LargeIconGeom LargeIconGeomFor (float viewportW)
		{
			float lineH = (FontManager.Manager != null && FontManager.Manager.DefaultFont != null)
				? FontManager.Manager.DefaultFont.LineHeight : 20f;

			float cellH   = m_LargeIconSize + LI_TEXT_GAP + lineH * LI_MAX_LINES;
			float minCellW = Math.Max (m_LargeIconSize, lineH * 9f);
			float ideal    = Math.Max (minCellW, m_LargeIconSize * 1.6f);

			int cols = Math.Max (1, (int)((viewportW + LI_CELL_MARGIN) / (ideal + LI_CELL_MARGIN)));

			LargeIconGeom g = new LargeIconGeom();
			g.ColsPerRow = cols;
			g.TextLineH  = lineH;
			g.CellH      = cellH;
			g.CellW      = (viewportW - (cols - 1) * LI_CELL_MARGIN) / cols;
			g.TextW      = Math.Min (g.CellW, m_LargeIconSize * 2.4f);
			int items    = Items.Count;
			g.Rows       = (items + cols - 1) / Math.Max (1, cols);
			return g;
		}

		/// <summary>
		/// Zerlegt <paramref name="text"/> in max. 2 Zeilen, je ≤ <paramref name="maxW"/> px breit.
		/// Split an einer Wortgrenze nahe der Mitte; pro Zeile Ellipsis nur wenn sie wirklich
		/// zu breit ist (WinForms-LargeIcon-Parätit: Text 2-zeilig, zentriert, Ellipsis).
		/// </summary>
		public static (string Line1, string Line2, bool Truncated) SplitTwoLines (string text, IGUIFont font, float maxW)
		{
			if (string.IsNullOrEmpty (text))
				return ("", "", false);

			string d = "…";

			if (font.Measure (text).Width <= maxW)
				return (text, "", false);

			// Wortgrenze im Mittelbereich suchen (45–65 %), sonst fester Cut.
			int n   = text.Length;
			int lo  = Math.Max (1, (int)(n * 0.45f));
			int hi  = Math.Min (n - 1, (int)(n * 0.65f));
			int cut = -1;
			for (int i = lo; i <= hi; i++) {
				if (text [i] == ' ') { cut = i; break; }
			}
			if (cut < 0)
				cut = lo;

			string l1 = text.Substring (0, cut).TrimEnd ();
			string l2 = text.Substring (cut).TrimStart ();

			if (font.Measure (l1).Width > maxW)
				l1 = Ellipsize (l1, font, maxW, d);
			if (font.Measure (l2).Width > maxW)
				l2 = Ellipsize (l2, font, maxW, d);

			if (l1.Length == 0 && l2.Length == 0)
				l1 = d;

			bool truncated = l1.EndsWith (d) || l2.EndsWith (d, StringComparison.Ordinal);
			return (l1, l2, truncated);
		}

		static string Ellipsize (string s, IGUIFont font, float maxW, string dots)
		{
			if (font.Measure (s + dots).Width <= maxW)
				return s;
			int lo = 1, hi = s.Length - 1, best = 1;
			while (lo <= hi) {
				int mid = (lo + hi) / 2;
				if (font.Measure (s.Substring (0, mid) + dots).Width <= maxW) {
					best = mid;
					lo = mid + 1;
				} else {
					hi = mid - 1;
				}
			}
			return s.Substring (0, best) + dots;
		}

		#endregion

		#region Navigation (Cursortasten) & Sichtbarkeit

		/// <summary>Großenzellen-Spalten pro Zeile (LargeIcon-Modus).</summary>
		public int LargeIconColsPerRow(float viewportW)
		{
			var g = LargeIconGeomFor (viewportW);
			return Math.Max (1, g.ColsPerRow);
		}

		/// <summary>
		/// Wählt das benachbarte Item (Pfeiltasten; WinForms-Parätät):
		/// LargeIcon: links/rechts = Nachbarzelle, oben/unten = Zeile. Details: oben/unten = Zeile.
		/// </summary>
		/// <param name="dir">0=Links, 1=Rechts, 2=Oben, 3=Unten</param>
		public void MoveSelection (int dir)
		{
			if (Items.Count == 0) return;
			int maxIdx = Items.Count - 1;
			int cur = (m_SelectedIndex < 0) ? 0 : m_SelectedIndex;
			int cols = (View == ListViewView.LargeIcon)
				? LargeIconColsPerRow (Math.Max(1f, (Body != null ? Body.Bounds.Width : 200f)))
				: 1;
			int next;
			switch (dir) {
				case 0:    next = cur - 1; break;
				case 1:    next = cur + 1; break;
				case 2:    next = cur - cols; break;
				default:   next = cur + cols; break;
			}
			if (next < 0 || next > maxIdx) return;
			if (next == cur) return;
			SelectedIndex = next;
			Invalidate ();
		}

		/// <summary>Scrollt das angegebene Item in den sichtbaren Bereich (view-modusabhängig).</summary>
		public void ScrollItemIntoView (int index)
		{
			if (Body == null || Body.VScrollBar == null || !Body.VScrollBar.IsVisibleEnabled) return;
			if (index < 0 || index >= Items.Count) return;
			float vp  = Math.Max (1f, Body.Bounds.Height);
			float top, bottom;
			if (View == ListViewView.LargeIcon) {
				float vw = Math.Max (1f, Body.Bounds.Width);
				var g = LargeIconGeomFor (vw);
				int row = index / Math.Max (1, g.ColsPerRow);
				// Zeilenposition inkl. Zeilen-Abstand (LI_CELL_MARGIN) — wie in DocumentSize.
				float pitch = g.CellH + ListView.LI_CELL_MARGIN;
				top    = row * pitch;
				bottom = top + g.CellH;
			} else {
				top    = index * Body.RowHeight;
				bottom = top + Body.RowHeight;
			}
			if (top < Body.VScrollBar.Value)
				Body.VScrollBar.Value = top;
			else if (bottom > Body.VScrollBar.Value + vp)
				Body.VScrollBar.Value = bottom - vp;
		}

		#endregion

		#region Public API

		public int Count => Items.Count;

		public ListViewItem this[int index] => Items [index];

		public void Add (string text)
		{
			Items.Add (new ListViewItem(text));
			Invalidate();
		}

		public void Add (ListViewItem item)
		{
			if (item == null)
				return;
			Items.AddLast (item);
			Invalidate();
		}

		public void AddRange(IEnumerable<ListViewItem> list)
		{
			if (list == null)
				return;
			foreach (var it in list)
				Items.AddLast(it);
			Invalidate();
		}

		/// <summary>Entfernt alle Items (alias für <see cref="Items"/>, Clear).</summary>
		public void ClearItems ()
		{
			m_SelectedIndex = -1;
			Items.Clear();
			Invalidate();
		}

		public string GetSubItem (int row, int column)
		{
			if (row < 0 || row >= Items.Count)
				return "";
			return Items [row].GetSubItem (column);
		}

		/// <summary>Gibt die Text-Zeile + SubItems als <c>"a | b | c"</c>-String zurück.</summary>
		public string GetRowText (int row)
		{
			if (row < 0 || row >= Items.Count)
				return "";
			var it = Items [row];
			var parts = new List<string>();
			parts.Add (it.Text ?? "");
			int sub = it.SubItems.Count;
			for (int i = 0; i < sub; i++)
				parts.Add (it.SubItems [i] ?? "");
			return string.Join (" | ", parts);
		}

		#endregion

		#region Paint

		public override void OnPaintBackground (IGUIContext ctx, RectangleF bounds)
		{
			base.OnPaintBackground (ctx, bounds);

			// Rahmen
			ctx.DrawRectangle (Style.BorderColorPen, bounds);
		}

		#endregion
		#endregion

	} // class ListView


	/// <summary>View-Modes.</summary>
	public enum ListViewView
	{
		/// <summary>Spaltentabelle mit Header (WinForms-Parätät: View.Details).</summary>
		Details,
		/// <summary>Wie Details (WinForms-Parätät: View.List); Details ist der Standard.</summary>
		List,
		/// <summary>Große Icons mit 2-zeiligem Text darunter (WinForms-Parätät: View.LargeIcon).</summary>
		LargeIcon
	}


	/// <summary>Spalte: Titel, Breite, Z-Order.</summary>
	public class ListViewColumn
	{
		string m_Text = "";
		public string Text
		{
			get { return m_Text; }
			set { m_Text = value ?? ""; }
		}

		[DpiScalable]
		public float Width { get; set; }

		public int Order { get; set; }   // -1 = Unsorted

		public ListViewColumn () { Width = 110f; Order = -1; }
		public ListViewColumn (string text) : this() { Text = text; }
		public ListViewColumn (string text, float width) : this() { Text = text; Width = width; }

		public override string ToString()
		{
			return "[ListViewColumn: " + Text + ", " + Width + "]";
		}
	}


	/// <summary>ThreadSafe-Spaltenliste.</summary>
	public class ListViewColumnCollection : BinarySortedList<ListViewColumn>
	{
		public new void Add (ListViewColumn column)
		{
			this.AddLast (column);
		}

		public void Add (string text)
		{
			this.AddLast (new ListViewColumn(text));
		}

		public ListViewColumn GetByOrder(int order)
		{
			for (int i = 0; i < this.Count; i++) {
				if (this[i].Order == order)
					return this[i];
			}
			return null;
		}
	}



	/// <summary>
	/// Gemeinsames Spalten-Layout: berechnet X-Start + Width für jede Spalte aus
	/// <see cref="ListViewColumnCollection"/>. WIRD VON <see cref="ColumnHeader"/> UND
	/// <see cref="ListViewBody"/> GENUTZT, damit die Spaltenbreiten exakt übereinstimmen
	/// und die Maus-Aufteilung (Resize-Grenzen) zum gezeichneten Layout passt.
	/// Jede Spalte hält ihre <see cref="ListViewColumn.Width"/> (WinForms/DataGridView-Semantik:
	/// keine Auto-Stretch der letzten Spalte → Header und Body sind pixelgenau identisch).
	/// </summary>
	public sealed class ColumnLayout
	{
		/// <summary>Minimale Spaltenbreite (Resize-Untergrenze).</summary>
		public const float MinWidth = 30f;
		/// <summary>Mauserfassungs-Band um eine Spaltengrenze (px). Nur innerhalb dieses Bandes wird ge-Grabbed.</summary>
		public const float GrabTolerance = 6f;

		public int Count { get; private set; }

		/// <summary>X-Start der Spalte relativ zum Widget-Left (0-basiert).</summary>
		public float X  (int i) { return i >= 0 && i < Count ? m_X [i]  : 0f; }
		/// <summary>Width der Spalte.</summary>
		public float W  (int i) { return i >= 0 && i < Count ? m_W [i]  : 0f; }
		/// <summary>Rechte Kante (X+Width) der Spalte relativ zum Widget-Left.</summary>
		public float Right (int i) { return i >= 0 && i < Count ? m_X [i] + m_W [i] : 0f; }

		/// <summary>Summe aller Spaltenbreiten.</summary>
		public float TotalWidth { get; private set; }

		/// <summary>
		/// Gesucht: Index der Spalte, deren Rechte Kante (Grenze) am nächsten zu
		/// <paramref name="x"/> liegt (relativ zum Widget-Left), oder -1.
		/// Nur innerhalb von <see cref="GrabTolerance"/> px wird eine Grenze erfaßt
		/// (sonst gibt ein Klick mitten in einer Spalte die FALSCHE Spalte – wie WinForms,
		/// wird nur am Rand ge-Grabbed).
		/// </summary>
		public int HitResizer (float x)
		{
			int bestIdx = -1;
			float bestDist = float.MaxValue;
			for (int i = 0; i < Count; i++) {
				float d = Math.Abs (x - Right (i));
				if (d < bestDist) {
					bestDist = d;
					bestIdx = i;
				}
			}
			// Nur innerhalb der Erfass-Toleranz → sonst -1 (kein Resizer unter der Maus).
			if (bestDist > GrabTolerance)
				return -1;
			return bestIdx;
		}
		/// <summary>Zurücksetzt auf den nächsten größeren Resizer (Maus rechts vom Punkt).</summary>
		public int NextResizer (float x)
		{
			for (int i = 0; i < Count; i++) {
				if (Right (i) >= x)
					return i;
			}
			return -1;
		}

		float [] m_X = new float [0];
		float [] m_W = new float [0];

		/// <summary>Rechnet das X-Layout aus den Spaltenbreiten nach.</summary>
		public static ColumnLayout Build (ListViewColumnCollection columns)
		{
			var l = new ColumnLayout();
			int n = columns.Count;
			l.Count = n;
			l.m_X = new float [n];
			l.m_W = new float [n];

			float x = 0f;
			float total = 0f;
			for (int i = 0; i < n; i++) {
				float w = columns [i].Width;
				if (w < MinWidth)
					w = MinWidth;
				l.m_X [i] = x;
				l.m_W [i] = w;
				x += w;
				total += w;
			}
			l.TotalWidth = total;
			return l;
		}
	}


	/// <summary>ThreadSafe-Itemliste.</summary>
	public class ListViewItemCollection : BinarySortedList<ListViewItem>
	{
	}


	/// <summary>
	/// Eine Zeile in einer ListView.
	/// </summary>
	public class ListViewItem
	{
		string m_Text = "";
		public string Text
		{
			get { return m_Text; }
			set { m_Text = value ?? ""; }
		}

		public BinarySortedList<string> m_SubItems = new BinarySortedList<string>();
		/// <summary>Sub-Items (einzeln pro Spalte; leer wenn keine).</summary>
		public BinarySortedList<string> SubItems
		{
			get { return m_SubItems; }
		}

		public void AddSubItem (string value) { m_SubItems.AddLast (value ?? ""); }

		/// <summary>Gibt den Wert in <paramref name="column"/>-Spalte (Spalte 0 = Text-Spalte).</summary>
		public string GetSubItem (int column)
		{
			if (column == 0)
				return Text;
			int i = column - 1;
			if (i < 0 || i >= m_SubItems.Count)
				return "";
			return m_SubItems [i];
		}

		/// <summary>Icon: Key in <see cref="ListView.Images"/>. Optional.</summary>
		public string ImageKey { get; set; }

		/// <summary>Icon: FontAwesome-Glyph. Optional.</summary>
		public char IconGlyph { get; set; }

		public object Tag { get; set; }

		public ListViewItem () {}
		public ListViewItem (string text) { Text = text; }

		/// <summary>Convenience: text, a FontAwesome glyph, and sub-item cells (one per column beyond the first).</summary>
		public ListViewItem (string text, char iconGlyph, params string[] subItems)
			: this(text)
		{
			IconGlyph = iconGlyph;
			if (subItems != null) {
				foreach (var s in subItems)
					AddSubItem (s);
			}
		}

		public override string ToString()
		{
			return "[ListViewItem: " + Text + ", " + (m_SubItems?.Count ?? 0) + " subs]";
		}
	}


	/// <summary>Spaltenkopf-Leiste (zeichnet die Header + Sortierpfeile).</summary>
	public class ColumnHeader : Widget
	{
		public ListView View { get; }

		public ColumnHeader (string name, ListView view)
			: base (name, Docking.Top, new ColumnHeaderWidgetStyle())
		{
			View = view;
			Font = FontManager.Manager.DefaultFont;
		}

		/// <summary>Feste Höhe der Spaltenleiste. Docking.Top nimmt die PreferredSize-Height.</summary>
		public override SizeF PreferredSize (IGUIContext ctx, SizeF proposedSize)
		{
			return new SizeF (Math.Max (proposedSize.Width, MinSize.Width), 26f);
		}

		IGUIFont m_Font = null;
		public IGUIFont Font
		{
			get { return m_Font; }
			set { m_Font = value; }
		}

		public float RowHeight
		{
			get { return Font != null ? Font.LineHeight : 20f; }
		}

		#region Resize state

		/// <summary>Index der Spalte, die gerade per Maus resized wird (-1 = kein Drag).</summary>
		int m_ResizeIndex = -1;
		/// <summary>X-Start des Mausevents beim Beginn des Drags (Screen/Window).</summary>
		float m_ResizeStartMouseX = 0f;
		/// <summary>Spaltenbreite zum Zeitpunkt des Drag-Starts. Verhindert Drift bei vielen Events.</summary>
		float m_ResizeStartWidth = 0f;
		/// <summary>Spalte, deren Resizer (rechter Rand) aktuell gehovered wird (-1 = keiner).</summary>
		int m_HoverResizer = -1;

		/// <summary>Index des Resizers am nächsten zu <paramref name="mouseX"/> (relativ zum Bounds-Left), oder -1.</summary>
		int HitResizer (float mouseX)
		{
			ColumnLayout layout = View.GetLayout();
			if (layout.Count == 0)
				return -1;

			// mouseX ist Fensterkoordinaten; Spaltenpositionen liegen aber im Dokument-Coordinate-System,
			// das um die aktuelle H-Scrollung verschoben ist (Viewport = Doc − ScrollX).
			float rel = (mouseX - Bounds.Left) + (View != null ? View.ScrollX : 0f);
			if (rel < 0 || rel > layout.TotalWidth + 1f)
				return -1;

			int idx = layout.HitResizer (rel);

			// Resizer muss auch im Viewport sichtbar sein (Sonst würde man außerhalb des
			// sichtbaren Bereichs eine Grenze resizen – unsinnig).
			// Die Grenze liegt bei Bounds.Left + Right(idx) − ScrollX in Fensterkoordinaten.
			float visibleX = Bounds.Left + layout.Right (idx) - (View != null ? View.ScrollX : 0f);
			if (visibleX < Bounds.Left || visibleX > Bounds.Right)
				return -1;

			return idx;
		}

		#endregion

		#region Paint

		public override void OnPaintBackground (IGUIContext ctx, RectangleF bounds)
		{
			// Header-Hintergrund
			ctx.FillRectangle (Style.BackColorBrush, bounds);
		}

		public override void OnPaint (IGUIContext ctx, RectangleF bounds)
		{
			base.OnPaint (ctx, bounds);

			var view = this.View;
			if (view == null || view.Columns == null || Font == null)
				return;

			// Gemeinsames Spalten-Layout – IDENTISCH zum Body, damit Breiten exakt übereinstimmen.
			ColumnLayout layout = view.GetLayout();

			// H-Scrollung: dieselbe gemeinsame Position wie der Body (Header + Body in Phase).
			float scrollX = view.ScrollX;

			// Zellen-Innenabstand (DataGridView.CellPadding-Parität).
			Padding cp = view.CellPadding;

			float y = bounds.Top;
			float h = bounds.Height;
			int colCount = layout.Count;

			// 1) Hover-Highlight über dem Resizer, der gerade gehovered wird (deutlich sichtbarer Griff).
			if (m_HoverResizer >= 0 && m_ResizeIndex < 0) {
				float rx = bounds.Left + layout.Right (m_HoverResizer) - scrollX;
				// 5px breites Griff-Fenster zentriert auf die Grenze (Maus-Region = HitResizer ±)
				ctx.FillRectangle (Theme.Brushes.HighLightBlue, new RectangleF (rx - 3f, y, 6f, h));
			}

			// 2) Spalten-Titel, getrennt durch senkrechte Linien (Spaltengrenzen = SharedLayout.Right)
			for (int c = 0; c < colCount; c++) {

				RectangleF rect = new RectangleF (bounds.Left + layout.X (c) - scrollX, y, layout.W (c), h);

				// Trennlinie rechts der Spalte (kein Resizer auf die letzte Kante malen – nur Trennstrich)
				if (c < colCount - 1) {
					float lineX = bounds.Left + layout.Right (c) - scrollX;
					ctx.DrawLine (Style.BorderColorPen, lineX, y, lineX, y + h);
				}

				// Sortier-Pfeil
				ListViewColumn col = view.Columns [c];
				string arrow = "";
				if (col.Order >= 0) {
					arrow = (col.Order == 1) ? "▲" : (col.Order == 2) ? "▼" : "";
				}

				string title = col.Text + (arrow.Length > 0 ? " " + arrow : "");
				// Text-Innenabstand: Titel nach cp eingerückt (bleibt innerhalb der Zelle).
				RectangleF textRect = new RectangleF (
					rect.Left + cp.Left,
					rect.Top + cp.Top,
					Math.Max (1f, rect.Width - cp.Left - cp.Right),
					Math.Max (1f, rect.Height - cp.Top - cp.Bottom));
				ctx.DrawString (title, Font, Style.ForeColorBrush, textRect, FontFormat.DefaultSingleLine);
			}
		}

		#endregion

		#region Mouse

		public override void OnMouseEnter (IGUIContext ctx)
		{
			// Cursor-Refresh (VSplit bei Hover des Resizers)
			UpdateCursor (ctx);
			base.OnMouseEnter (ctx);
		}

		public override void OnMouseLeave (IGUIContext ctx)
		{
			// Bei Verlassen Header: Cursor zurück, Hover-Resizer weg
			m_HoverResizer = -1;
			Cursor = Cursors.Default;
			Invalidate();
			base.OnMouseLeave (ctx);
		}

		/// <summary>Setzt den Cursor passend zum Hover-Zustand.</summary>
		void UpdateCursor (IGUIContext ctx)
		{
			if (m_ResizeIndex >= 0) {
				if (Cursor != Cursors.VSplit) {
					Cursor = Cursors.VSplit;
					ctx.SetCursor (Cursors.VSplit);
				}
			} else {
				if (m_HoverResizer >= 0) {
					if (Cursor != Cursors.VSplit) {
						Cursor = Cursors.VSplit;
						ctx.SetCursor (Cursors.VSplit);
					}
				}
			}
		}

		public override void OnMouseMove (MouseMoveEventArgs e)
		{
			base.OnMouseMove (e);

			var view = this.View;
			if (view == null)
				return;

			// 1) Drag-Stepping: Breite der Spalte m_ResizeIndex ändern, clamped auf MinWidth
			if (m_ResizeIndex >= 0) {
				ColumnLayout layout = view.GetLayout();
				// Neue Breite = Start-Breite + (aktuelle MausX – Start-MausX)
				float newW = m_ResizeStartWidth + (e.X - m_ResizeStartMouseX);
				newW = Math.Max (ColumnLayout.MinWidth, newW);

				// Auch die nächste Spalte darf unter ihre MinWidth fallen? Nein – nur die aktuelle clampen,
				// wie WinForms. (Optional: MaxWidth = 2*Bounds.Width; hier bewusst großzügig.)
				view.Columns [m_ResizeIndex].Width = newW;

				// Header + Body + ggf. Layout: neu rendern
				Invalidate();
				if (view != null)
					view.Invalidate();
				return;
			}

			// 2) Hover-Erkennung: Resizer unter der Maus gefunden?
			int idx = HitResizer (e.X);
			if (idx != m_HoverResizer) {
				m_HoverResizer = idx;
				this.Invalidate();
				Cursor = (idx >= 0) ? Cursors.VSplit : Cursors.Default;
				RefreshCursor();
			}
		}

		public override void OnMouseDown (MouseButtonEventArgs e)
		{
			base.OnMouseDown (e);

			var view = this.View;
			if (view == null)
				return;

			// 1) Resizer getroffen? → Drag-Start für diese Spalte, KEIN Sort-Toggle.
			int idx = HitResizer (e.X);
			if (idx >= 0) {
				m_ResizeIndex      = idx;
				m_ResizeStartMouseX = e.X;
				m_ResizeStartWidth  = view.Columns [idx].Width;
				if (m_HoverResizer == -1) {
					m_HoverResizer = idx;
					Invalidate();
				}
				Cursor = Cursors.VSplit;
				return;
			}

			// 2) Kein Resizer → Zeilensortierung (Bestehendes; nur Spalte 0, wo kein Resizer links ist, sortiert)
			ColumnLayout layout = view.GetLayout();
			if (layout.Count == 0)
				return;

			float rel = e.X - Bounds.Left;
			if (rel < 0)
				return;

			// Spalte unter der Maus: erste Spalte mit Right > mouseX
			int colIdx = -1;
			for (int c = 0; c < layout.Count; c++) {
				if (layout.Right (c) > rel) {
					colIdx = c;
					break;
				}
			}
			if (colIdx < 0)
				colIdx = layout.Count - 1;

			ListViewColumn col = view.Columns [colIdx];
			// Order: -1 (keine) → 1 (asc) → 2 (desc) → -1
			col.Order = (col.Order == -1) ? 1 : (col.Order == 1) ? 2 : -1;

			view.Invalidate();
		}

		public override void OnMouseUp (MouseButtonEventArgs e)
		{
			base.OnMouseUp (e);

			// Drag-Ende: Zustand säubern
			int resizeIdx = m_ResizeIndex;
			if (resizeIdx >= 0) {
				m_ResizeIndex  = -1;
				m_HoverResizer = -1;
				Cursor = Cursors.Default;
				RefreshCursor();
				View.Invalidate();
				Invalidate();
			}
		}

		#endregion

	} // class ColumnHeader


	/// <summary>Scrollender Body mit allen Zeilen + Icons + Texten.</summary>
	public class ListViewBody : ScrollableContainer
	{
		public ListView View { get; }

		public ListViewBody (string name, ListView view)
			: base (name, Docking.Fill, new ListViewBodyWidgetStyle())
		{
			View = view;
			Font = FontManager.Manager.DefaultFont;

			// VertikaL + horizontale Scrollung (WinForms-Parität)
			ScrollBars = ScrollBars.Both;
			if (HScrollBar != null)
				HScrollBar.Scroll += (s, e) =>
				{
					// Shared H-Offset → Header + Body in Phase bleiben.
					if (View != null)
						View.ScrollX = HScrollBar.Value;
					else
						Invalidate();
				};

			AutoScroll = true;
			CanFocus = true;
			TabStop = true;
		}

		IGUIFont m_Font = null;
		public IGUIFont Font
		{
			get { return m_Font; }
			set { 
				if (m_Font != value) {
					m_Font = value;
					ResetCachedLayout();
					Invalidate();
				}
			}
		}

		/// <summary>
		/// Bei Fokuswechsel neu zeichnen: die Auswahl-Farbe (blau/grau) hängt von
		/// <see cref="Widget.IsFocused"/> ab – das Framework invalidiert den Body selbst
		/// aber nicht zwingend. OnGotFocus/OnLostFocus → Invalidate() sichert das ab.
		/// </summary>
		public override void OnGotFocus()
		{
			base.OnGotFocus();
			Invalidate();
		}

		public override void OnLostFocus()
		{
			base.OnLostFocus();
			Invalidate();
		}

		public float RowHeight
		{
			get { return (Font != null ? Font.LineHeight : 20f) + (View != null ? View.CellPadding.Height : 0f); }
		}

		public int RowCount => View != null ? View.Items.Count : 0;

		protected override void LayoutChildren (IGUIContext ctx, RectangleF bounds)
		{
			base.LayoutChildren (ctx, bounds);

			float colW = Bounds.Width;
			float docH = RowCount * RowHeight;
			float docW = colW;
			if (View != null && View.View == ListViewView.LargeIcon) {
				var g = View.LargeIconGeomFor (Bounds.Width);
				// Dokument-Höhe: alle Zeilen inkl. Abstände dazwischen (LI_CELL_MARGIN je Zeile).
				docH = g.Rows * (g.CellH + ListView.LI_CELL_MARGIN);
				// Dokument-Breite: Vollbreite = viewport (keine H-Scroll nötig bei LargeIcon).
				docW = Bounds.Width;
			}
			else if (View != null) {
				// width: die volle Spaltensumme, damit die H-Scrollbar richtig dimensioniert wird
				float tw = View.GetLayout().TotalWidth;
				if (tw > colW)
					colW = tw;
				docW = colW;
			}
			DocumentSize = new SizeF (docW, docH);
		}

		public override void OnPaintBackground (IGUIContext ctx, RectangleF bounds)
		{
			base.OnPaintBackground (ctx, bounds);

			var view = this.View;
			if (view == null || view.Items == null || Font == null)
				return;

			if (view.View == ListViewView.LargeIcon) {
				PaintLargeIcon (ctx, bounds);
				return;
			}

			float scrollOffsetY = 0;
			float scrollWidth   = 0;
			if (VScrollBar != null && VScrollBar.IsVisibleEnabled) {
				scrollWidth   = VScrollBar.Width;
				scrollOffsetY = VScrollBar.Value;
			}

			// H-Scrollung: gemeinsame Position (gilt für Viewport-Basis)
			float scrollX = view.ScrollX;

			float rowHeight = RowHeight;
			float bodyWidth = bounds.Width - scrollWidth;

			// Clip on body area
			RectangleF clipRect = new RectangleF (bounds.Left, bounds.Top, bodyWidth, bounds.Height);
			using (var clip = new ClipBoundClip(ctx, clipRect, false)) {

				int n = view.Items.Count;
				for (int r = 0; r < n; r++) {

					RectangleF rowRect = new RectangleF (
						bounds.Left,
						(r * rowHeight) + bounds.Top - scrollOffsetY,
						bodyWidth,
						rowHeight);

					// Skip fully off-screen rows
					if (rowRect.Bottom < bounds.Top || rowRect.Top > bounds.Bottom)
						continue;

					ListViewItem item = view.Items [r];

					// Selection highlight: blau fokussiert, grau unfokussiert (WinForms-Parität).
					// This = Body (ScrollableContainer) ist das fokusierte Widget.
					bool selActive = (r == view.SelectedIndex);
					if (selActive) {
						Brush selBrush = IsFocused
							? new SolidBrush (Theme.Colors.HighLightBlue)
							: new SolidBrush (Theme.Colors.Base01);
						ctx.FillRectangle (selBrush, rowRect);
					}

					// Spalte 0 zeichnet Icon + Text (siehe Spalten-Loop unten).

					// Draw columns of text
					// — Gemeinsames Spalten-Layout: IDENTISCH wie im Header, damit Breiten & Trennlinien exakt übereinstimmen.
					// (Keine globale Icon-Verschiebung! Jede Zelle beginnt bei denselben Grenzen wie der Header.
					//  Nur der Text in Spalte 0 ist für das Icon nach rechts eingerückt.)
					ColumnLayout layout = view.GetLayout();
					int colCount = layout.Count;

					for (int c = 0; c < colCount; c++) {

						RectangleF cellRect = new RectangleF (
							bounds.Left + layout.X (c) - scrollX,
							rowRect.Top,
							layout.W (c),
							rowHeight);

						string text = item.GetSubItem (c);

						Brush brush = (r == view.SelectedIndex)
							? new SolidBrush (IsFocused ? Color.White : Theme.Colors.Base03)
							: Style.ForeColorBrush;

						// Zellen-Innenabstand + Icon-Abstand (DataGridView-Parität)
					Padding cp  = view.CellPadding;
					Padding ip  = view.IconPadding;

					// Spalte 0: Icon + Text dahinter.
					RectangleF drawRect = cellRect;
					if (c == 0 && view.IconSize > 0) {
						float iconLeft = cellRect.Left + ip.Left;                                  // Icon-Innenabstand (IconPadding)
						float textLeft = iconLeft + view.IconSize + ip.Right;                      // Abstand Icon→Text = IconPadding.Right
						float textW    = Math.Max (1f, cellRect.Width - (iconLeft - cellRect.Left) - view.IconSize - ip.Right);
						drawRect = new RectangleF (textLeft, cellRect.Top, textW, cellRect.Height);

						// Icon: Top/Bottom aus IconPadding (Icons in Zeilen berühren sich nicht —
						// RowHeight enthält bereits CellPadding.Height, Icon zentriert in seiner Box).
						float iconTop  = rowRect.Top + ip.Top;
						float iconH    = Math.Min (view.IconSize, rowHeight - ip.Top - ip.Bottom);
						RectangleF iconRect = new RectangleF (iconLeft, iconTop, view.IconSize, iconH);
						if (view.Images != null && !String.IsNullOrEmpty (item.ImageKey)) {
							TextureImage tex;
							if (view.Images.TryGetValue (item.ImageKey, out tex) && tex != null)
								tex.Paint (ctx, iconRect);
						}
						else if (item.IconGlyph != 0 && view.IconFont != null) {
							ctx.DrawString (item.IconGlyph.ToString(), view.IconFont,
								brush, iconRect, FontFormat.DefaultIconFontFormatCenter);
						}
					}
					else {
						// Normale Text-Zelle: nach cp eingerückt.
						drawRect = new RectangleF (cellRect.Left + cp.Left, cellRect.Top,
							Math.Max (1f, cellRect.Width - cp.Left - cp.Right), cellRect.Height);
					}

						ctx.DrawString (text, Font, brush, drawRect, FontFormat.DefaultSingleLine);

						// Trennlinie rechts der Spalte (Zwischenlinien) – EXAKT an denselben Grenzen wie im Header.
						if (c < colCount - 1) {
							float lineX = bounds.Left + layout.Right (c) - scrollX;
							ctx.DrawLine (Style.BorderColorPen, lineX, rowRect.Top, lineX, rowRect.Bottom);
						}
					}

					// Row separator – bis zur V-Scrollbar, H-Scrollbar wird unten überlagert
					ctx.DrawLine (Style.BorderColorPen, bounds.Left, rowRect.Bottom, bounds.Left + bodyWidth, rowRect.Bottom);
				}
			}
		}

		#region LargeIcon: Zeichnen, Tooltip (DataGridView-Pattern), Maus & Tastatur

		// Tooltip-Cache (DataGridView-Pattern): beim Zeichnen wird pro LargeIcon-Item
		// das Text-Rechteck + der volle Text gecacht; beim MouseMove wird das Item unter
		// dem Cursor per HitTest ermittelt — und NUR wenn der Text gekürzt wurde den Tooltip zeigen.
		System.Collections.Generic.List<RectangleF> LiCellRects = new System.Collections.Generic.List<RectangleF> ();
		System.Collections.Generic.List<string>         LiCellTexts = new System.Collections.Generic.List<string> ();
		int   LiTooltipIndex = -1;

		public void ClearLiCache ()
		{
			LiCellRects.Clear ();
			LiCellTexts.Clear ();
			LiTooltipIndex = -1;
		}

		/// <summary>
		/// LargeIcon-Rendering: zentriert große Icons, darunter 2-zeiliger Text (WinForms-Parätät).
		/// Für jedes Item werden die Text-Rectangle + voller Text gecacht (Tooltip-Quelldaten).
		/// </summary>
		void PaintLargeIcon (IGUIContext ctx, RectangleF bounds)
		{
			var view = this.View;
			if (view == null || view.Items == null || Font == null) return;

			float scroll = 0f;
			if (VScrollBar != null && VScrollBar.IsVisibleEnabled)
				scroll = VScrollBar.Value;

			float viewportW = bounds.Width;
			var g = view.LargeIconGeomFor (viewportW);

			// Clip auf den Viewport
			using (var clip = new ClipBoundClip (ctx, new RectangleF (bounds.Left, bounds.Top, bounds.Width, bounds.Height), false)) {
				LiCellRects.Clear ();
				LiCellTexts.Clear ();

				int n = view.Items.Count;
				for (int i = 0; i < n; i++) {
					(float ox, float oy) = g.Origin (i);
					float x = bounds.Left + ox;
					float y = bounds.Top + oy - scroll;
					RectangleF cell = new RectangleF (x, y, g.CellW, g.CellH);
					if (cell.Bottom < bounds.Top || cell.Top > bounds.Bottom) continue;   // off-screen: nicht zeichnen

					ListViewItem item = view.Items [i];
					string fullText = item.Text ?? "";
					string l1, l2; bool truncated;
					(l1, l2, truncated) = ListView.SplitTwoLines (fullText, Font, g.TextW);

					// Aktives Item: Backgroundfarbe hervorheben (WinForms LargeIcon-Auswahl — dunkler Räumchen).
					bool selActive = (i == view.SelectedIndex);
					if (selActive) {
						Brush sb = new SolidBrush (IsFocused
							? Theme.Colors.HighLightBlue
							: Theme.Colors.Base01);
						ctx.FillRectangle (sb, cell);
					}

					Brush brush = selActive ? new SolidBrush (IsFocused ? Color.White : Theme.Colors.Base03) : Style.ForeColorBrush;

					// Icon: zentriert üben (LargeIconSize) — ImageList oder FontAwesome-Glyph.
					float iconW = Math.Min (view.LargeIconSize, g.CellW - 4f);
					float iconX = x + (g.CellW - iconW) / 2f;
					float iconY = y + 1f;
					RectangleF iconRect = new RectangleF (iconX, iconY, iconW, Math.Min (view.LargeIconSize, iconW));
					if (view.Images != null && !System.String.IsNullOrEmpty (item.ImageKey)) {
						TextureImage tex;
						if (view.Images.TryGetValue (item.ImageKey, out tex) && tex != null)
							tex.Paint (ctx, iconRect);
					} else if (item.IconGlyph != 0 && view.IconFont != null) {
						ctx.DrawString (item.IconGlyph.ToString (), view.IconFont, brush, iconRect, FontFormat.DefaultIconFontFormatCenter);
					}

					// Text: max. 2 Zeilen, je zentriert + pro Zeile Ellipsis (FontFormat.DefaultSingleLineCentered).
					float tY = y + view.LargeIconSize + ListView.LI_TEXT_GAP;
					float tH = g.TextLineH;
					RectangleF line1 = new RectangleF (x, tY, g.CellW, tH);
					if (l1.Length > 0)
						ctx.DrawString (l1, Font, brush, line1, FontFormat.DefaultSingleLineCentered);
					if (l2.Length > 0) {
						RectangleF line2 = new RectangleF (x, tY + tH, g.CellW, tH);
						ctx.DrawString (l2, Font, brush, line2, FontFormat.DefaultSingleLineCentered);
					}

					// Tooltip-Cache (DataGridView-PATTERN): NUR wenn der Text gekürzt wurde.
					// Volle Zelle (Icon + Text) hit-testbar — wie WinForms LargeIcon.
					if (truncated && fullText.Length > 0) {
						LiCellRects.Add (new RectangleF (x, y, g.CellW, g.CellH));
						LiCellTexts.Add (fullText);
					}
				}
			}
		}

		/// <summary>Item-Text-Rect unter dem (window-)Punkt e.X/e.Y, oder -1. Nur mit gecachten Tooltips.</summary>
		int HitTestLi (float x, float y)
		{
			for (int i = 0; i < LiCellRects.Count; i++) {
				if (LiCellRects [i].Contains (x, y))
					return i;
			}
			return -1;
		}

		void HideLiTooltip ()
		{
			if (Root != null && LiTooltipIndex >= 0) {
				Root.HideTooltip ();
				LiTooltipIndex = -1;
			}
		}

		public override void OnMouseMove (MouseMoveEventArgs e)
		{
			base.OnMouseMove (e);
			var view = this.View;
			if (view == null || view.View != ListViewView.LargeIcon || LiCellRects.Count == 0) {
				HideLiTooltip ();
				return;
			}
			// DataGridView-PATTERN: bei MouseMove das Item unter dem Cursor ermitteln ...
			int i = HitTestLi (e.X, e.Y);
			// ... NUR wenn der Text zum Item zu lang (gekürzt) ist, den Tooltip setzen.
			if (i >= 0 && i != LiTooltipIndex) {
				if (Root != null) {
					Root.ShowTooltip (LiCellTexts [i], new PointF (e.X, e.Y));
					LiTooltipIndex = i;
				}
			} else if (i < 0) {
				HideLiTooltip ();
			}
		}

		public override void OnMouseLeave (IGUIContext ctx)
		{
			base.OnMouseLeave (ctx);
			HideLiTooltip ();
		}

		#endregion

		public override void OnMouseDown (MouseButtonEventArgs e)
		{
			base.OnMouseDown (e);

			if (VScrollBar != null && VScrollBar.IsVisibleEnabled) {
				RectangleF scrollbounds = this.Bounds;
				scrollbounds.Width -= VScrollBar.Width;
				if (e.X > scrollbounds.Right)
					return;
			}

			if (View.View == ListViewView.LargeIcon) {
				// LargeIcon: Zelle unter dem Cursor per Grid-Geometrie (Cursortasten-Index).
				float relX = e.X - Bounds.Left;
				float relY = e.Y - Bounds.Top + (VScrollBar?.Value ?? 0f);
				var g = View.LargeIconGeomFor (Bounds.Width);
				int idx = g.IndexAt (relX, relY);
				if (idx >= 0 && idx < View.Items.Count) {
					View.SelectedIndex = idx;
				}
			} else if (RowHeight > 0) {
				int index = (int)((e.Y - Bounds.Top + (VScrollBar?.Value ?? 0f)) / RowHeight);
				if (index >= 0 && index < RowCount)
					View.SelectedIndex = index;
			}
		}

		public override bool OnKeyDown (KeyboardKeyEventArgs e)
		{
			if (!IsFocused)
				return false;

			switch (e.Key) {
				case Keys.Up:
					if (View.View == ListViewView.LargeIcon) { View.MoveSelection (2); View.ScrollItemIntoView (View.SelectedIndex); }
					else SeekIndex (View.SelectedIndex - 1);
					return true;
				case Keys.Down:
					if (View.View == ListViewView.LargeIcon) { View.MoveSelection (3); View.ScrollItemIntoView (View.SelectedIndex); }
					else SeekIndex (View.SelectedIndex + 1);
					return true;
				case Keys.Left:
					if (View.View == ListViewView.LargeIcon) { View.MoveSelection (0); View.ScrollItemIntoView (View.SelectedIndex); }
					return View.View == ListViewView.LargeIcon;
				case Keys.Right:
					if (View.View == ListViewView.LargeIcon) { View.MoveSelection (1); View.ScrollItemIntoView (View.SelectedIndex); }
					return View.View == ListViewView.LargeIcon;
				case Keys.PageUp:
					SeekIndex (View.SelectedIndex - (int)(Bounds.Height / Math.Max(1f, RowHeight)));
					return true;
				case Keys.PageDown:
					SeekIndex (View.SelectedIndex + (int)(Bounds.Height / Math.Max(1f, RowHeight)));
					return true;
				case Keys.Home:
					View.SelectedIndex = 0;
					return true;
				case Keys.End:
					View.SelectedIndex = Math.Max(0, RowCount - 1);
					return true;
				case Keys.Enter:
					if (View != null && View.SelectedIndex >= 0)
						View.OnItemActivated();
					return true;
			}

			return base.OnKeyDown(e);
		}

		public override void OnDoubleClick (MouseButtonEventArgs e)
		{
			base.OnDoubleClick (e);
			// Doppelklick = Zeile aktivieren (WinForms-Parität).
			if (View != null && View.SelectedIndex >= 0)
				View.OnItemActivated();
		}

		void SeekIndex (int newIndex)
		{
			if (RowCount <= 0)
				return;
			int cur = View.SelectedIndex;
			newIndex = Math.Max (0, Math.Min (RowCount - 1, newIndex));
			if (newIndex == cur)
				return;

			View.SelectedIndex = newIndex;

			// Ensure visible
			if (VScrollBar != null && VScrollBar.IsVisibleEnabled) {
				float top    = newIndex * RowHeight;
				float bottom = top + RowHeight;
				if (top < VScrollBar.Value)
					VScrollBar.Value = top;
				else if (bottom > VScrollBar.Value + Bounds.Height)
					VScrollBar.Value = bottom - Bounds.Height;
			}
		}

	} // class ListViewBody


	#region WidgetStyles

	public class ListViewWidgetStyle : WidgetStyle
	{
		public override void InitStyle ()
		{
			SetBackColor   (Theme.Colors.Base03);
			SetForeColor   (Theme.Colors.Base2);
			SetBorderColor (Theme.Colors.Base01);
		}
	}

	public class ListViewBodyWidgetStyle : WidgetStyle
	{
		public override void InitStyle ()
		{
			// Body: heller Hintergrund, dunkler Vordergrund (WinForms-Ähnlichkeit)
			SetBackColor   (Theme.Colors.Base3);
			SetForeColor   (Theme.Colors.Base03);
			SetBorderColor (Theme.Colors.Base01);
		}
	}

	public class ColumnHeaderWidgetStyle : WidgetStyle
	{
		public override void InitStyle ()
		{
			// Header: dunkler Hintergrund, heller Vordergrund — deutlicher Kontrast zum hellen Body
			SetBackColor   (Theme.Colors.Base03);
			SetForeColor   (Theme.Colors.Base2);
			SetBorderColor (Theme.Colors.Base01);
		}
	}

	#endregion

} // namespace
