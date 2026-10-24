using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using KS.Foundation;

namespace SummerGUI
{
	public class TableLayoutContainerStyle : WidgetStyle
	{
		public override void InitStyle ()
		{
			SetBackColor (Color.Empty);
			SetForeColor (Theme.Colors.Base03);
			SetBorderColor (Color.Empty);
		}
	}

	public enum TableSizeModes
	{
		None,
		Fixed,
		Content,
		Fill,
		Relative      // col.Width = GEWICHT/Bruchteil (z.B. 0.1f); prop. Verteilung der freien Breite (WinForms-SizeTypePercent). Ein einzelnes Relative = ganze freie Breite
	}		

	public class WidgetTableCell : IComparable<WidgetTableCell>
	{
		public Widget Widget { get; set; }

		public WidgetTableCell(int row, int col)
		{			
			Row = row;
			Column = col;
		}

		~WidgetTableCell()
		{
			Widget = null;
		}

		public int CompareTo(WidgetTableCell other)
		{
			int ret = Row.CompareTo (other.Row);
			if (ret == 0)
				return Column.CompareTo (other.Column);
			return ret;
		}

		public int Column { get; set; }
		public int Row { get; set; }

		public int ColumnSpan { get; set; }
		public int RowSpan { get; set; }

		public bool AutoSize { get; set; }

		public Alignment HAlign { get; set; }
		public Alignment VAlign { get; set; }

		[DpiScalable]
		public SizeF MinSize { get; set; }
		
		[DpiScalable]
		public SizeF MaxSize { get; set; }
		public RectangleF Bounds { get; private set; }

		public float Width 
		{ 
			get {
				return Bounds.Width;
			}
		}

		public float Height
		{ 
			get {
				return Bounds.Height;
			}
		}

		public int TabIndex
		{
			get{
				if (Widget == null)
					return -1;
				return Widget.TabIndex;
			}
		}

		public SizeF PreferredSize { get; private set; }
		public SizeF ProposedSize { get; set; }

		public void UpdatePreferredSize(IGUIContext ctx)
		{
			if (Widget == null) {
				PreferredSize = ProposedSize;
			} else {
				if (ProposedSize != Bounds.Size)
					Widget.Update (false, -1);
				PreferredSize = Widget.PreferredSize (ctx, ProposedSize).Inflate (Widget.Margin);
			}
		}

		public void OnLayout(IGUIContext ctx, RectangleF bounds)
		{			
			if (Widget != null) {
				if (bounds.Width != Bounds.Width)
					Widget.Update (false, -1);
				Widget.OnLayout (ctx, bounds.Inflate(Widget.Margin));
				Bounds = new RectangleF(bounds.Left, bounds.Top, bounds.Width, 
					Math.Max(bounds.Height, Widget.Height));
			} else {
				Bounds = bounds;
			}
		}

		public override string ToString ()
		{
			return string.Format ("[WidgetTableCell: Column={0}, Row={1}, ColumnSpan={2}, RowSpan={3}, AutoSize={4}, HAlign={5}, VAlign={6}, MinSize={7}, MaxSize={8}, Bounds={9}, Width={10}, Height={11}, PreferredSize={12}, ProposedSize={13}, Widget={14}]", Column, Row, ColumnSpan, RowSpan, AutoSize, HAlign, VAlign, MinSize, MaxSize, Bounds, Width, Height, PreferredSize, ProposedSize, Widget);
		}
	}

	public class WidgetTableColumn : IComparable<WidgetTableColumn>
	{
		public int Index { get; private set; }
		public TableSizeModes SizeMode { get; set; }
				
		public float Width { get; set; }

		public WidgetTableColumn(int index)
		{
			Index = index;
			SizeMode = TableSizeModes.Fill;
		}

		public int CompareTo(WidgetTableColumn other)
		{
			if (other == null)
				return 0;
			return Index.CompareTo (other.Index);
		}

		public override string ToString ()
		{
			return string.Format ("[WidgetTableColumn: SizeMode={0}, Width={1}]", SizeMode, Width);
		}
	}

	public class WidgetTableColumnCollection : BinarySortedList<WidgetTableColumn> {}

	public class WidgetTableRow : IComparable<WidgetTableRow>
	{
		public BinarySortedList<WidgetTableCell> Cells { get; private set; }

		public TableSizeModes SizeMode { get; set; }

		public int Index { get; private set; }

