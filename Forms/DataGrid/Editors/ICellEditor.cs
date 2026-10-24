using System;
using OpenTK.Windowing.GraphicsLibraryFramework;
using KS.Foundation;
using SummerGUI;
using SummerGUI.DataGrid;

namespace SummerGUI.DataGrid.Editors
{
	/// <summary>
	/// Gemeinsames Schnittstellen aller Zell-Editoren (Phase 1).
	/// Editoren sind Overlay-Widgets (ZIndex >= 10000) des hosting DataGridView;
	/// BeginEdit()/EndEdit() des Grids verwaltet die Lebensdauer (AddChild/RemoveChild,
	/// Fokus, Position via SetBounds). Konzeption: CELL-EDITING-Konzeption.md §2.4/§2.13.
	/// </summary>
	public interface ICellEditor : IOverlayWidget
	{
		/// <summary>Editor für die Zelle initialisieren (Wert, Range, Auswahl).</summary>
		void Initialize (DataGridView grid, DataGridColumn col, CellInfo cell);

		/// <summary>Liefert den Editwert, konvertiert gemäß <see cref="DataGridColumn.ValueType"/>.</summary>
		object GetEditValue ();

		/// <summary>True, sobald der Benutzer den Wert geändert hat.</summary>
		bool IsDirty { get; }

		/// <summary>Enter (Accept=true) / Esc (Accept=false); das Grid ruft EndEdit(Accept).</summary>
		event EventHandler<EditCompletedEventArgs> EditCompleted;

		/// <summary>Feuert bei jeder Benutzerveränderung des Werts.</summary>
		event EventHandler EditValueChanged;

		/// <summary>Löst das <see cref="EditCompleted"/>-Event aus (implementiert vom Editor).</summary>
		void FireEditCompleted (bool accept);
	}

	/// <summary>
	/// Enter/Esc-Ergebnis eines Editors.</summary>
	public class EditCompletedEventArgs : EventArgs
	{
		public bool Accept { get; private set; }

		public EditCompletedEventArgs (bool accept)
		{
			Accept = accept;
		}
	}

	/// <summary>
	/// Geteilte Konstanten und Helfer aller Editoren.</summary>
	public static class CellEditorBase
	{
		/// <summary>Overlay-Z-Index (Entscheidung Konzeption §2.13).</summary>
		public const int ZIndex = 10000;

		/// <summary>Nicht-modal: Root schließt bei Outside-Klick automatisch.</summary>
		public static OverlayModes Mode
		{
			get { return OverlayModes.Overlay; }
		}

		/// <summary>Liest den aktuellen (angezeigten) Zellenwert als String.</summary>
		public static string GetInitialValue (DataGridView grid, DataGridColumn col, CellInfo cell)
		{
			if (grid == null || grid.DataProvider == null || col == null || cell == CellInfo.Empty)
				return null;

			try
			{
				return grid.DataProvider.GetFormattedValue (cell.Row, col.Key);
			}
			catch
			{
				return null;
			}
		}

		/// <summary>Verträgliches Bool-Parsen (true/1/ja/yes/x).</summary>
		public static bool ParseBool (string s)
		{
			if (string.IsNullOrEmpty (s))
				return false;

			bool b;
			if (bool.TryParse (s, out b))
				return b;

			s = s.Trim ().ToLowerInvariant ();
			return s == "1" || s == "true" || s == "ja" || s == "yes" || s == "x" || s == "checked";
		}

		/// <summary>Wert-Vergleich, case-insensitive (string/enum-sicher).</summary>
		public static bool ValueMatches (object a, object b)
	 {
			if (a == null && b == null)
				return true;

			if (a == null || b == null)
				return false;

			if (a.Equals (b))
				return true;

			return a.ToString ().Equals (b.ToString (), StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>
		/// Bearbeitet editorgemeinsame Tasten (Enter/Esc).
		/// <paramref name="key"/> ist <c>e.Key</c> aus der Key-Event-Args (hier als Object
		/// übergeben, da der Keys-Typ nur in Expressions-Kontext auflösbar ist).
		/// True, wenn die Taste konsumiert wurde; <paramref name="accept"/> trägt das Ergebnis.</summary>
		public static bool HandleCommonKey (Keys key, out bool accept)
		{
			if (key == Keys.Enter)
			{
				accept = true;
				return true;
			}

			if (key == Keys.Escape)
			{
				accept = false;
				return true;
			}

			accept = false;
			return false;
		}

		/// <summary>Feuert <see cref="ICellEditor.EditCompleted"/>.</summary>
		public static void FireEditCompleted (ICellEditor editor, bool accept)
		{
			editor.FireEditCompleted (accept);
		}
	}
}
