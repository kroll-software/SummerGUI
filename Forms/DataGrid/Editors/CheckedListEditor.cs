using System;
using System.Collections.Generic;
using OpenTK.Windowing.Common;
using KS.Foundation;
using SummerGUI.DataGrid;
using SummerGUI.DataGrid.Editors;

namespace SummerGUI
{
	/// <summary>
	/// DropDown-Checklist-Editor (EditType <see cref="EditTypes.CheckList"/>) — 1:1
	/// Legacy (ProjectPlanner <c>ucTaskWorks</c>, Spalte "Preferred Resources"):
	/// der Editor <b>IST</b> das vorhandene <see cref="CheckedListBox"/>-Widget —
	/// beim Klick auf die Zelle öffnet sich die Liste SOFORT in der Zellfläche
	/// (Legacy: CellBeginEdit → <c>checkedListBox1</c> am <c>GetCellDisplayRectangle</c>,
	/// ohne Zwischenstufe), Items <b>sortiert</b> (Legacy: <c>Sorted = true</c>),
	/// Anfangszustand = Zellenwert (List der gewählten Keys → Haken gesetzt).
	///
	/// Abwählen/Auswählen: Klick im Häkchen-Bereich oder Leertaste (Widget-intern),
	/// Navigation hoch/runter, Scrollen über <see cref="ListBox.VScrollBar"/>.
	///
	/// Commit (Enter / Klick außerhalb / OK): Keys aller geprüften Items (Item-Reihenfolge
	/// = sortiert nach Name — 1:1 Legacy <c>checkedListBox1.CheckedItems</c>-Iteration).
	/// Abbrechen (Esc): kein Zell-Schreibzugriff.
	///
	/// Item-Menge: Spalten <see cref="DataGridColumn.ValueList"/>; optional
	/// zeilenweise über <see cref="DataGridColumn.RowItemList"/> filterbar
	/// (Skill-Kaskade: nur Ressourcen des Zeilen-Skills, 1:1 Legacy BeginEdit).
	/// Zellwert bleibt value-basiert (<see cref="List{T}"/> der Keys, ★126);
	/// die Anzeige (Namen, ", "-getrennt) macht die Control (DisplayText-Hook).</summary>
	public class CheckedListEditor : CheckedListBox, ICellEditor
	{
		protected DataGridView m_Grid = null;
		protected DataGridColumn m_Column = null;
		protected CellInfo m_Cell = CellInfo.Empty;
		protected bool m_Dirty = false;
		protected bool m_Initializing = false;

		// IOverlayWidget (ICellEditor-Vertrag)
		public OverlayModes OverlayMode
		{
			get { return CellEditorBase.Mode; }
		}

		public event EventHandler<EventArgs> Closing;

		public void OnClose ()
		{
			if (Closing != null)
				Closing (this, EventArgs.Empty);
		}

		// ICellEditor
		public event EventHandler<EditCompletedEventArgs> EditCompleted;
		public event EventHandler EditValueChanged;

		public virtual bool IsDirty
		{
			get { return m_Dirty; }
		}

		protected virtual void OnEditorValueChanged ()
		{
			if (m_Initializing)
				return;

			m_Dirty = true;
			if (EditValueChanged != null)
				EditValueChanged (this, EventArgs.Empty);
		}

		public CheckedListEditor ()
			: base ("CellEditor.CheckedList")
		{
			ZIndex = CellEditorBase.ZIndex;
			CanFocus = true;

			// Häkchenwechsel = Wertänderung (Legacy: ItemCheck →
			// bItemCheckFlag + NotifyCurrentCellDirty).
			CheckedChanged += delegate
			{
				OnEditorValueChanged ();
			};
		}

		// — Aufbau —

		/// <summary>
		/// Editor initialisieren (1:1 Legacy <c>GRD_CellBeginEdit</c>): Items aus der
		/// Spalte (optional zeilenweise gefiltert), <b>sortiert</b>; Haken gesetzt für
		/// die Keys des (rohen) Zellenwerts. Andere Zellwert-Typen bleiben
		/// unausgewählt (kein Silent-Guess).</summary>
		public void Initialize (DataGridView grid, DataGridColumn col, CellInfo cell)
		{
			m_Grid = grid;
			m_Column = col;
			m_Cell = cell;
			m_Dirty = false;

			m_Initializing = true;
			try
			{
				Items.Clear ();

				// Add() = SORTIERTES Einfügen (Comparator nach Text — 1:1 Legacy
				// checkedListBox1.Sorted = true). AddUnsorted würde die Reihenfolge
				// der ValueList behalten — das will hier die Legacy nicht.
				foreach (ValueListItem item in GetItemsForCell (col, cell))
				{
					if (item == null)
						continue;

					Items.Add (item.Text, item.Value);
				}

				// Anfangszustand aus dem Zellenwert (value-basiert: List der Keys,
				// Compare nach Wert, ★126).
				object cur = GetCellRawValue ();
				var curList = cur as List<string>;
				for (int i = 0; i < Items.Count; i++)
				{
					ListBoxItem li = Items [i];
					if (li == null || li.Value == null)
						continue;

					li.Checked = curList != null && curList.Contains (li.Value);
				}
			}
			finally
			{
				m_Initializing = false;
			}
		}