		public WidgetTableRow(int index)
		{
			Index = index;
			Cells = new BinarySortedList<WidgetTableCell> ();
			SizeMode = TableSizeModes.Content;
		}

		public int CompareTo(WidgetTableRow other)
		{
			if (other == null)
				return 0;
			return Index.CompareTo (other.Index);
		}

		public int CellCount
		{
			get{
				return Cells.Count;
			}
		}
		
		public float Height { get; set; }

		public void AddCell(Widget widget, int column, int columnSpan)
		{
			Cells.Add (new WidgetTableCell(Index, column) {
				Widget = widget,
				ColumnSpan = columnSpan
			});
		}

		public void AddCell(WidgetTableCell cell)
		{
			if (cell == null)
				return;
			Cells.Add (cell);
		}

		public override string ToString ()
		{
			return string.Format ("[WidgetTableRow: Cells={0}, SizeMode={1}, CellCount={2}, Height={3}]", Cells, SizeMode, CellCount, Height);
		}
	}

	public class WidgetTableRowCollection : BinarySortedList<WidgetTableRow> {}

	public class WidgetTable
	{
		public WidgetTableRowCollection Rows { get; private set; }
		public WidgetTableColumnCollection Columns { get; private set; }

		public WidgetTable()
		{
			Rows = new WidgetTableRowCollection ();
			Columns = new WidgetTableColumnCollection ();
		}

		public int RowCount 
		{ 
			get {
				return Rows.Count;
			}
		}
		public int ColumnCount 
		{ 
			get {
				if (Rows.Count == 0)
					return 0;

				int count = 0;
				foreach (var row in Rows)
					foreach (var cell in row.Cells)
						count = Math.Max (count, cell.Column + cell.ColumnSpan);
				return count;
			}
		}
		
		// Ein Widget in seiner Zelle finden (uebergeordnete Zeile + Index in der Zellenliste).
		bool FindCell (Widget widget, out WidgetTableRow parentRow, out int cellIndex)
		{
			parentRow = null;
			cellIndex = -1;
			for (int row = 0; row < Rows.Count; row++)
			{
				for (int c = 0; c < Rows [row].CellCount; c++)
				{
					if (ReferenceEquals (Rows [row].Cells [c].Widget, widget))
					{
						parentRow = Rows [row];
						cellIndex = c;
						return true;
					}
				}
			}
			return false;
		}

		// WinForms-TableLayoutPanel.SetRow-Paritaet: ein Widget nach dem Hinzufuegen in eine
		// andere Zeile umhaengen (gleiche Spalte bleibt). Loest das "gemeinsame Slot"-Muster:
		// z.B. Start/StartTime links neben Duration ODER neben End, je nach TaskTyp.
		public void SetRow (Widget widget, int row)
		{
			if (!FindCell (widget, out WidgetTableRow parentRow, out int cellIndex))
				return;

			WidgetTableCell cell = parentRow.Cells [cellIndex];
			if (cell.Row == row)
				return;	// kein Umdocking noewig

			while (Rows.Count <= row)
				Rows.AddLast (new WidgetTableRow (Rows.Count));
			WidgetTableRow target = Rows [row];
			if (target != parentRow)
			{
				parentRow.Cells.RemoveAt (cellIndex);
				cell.Row = row;	// Sort-Key vor dem Re-Add aktualisieren
				target.Cells.Add (cell);
			}
		}

		// WinForms-TableLayoutPanel.SetColumn-Paritaet: in eine andere Spalte umhaengen.
		public void SetColumn (Widget widget, int column)
		{
			if (!FindCell (widget, out WidgetTableRow parentRow, out int cellIndex))
				return;

			WidgetTableCell cell = parentRow.Cells [cellIndex];
			if (cell.Column == column)
				return;

			while (Columns.Count <= column)
				Columns.AddLast (new WidgetTableColumn (Columns.Count));
			parentRow.Cells.RemoveAt (cellIndex);
			cell.Column = column;
			parentRow.Cells.Add (cell);
		}

		int tabIndex = 0;

		public void AddWidget(Widget widget)
		{
			if (RowCount == 0) {
				AddWidget (widget, 0, 0);
			} else {				
				AddWidget (widget, RowCount - 1, Rows [RowCount - 1].CellCount);
			}
		}

