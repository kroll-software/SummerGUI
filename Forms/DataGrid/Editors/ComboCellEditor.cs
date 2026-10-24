using System;
using System.Drawing;
using OpenTK.Windowing.Common;
using KS.Foundation;
using OpenTK.Input;

namespace SummerGUI.DataGrid.Editors
{
	/// <summary>
	/// Combo-Editor (EditType ComboBox). Erbt ComboBox und implementiert ICellEditor.
	/// Filled from the column's ValueList; Q2: value = ValueListItem.Value.
	/// Enter/OK confirms, Esc cancels.</summary>
	public class ComboCellEditor : ComboBox, ICellEditor
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

		public ComboCellEditor () : base ("CellEditor.Combo")
		{
			ZIndex = CellEditorBase.ZIndex;
			CanFocus = true;
			SelectedIndexChanged += delegate
			{
				OnEditorValueChanged ();
			};
			TextChanged += delegate
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

			// TextBox-Font sicherstellen: ComboBox.OnLayout setzt Button-Bounds und ItemHeight nur
			// wenn TB.Font != null; ohne Font bleibt der Button unsichtbar und die DropDown-Liste leer.
			if (grid != null && TB != null && TB.Font == null)
				TB.Font = grid.Font != null ? grid.Font : FontManager.Manager.DefaultFont;

			m_Initializing = true;
			try
			{
				Items.Clear ();
				if (col != null && col.ValueList != null)
					foreach (ValueListItem item in col.ValueList)
						if (item != null)
							Items.Add (item.Text, item.Value);

				string cur = CellEditorBase.GetInitialValue (grid, col, cell);
				int match = -1;
				for (int i = 0; i < Items.Count; i++)
				{
					ComboBoxItem it = Items [i];
					if (it != null && CellEditorBase.ValueMatches (it.Value, cur))
					{
						match = i;
						break;
					}
				}

				if (match >= 0)
					SelectIndex (match);
				else
					Text = cur ?? "";
			}
			finally
			{
				m_Initializing = false;
			}
		}

		public virtual object GetEditValue ()
		{
			object v = SelectedValue;
			if (v == null)
				v = Text;
			return v;
		}


		/// <summary>Löst das EditCompleted-Event aus (Interface-Implementierung).</summary>
		public void FireEditCompleted (bool accept)
		{
			EventHandler<EditCompletedEventArgs> h = EditCompleted;
			if (h != null)
				h (this, new EditCompletedEventArgs (accept));
		}
		// Editor-Scoping: SetBounds hat kein Layout-Trigger; Fill-Kind (TB) manuell setzen.
		public override void SetBounds (float x, float y, float w, float h)
		{
			base.SetBounds (x, y, w, h);
			if (TB != null)
				TB.SetBounds (PaddingBounds);
			EnsureButtonBounds ();
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

		// Framework-Lücke: ComboBoxBase.DropDown() registriert das Overlay, aber niemand setzt dessen
		// Bounds (DropDownHeight wird nirgends genutzt) → Liste unsichtbar. Hier Editor-scope-Fix:
		// Größe/Position direkt unterhalb des Editors.
		protected override void ConfigureDropDownWindow (Container window)
		{
			base.ConfigureDropDownWindow (window);
			if (window == null)
				return;

			float itemH = ItemHeight > 0 ? ItemHeight : (TB != null && TB.Font != null ? TB.Font.TextBoxHeight : 20f);
			int n = Math.Max (1, Items.Count);
			float h = Math.Min (n, 10) * itemH + 2f;
			float w = Math.Max (Bounds.Width, 160f);
			window.SetBounds (new RectangleF (Bounds.Left, Bounds.Bottom, w, h));
		}

		// Button-Router: TB (Dock Fill) bedeckt den Button — Root dispatcht auf den Editor (Top-Overlay),
		// daher hier manuell an den Button weiterreichen wenn der Punkt im Button-Bereich liegt.
		// Combo.OnLayout setzt Button-Bounds nur wenn TB.Font != null; im Editor ist Font ggf. null →
		// dann hier sicherstellen, dass der Button-Bereich existiert.
		private void EnsureButtonBounds ()
		{
			if (Button != null && TB != null && (Button.Width <= 0 || Button.Height <= 0))
			{
				float h = TB.Font != null ? TB.Font.TextBoxHeight : 22f;
				Button.SetBounds (new RectangleF (Bounds.Right - h - 1, Bounds.Top, h, h));
			}
		}

		internal override void InvokeMouseDown (MouseButtonEventArgs e)
		{
			EnsureButtonBounds ();
			if (Button != null && Button.Visible && Button.Bounds.Contains (e.X, e.Y))
				Button.InvokeMouseDown (e);
			else
				base.InvokeMouseDown (e);
		}

		internal override void InvokeMouseUp (MouseButtonEventArgs e)
		{
			EnsureButtonBounds ();
			if (Button != null && Button.Visible && Button.Bounds.Contains (e.X, e.Y))
				Button.InvokeMouseUp (e);
			else
				base.InvokeMouseUp (e);
		}
	}
}