		/// <summary>Rohzelle (List der Keys) — Exception-resistent wie in den anderen Editoren.</summary>
		object GetCellRawValue ()
		{
			if (m_Grid != null && m_Grid.DataProvider != null && m_Column != null)
			{
				try
				{
					return m_Grid.DataProvider.GetValue (m_Cell.Row, m_Column.Key);
				}
				catch (Exception ex)
				{
					ex.LogError ();
				}
			}

			return null;
		}

		/// <summary>
		/// Item-Menge einer Zelle: <c>col.RowItemList(row)</c> (Skill-Kaskade;
		/// null/Exception → Spalten-<c>ValueList</c>) — 1:1 Legacy BeginEdit
		/// (Resources-Filterung via <c>r.ContainsSkill(skill.Key)</c>).</summary>
		static List<ValueListItem> GetItemsForCell (DataGridColumn col, CellInfo cell)
		{
			if (col == null)
				return new List<ValueListItem> ();

			if (col.RowItemList != null)
			{
				try
				{
					List<ValueListItem> filtered = col.RowItemList (cell.Row);
					if (filtered != null)
						return filtered;
				}
				catch (Exception ex)
				{
					ex.LogError ();
				}
			}

			return (col.ValueList != null)
				? new List<ValueListItem> (col.ValueList)
				: new List<ValueListItem> ();
		}

		/// <summary>
		/// Editor-Bounds: Zellenfläche, aber Höhe = volle Inhaltshöhe (max. Grid-Höhe,
		/// 1:1 Legacy: <c>Size = (rect.Width, Min(GRID.Height - Top, itemHeight*(Count+1)))</c>).
		/// Der Grid-SetBounds-Aufruf folgt auf Initialize → Items.Count ist bekannt.</summary>
		public override void SetBounds (float x, float y, float w, float h)
		{
			float rowH = ItemHeight > 0 ? ItemHeight : 24f;
			float want = Math.Max (h, Items.Count * rowH + 8f);

			// Legacy-Kappe: unterhalb des Grids endet die Liste.
			if (m_Grid != null)
				want = Math.Min (want, h + Math.Max (0f, m_Grid.Height - y));

			base.SetBounds (x, y, w, want);
		}

		// — ICellEditor —

		/// <summary>
		/// Editwert: Keys aller geprüften Items (Item-Reihenfolge = sortiert nach
		/// Name, 1:1 Legacy <c>CheckedItems</c>-Iteration); keine Auswahl → leere
		/// <see cref="List{T}"/> (★126: value-basiert, nie Anzeigetext).</summary>
		public virtual object GetEditValue ()
		{
			var result = new List<string> ();
			for (int i = 0; i < Items.Count; i++)
			{
				ListBoxItem li = Items [i];
				if (li == null || !li.Checked)
					continue;

				if (li.Value is string s)
					result.Add (s);
				else if (li.Value != null)
					result.Add (li.Value.ToString ());
			}

			return result;
		}

		/// <summary>Löst das EditCompleted-Event aus (Interface-Implementierung).</summary>
		public void FireEditCompleted (bool accept)
		{
			EventHandler<EditCompletedEventArgs> h = EditCompleted;
			if (h != null)
				h (this, new EditCompletedEventArgs (accept));
		}

		// Enter → Commit; Esc → Abbrechen (kein Schreibzugriff); die Grid-Maus
		// (Klick außerhalb) löst ebenfalls EditCompleted aus.
		// Leertaste/Hoch/Runter = Widget-intern (CheckedListBox.ToggleChecked /
		// ListBox-Navigation) — erst nach den Global-Tasten prüfen.
		public override bool OnKeyDown (KeyboardKeyEventArgs e)
		{
			bool accept;
			if (CellEditorBase.HandleCommonKey (e.Key, out accept))
			{
				FireEditCompleted (accept);
				return true;
			}

			return base.OnKeyDown (e);
		}
	}
}