		public void AddWidget(Widget widget, int row, int column, int rowSpan = 1, int columnSpan = 1, bool autoSize = true)
		{
			// Spalten auf die volle Span-Range wachsen lassen (nicht nur bis Start-Index),
			// sonst fehlt die rechte Spalte(n), die nur ueber einen Span erreicht wird;
			// UpdateColumnWidthsNormal/LayoutNormal indexen Table.Columns[..column+span-1] ungesichert.
			// Fuer span-1-Zellen identisch zu zuvor (while Count <= idx) -> backward-compatible.
			while (RowCount <= row)
				Rows.AddLast (new WidgetTableRow (Rows.Count));
			while (Columns.Count < column + columnSpan)
				Columns.AddLast (new WidgetTableColumn (Columns.Count));
		
			if (widget != null)
				widget.TabIndex = tabIndex++;

			Rows [row].AddCell (new WidgetTableCell(row, column) {				
				RowSpan = rowSpan,
				ColumnSpan = columnSpan,
				AutoSize = autoSize,
				Widget = widget,
			});
		}

		public void Clear()
		{
			Rows.Clear ();
			Columns.Clear ();
		}
	}

	public class TableLayout : DisposableObject
	{
		public Container Owner { get; private set; }

		public WidgetTable Table { get; private set; }

		public TableLayout(Container owner)
		{
			Owner = owner;
			Table = new WidgetTable ();
		}
			
		private SizeF m_CellPadding;		
		public SizeF CellPadding 
		{ 
			get {
				return m_CellPadding;
			}
			set {
				if (m_CellPadding != value) {
					m_CellPadding = value;
					ResetCachedLayout ();
				}
			}
		}

		private float m_CollapsibleColumnsWidth;
		public float CollapsibleColumnsWidth 
		{ 
			get {
				return m_CollapsibleColumnsWidth;
			}
			set {
				if (m_CollapsibleColumnsWidth != value) {
					m_CollapsibleColumnsWidth = value;
					ResetCachedLayout();
				}
			}
		}

		public float Width { get; protected set; }
		public float Height { get; protected set; }

		// *** this is never called ..
		/*** 
		void LayoutCells(IGUIContext ctx)
		{			
			for (int i = 0; i < Table.Rows.Count; i++) {
				WidgetTableRow row = Table.Rows [i];
				for (int k = 0; k < row.Cells.Count; k++) {
					WidgetTableCell cell = row.Cells [k];
					if (cell != null && cell.Widget != null)
						//cell.Widget.OnLayout (ctx, cell.Widget.Bounds);
						cell.Widget.OnLayout (ctx, cell.Bounds);
				}
			}
		}
		***/


		/*** ***/
		void UpdateColumnWidthsCollapsed(IGUIContext ctx, SizeF proposedSize)
		{			
			float width = proposedSize.Width;
			Table.Columns.ForEach (col => col.Width = width);
			Width = width;
		}

		void UpdateRowHeightsCollapsed(IGUIContext ctx, SizeF proposedSize)
		{
			float w = proposedSize.Width;
			float h = 0;

			Table.Rows.SelectMany (row => row.Cells).Where (cell => cell.Widget != null)
				.OrderBy (cell => cell.TabIndex).ForEach (cell => {
					int row = cell.Row;
					if (row >= 0 && row < Table.Rows.Count) {
						cell.ProposedSize = new SizeF (proposedSize.Width, Table.Rows[row].Height);
						cell.UpdatePreferredSize (ctx);
						h += cell.PreferredSize.Height + CellPadding.Height;
						w = Math.Max(w, cell.PreferredSize.Width);
					}
				});

			Width = Math.Max(w, proposedSize.Width);
			Height = h - CellPadding.Height;
		}


