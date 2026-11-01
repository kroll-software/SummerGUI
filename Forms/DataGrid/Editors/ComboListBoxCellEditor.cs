using System;
using System.Drawing;
using OpenTK.Windowing.Common;
using KS.Foundation;
using OpenTK.Input;

namespace SummerGUI.DataGrid.Editors
{
	/// <summary>
	/// Read-only-Combo-Editor (EditType <c>DropDownList</c>). Erbt <see cref="ComboListBox"/>
	/// (kein Text-Eingabefeld) und implementiert ICellEditor.
	/// Filled from the column's ValueList; value = ValueListItem.Value (Q2).
	/// Der aktuelle Zellwert wird sofort angezeigt (Initial-Selektion per Wert-Match);
	/// geändert wird er ausschließlich über die Drop-Down-Auswahl — keine Freitexteingabe.
	/// Enter/OK bestätigt, Esc bricht ab.
	/// Gegenstück mit Texteingabe (EditType <c>DropDownCombo</c>): <see cref="ComboCellEditor"/>.</summary>
	public class ComboListBoxCellEditor : ComboListBox, ICellEditor
	{

		protected DataGridView m_Grid = null;
		protected DataGridColumn m_Column = null;
		protected CellInfo m_Cell = CellInfo.Empty;
		protected bool m_Dirty = false;
		protected bool m_Initializing = false;

		// IOverlayWidget
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

		public ComboListBoxCellEditor () : base ("CellEditor.ComboListBox")
		{
			ZIndex = CellEditorBase.ZIndex;
			CanFocus = true;
			// Read-only: Wertänderungen kommen ausschließlich über die Item-Auswahl
			// (kein TextChanged-Pfad, da kein Eingabefeld existiert).
			SelectedIndexChanged += delegate
			{
				OnEditorValueChanged ();
			};
		}

		public void Initialize (DataGridView grid, DataGridColumn col, CellInfo cell)
		{
			m_Grid = grid;
			m_Column = col;
			m_Cell = cell;
			m_Dirty = false;

			m_Initializing = true;
			try
			{
				// Items in der DEFINIERTEN Reihenfolge (ValueList-Reihenfolge, z.B. "(none)"
				// zuerst + Tasks in Projekt-Reihenfolge): AddUnsorted = AddLast (unsortiert).
				// Add() würde per Comparer sortieren — das wollen wir hier nicht.
				Items.Clear ();
				if (col != null && col.ValueList != null)
					foreach (ValueListItem item in col.ValueList)
						if (item != null)
							Items.AddUnsorted (item.Text, item.Value);

				// Aktueller Zellenwert (ROH, nicht formatiert).
				object cur = null;
				if (grid != null && grid.DataProvider != null && col != null)
				{
					try
					{
						cur = grid.DataProvider.GetValue (cell.Row, col.Key);
					}
					catch (Exception ex)
					{
						ex.LogError ();
					}
				}

				// Selektion per VALUE (Equals, kein String-Vergleich) — dasselbe Muster
				// wie EnumComboBox.SetValue: der Anzeigetext spielt keine Rolle.
				if (cur != null)
					for (int i = 0; i < Items.Count; i++)
					{
						if (Items [i] != null && Items [i].Value != null && Items [i].Value.Equals (cur))
						{
							SelectedIndex = i;
							break;
						}
					}
			}
			finally
			{
				m_Initializing = false;
			}
		}

		public virtual object GetEditValue ()
		{
			object v = SelectedValue;
			if (v != null)
				return v;

			if (SelectedItem != null)
				return SelectedItem.Text;

			// Keine Auswahl (unverändert): der originale Zellwert — nicht der formatierte
			// Anzeigetext, denn bei ValueList-Spalten wäre das der Name statt des Keys
			// (z.B. Task-Key) und würde das Modell verzerren.
			if (m_Grid != null && m_Grid.DataProvider != null && m_Column != null)
			{
				try
				{
					object raw = m_Grid.DataProvider.GetValue (m_Cell.Row, m_Column.Key);
					if (raw != null)
						return raw;
				}
				catch (Exception ex)
				{
					ex.LogError ();
				}
			}

			return null;
		}

		/// <summary>Löst das EditCompleted-Event aus (Interface-Implementierung).</summary>
		public void FireEditCompleted (bool accept)
		{
			EventHandler<EditCompletedEventArgs> h = EditCompleted;
			if (h != null)
				h (this, new EditCompletedEventArgs (accept));
		}

		// Editor-Scoping (wie bei ComboCellEditor): SetBounds hat keinen Layout-Trigger;
		// der Button (ComboListBox: Dock=Fill) muss die Editor-Bounds abdecken,
		// sonst bleibt er bei (0,0,0,0) → unsichtbar und aus der HitTest-Kette.
		public override void SetBounds (float x, float y, float w, float h)
		{
			base.SetBounds (x, y, w, h);
			if (Button != null)
				Button.SetBounds (Bounds);
		}

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

		// Framework-Lücke (wie bei ComboCellEditor): ComboBoxBase.DropDown() registriert das
		// Overlay, aber niemand setzt dessen Bounds (DropDownHeight wird nirgends genutzt)
		// → Liste unsichtbar. Hier Editor-scope-Fix: Größe/Position direkt unterhalb des Editors.
		protected override void ConfigureDropDownWindow (Container window)
		{
			base.ConfigureDropDownWindow (window);
			if (window == null)
				return;

			float itemH = ItemHeight > 0 ? ItemHeight : 20f;
			int n = Math.Max (1, Items.Count);
			float h = Math.Min (n, 10) * itemH + 2f;
			float w = Math.Max (Bounds.Width, 160f);
			window.SetBounds (new RectangleF (Bounds.Left, Bounds.Bottom, w, h));
		}
	}
}