		// *** auf den hier würde ich auch gerne verzichten..
		void UpdateColumnWidthsNormal(IGUIContext ctx, SizeF proposedSize)
		{
			int columnCount = Table.Columns.Count;

			float FillColWeight = 0;       // Fill/None = Gewicht 1.0; Relative = ihr Bruchteil (SizeTypePercent)
			float FixedColWidth = 0;       // absolute (Fixed) + Content = reserviert, wird NICHT verteilt
			for (int i = 0; i < columnCount; i++) {
				WidgetTableColumn col = Table.Columns [i];
				switch (col.SizeMode) {
				case TableSizeModes.Content:
					float maxWidth = 0;
					for (int k = 0; k < Table.RowCount; k++) {
						WidgetTableRow row = Table.Rows [k];
						for (int m = 0; m < row.Cells.Count; m++) {
							WidgetTableCell cell = row.Cells [m];
							if (cell.Column == i) {								
								if (cell.ColumnSpan == 1) {																		
									cell.UpdatePreferredSize (ctx);
									maxWidth = Math.Max (maxWidth, cell.PreferredSize.Width);
								}
								break;
							}
						}
					}
					col.Width = maxWidth;
					FixedColWidth += maxWidth;
					break;
				case TableSizeModes.None:
				case TableSizeModes.Fill:
					FillColWeight += 1.0f;             // gleichverteilt (Gewicht 1.0)
					break;
				case TableSizeModes.Fixed:
					FixedColWidth += col.Width;
					break;
				case TableSizeModes.Relative:
					FillColWeight += col.Width;        // Gewicht = Bruchteil (z.B. 0.1f)
					break;
				}
			}

			if (FillColWeight > 0) {
				float freePool = proposedSize.Width - FixedColWidth - ((columnCount - 1) * CellPadding.Width);
				if (freePool < 0) freePool = 0;         // Überbuchung (Fixed+Content > Breite): kein neg. Fill
				for (int i = 0; i < columnCount; i++) {
					WidgetTableColumn col = Table.Columns [i];
					float weight = 0;
					if (col.SizeMode == TableSizeModes.Fill || col.SizeMode == TableSizeModes.None)
						weight = 1.0f;                  // gleichverteilt
					else if (col.SizeMode == TableSizeModes.Relative)
						weight = col.Width;             // Gewicht = Bruchteil
					if (weight > 0)
						col.Width = freePool * (weight / FillColWeight);   // proportional (WinForms-SizeTypePercent)
				}
			}

			Width = Table.Columns.Sum(col => col.Width) + ((Table.Columns.Count - 1) * CellPadding.Width);
			
			//DumpColumns ();
		}			
			
		void UpdateRowHeightsNormal(IGUIContext ctx, SizeF proposedSize)
		{
			int FillRowCount = 0;
			float FixedRowHeight = 0;
			for (int i = 0; i < Table.RowCount; i++) {
				WidgetTableRow row = Table.Rows [i];
				switch (row.SizeMode) {
				case TableSizeModes.Content:
					float maxHeight = 0;
					for (int k = 0; k < row.CellCount; k++) {
						WidgetTableCell cell = row.Cells [k];
						if (cell.RowSpan == 1 && cell.Widget != null && cell.Widget.Visible) {
							float w = 0;
							for (int m = cell.Column; m < cell.Column + cell.ColumnSpan; m++)
								w += Table.Columns [m].Width + CellPadding.Width;
							cell.ProposedSize = new SizeF (w, row.Height);
							cell.UpdatePreferredSize (ctx);
							maxHeight = Math.Max (maxHeight, cell.PreferredSize.Height);
						}
					}
					row.Height = maxHeight;
					FixedRowHeight += maxHeight;
					break;
				case TableSizeModes.None:
				case TableSizeModes.Fill:
					FillRowCount++;
					break;
				case TableSizeModes.Fixed:
					FixedRowHeight += row.Height;
					break;
				}
			}

			if (FillRowCount > 0) {
				float fillRowHeight = (proposedSize.Height - FixedRowHeight - ((Table.RowCount - 1) * CellPadding.Height)) / FillRowCount;
				for (int i = 0; i < Table.RowCount; i++) {
					WidgetTableRow row = Table.Rows [i];
					if (row.SizeMode == TableSizeModes.Fill || row.SizeMode == TableSizeModes.None)
						row.Height = fillRowHeight;
				}
			}

			Height = (Table.Rows.Sum(row => row.Height) + ((Table.Rows.Count - 1) * CellPadding.Height));
		}

		public void DumpColumns()
		{
			Table.Columns.ForEach (col => Debug.WriteLine (col.ToString ()));
		}


		// *** Calculate Size / PreferredSize ***
		protected virtual void ResetCachedLayout()
		{
			CachedPreferredSize = SizeF.Empty;
			CachedProposedSize = SizeF.Empty;
		}
		public void Update()
		{
			ResetCachedLayout ();
		}

		SizeF CachedProposedSize;
		SizeF CachedPreferredSize;
		public SizeF PreferredSize (IGUIContext ctx, SizeF proposedSize)
		{			
			//if (CachedPreferredSize == SizeF.Empty) {
			if (proposedSize != CachedProposedSize) {
				CachedProposedSize = proposedSize;

				if (CollapsibleColumnsWidth > 0 && proposedSize.Width < CollapsibleColumnsWidth) {
					UpdateColumnWidthsCollapsed (ctx, proposedSize);
					UpdateRowHeightsCollapsed (ctx, proposedSize);
				} else {
					UpdateColumnWidthsNormal (ctx, proposedSize);
					UpdateRowHeightsNormal (ctx, proposedSize);
				}

				CachedPreferredSize = new SizeF (Width, Height);
			} else {
				//Console.WriteLine ("kein Layout erforderlich.");
			}
			return CachedPreferredSize;
		}


		// *** Layout table and cells / children ***

		void LayoutNormal(IGUIContext ctx, RectangleF bounds)
		{
			// ToDo: 
			// this would be a nice optimization
			// unfortunately Image Layout happens too late	
			// and is not ready on first OnLayout()
			// The Dirty flag is updated with some strange side effects, 
			// this has to be checked later
			/***
			if (false && !force && m_LastLayoutBounds == bounds) {
				LayoutCells (ctx);
				return;
			}
			m_LastLayoutBounds = bounds;
			***/

			int rows = Table.RowCount;
			int cols = Table.ColumnCount;

			if (rows == 0 || cols == 0)
				return;

			float width = bounds.Width;
			float height = bounds.Height;

			//UpdateColumnWidthsNormal (ctx, bounds);
			//UpdateRowHeightsNormal (ctx, bounds);
			//LayoutCells (ctx);
			//return;

			float top = bounds.Top;
			float left;

			for (int i = 0; i < Table.Rows.Count; i++) {
				WidgetTableRow row = Table.Rows [i];

				left = bounds.Left;
				int lastColumn = 0;
				for (int k = 0; k < row.Cells.Count; k++) {
					float colHeight = row.Height;
					WidgetTableCell cell = row.Cells [k];

					if (cell != null) {
						// Skip columns
						for (int m = lastColumn; m < cell.Column; m++)
							left += Table.Columns [m].Width + CellPadding.Width;

						float colWidth = 0;
						for (int m = cell.Column; m < cell.Column + cell.ColumnSpan; m++)
							colWidth += Table.Columns [m].Width;
						colWidth += CellPadding.Width * (cell.ColumnSpan - 1);

						lastColumn = cell.Column + cell.ColumnSpan;

						// Skip rows
						int j = i;
						while (cell.Row > j && j < rows) {
							top += Table.Rows [j].Height + CellPadding.Height;
							j++;
						}

						while (j < cell.Row + cell.RowSpan - 1) {
							j++;
							if (j < rows)
								colHeight += Table.Rows [j].Height + CellPadding.Height;
						}

						// calculate cell
						cell.OnLayout (ctx, new RectangleF (left, top, colWidth, colHeight));
						left += colWidth + CellPadding.Width;
					} else {
						left += Table.Columns[k].Width + CellPadding.Width;
					}						
				}

				top += Table.Rows [i].Height + CellPadding.Height;
			}			
		}

		void LayoutCollapsed(IGUIContext ctx, RectangleF bounds)
		{
			float w = bounds.Width;
			float x = bounds.X;
			float y = bounds.Y;

			Table.Rows.SelectMany (row => row.Cells).Where (cell => cell.Widget != null)
				.OrderBy (cell => cell.TabIndex)
				.ForEach (cell => {
					int row = cell.Row;
					if (row >= 0 && row < Table.Rows.Count) {						
						float h = cell.PreferredSize.Height;
						cell.OnLayout(ctx, new RectangleF(x, y, bounds.Width, h));
						y += h + CellPadding.Height;
						w = Math.Max(w, cell.Bounds.Width);
					}
				});

			Width = Math.Max(w, bounds.Width);
			Height = y - bounds.Y - CellPadding.Height;
		}			
			
		public void OnLayout(IGUIContext ctx, RectangleF bounds)
		{					
			if (CollapsibleColumnsWidth > 0 && bounds.Width < CollapsibleColumnsWidth)
				LayoutCollapsed (ctx, bounds);
			else
				LayoutNormal (ctx, bounds);
		}

		protected override void CleanupUnmanagedResources ()
		{			
			Table.Clear ();
			Owner = null;
			base.CleanupUnmanagedResources ();
		}
	}


	// ************ the public container **********

	public class TableLayoutContainer : Container
	{		
		public TableLayout Layout { get; private set; }
		public bool AutoSize { get; set; }

		public TableLayoutContainer (string name)
			: this(name, new TableLayoutContainerStyle()) {
		}

		public TableLayoutContainer (string name, IWidgetStyle style)
			: base (name, Docking.Fill, style)
		{
			Layout = new TableLayout (this);
			Layout.CellPadding = new SizeF (12f, 6f);
			Margin = Padding.Empty;
			Padding = Padding.Empty;
		}

		public WidgetTableRowCollection Rows 
		{ 
			get { 
				return Layout.Table.Rows;
			} 
		}

		public WidgetTableColumnCollection Columns 
		{ 
			get { 
				return Layout.Table.Columns;
			} 
		}
		
		[DpiScalable]
		public SizeF CellPadding
		{ 
			get {
				return Layout.CellPadding;
			}
			set {
				Layout.CellPadding = value;
			}
		}

		[DpiScalable]	// ToDo: ???
		public float CollapsibleColumnsWidth
		{ 
			get {
				return Layout.CollapsibleColumnsWidth;
			}
			set {
				Layout.CollapsibleColumnsWidth = value;
			}
		}			

		public T AddChild<T>(T child, int row, int column, int rowSpan = 1, int columnSpan = 1, bool autoSize = true) where T: Widget
		{
			base.AddChild (child);
			Layout.Table.AddWidget (child, row, column, rowSpan, columnSpan, autoSize);
			AutoSize = true;
			ResetCachedLayout ();
			return child;
		}
			
		// Hide this from inheritance..
		[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
		protected new T AddChild<T>(T child) where T : Widget
		{
			base.AddChild (child);
			Layout.Table.AddWidget (child);
			ResetCachedLayout ();
			return child;
		}
		
		// WinForms-TableLayoutPanel-Paritaet: ein Widget nach dem AddChild in eine andere
		// Zeile/Spalte umhaengen (ohne Doppel-Widget, ohne Overlay). Basis fuer die
		// Legacy-TaskEditDialog-Logik: Start/StartTime rutschen neben Duration (Fixed)
		// ODER bleiben bei End (Static), das inaktive Band kollabiert (Content-Zeile, 0).
		public void SetRow (Widget child, int row)
		{
			Layout.Table.SetRow (child, row);
			ResetCachedLayout ();
		}

		public void SetColumn (Widget child, int column)
		{
			Layout.Table.SetColumn (child, column);
			ResetCachedLayout ();
		}
			
		protected override void ResetCachedLayout ()
		{			
			base.ResetCachedLayout ();
			Layout.Update ();
		}

		SizeF lastProposedSize = SizeF.Empty;
		public override SizeF PreferredSize (IGUIContext ctx, SizeF proposedSize)
		{			
			// ToDo: Optimieren/Cachen

			if (Layout == null)
				return base.PreferredSize (ctx, proposedSize);

			//if (CachedPreferredSize == SizeF.Empty || true)
			if (lastProposedSize != proposedSize)
			{
				lastProposedSize = proposedSize;
				
				SizeF sz = Layout.PreferredSize(ctx, new SizeF(proposedSize.Width - Padding.Width, proposedSize.Height - Padding.Height));
				CachedPreferredSize = ClampMinMax(new SizeF (sz.Width + Padding.Width, sz.Height + Padding.Height));				
			}			
		
			return CachedPreferredSize;
		}

        public override void OnResize(IGUIContext ctx)
        {
            base.OnResize(ctx);
			lastProposedSize = SizeF.Empty;	// ToDo: Check this!
			Layout?.Update();
        }

		/*** ***/
		public override void OnLayout (IGUIContext ctx, RectangleF bounds)
		{
			if (!Visible || IsLayoutSuspended)
				return;			

			SizeF size = PreferredSize (ctx, bounds.Size);		
			RectangleF newBounds = new RectangleF (bounds.Location, size);
			base.OnLayout (ctx, newBounds);
		}

		protected override void LayoutChildren (IGUIContext ctx, RectangleF bounds)
		{			
			Layout.OnLayout (ctx, bounds);
		}
		
		public override void OnAfterLayout (IGUIContext ctx, RectangleF bounds)
		{			
			if (AutoSize) {				
				SetBounds (new RectangleF (bounds.Left, bounds.Top, Layout.Width + Padding.Width, Layout.Height + Padding.Height));				
			}

			base.OnAfterLayout (ctx, bounds);
		}        

		public override string ToString ()
		{
			return string.Format ("[TableLayoutContainer: Rows={0}, Columns={1}]", Layout.Table.RowCount, Layout.Table.ColumnCount);
		}

		protected override void CleanupManagedResources ()
		{
			Layout.Dispose ();
			base.CleanupManagedResources ();
		}			
	}
}

