using System;
using OpenTK.Windowing.Common;
using KS.Foundation;
using System.Globalization;
using OpenTK.Input;

namespace SummerGUI.DataGrid.Editors
{
	/// <summary>
	/// Zahlen-Editor (EditType Number/Integer/Decimal/NeedNumber). Erbt NumberTextBox
	/// und implementiert ICellEditor (Overlay, ZIndex 10000).
	/// Range/Increment kommen aus den Column-Properties Minimum/Maximum/Increment (Q6).</summary>
	public class NumberCellEditor : NumberTextBox, ICellEditor
	{
		protected DataGridView m_Grid = null;
		protected DataGridColumn m_Column = null;
		protected CellInfo m_Cell = CellInfo.Empty;
		protected bool m_Dirty = false;
		protected bool m_HasValue = false;
		protected bool m_Initializing = false;

		// ── IOverlayWidget ────────────────────────────────────────────
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

		// ── ICellEditor ───────────────────────────────────────────────
		public event EventHandler<EditCompletedEventArgs> EditCompleted;
		public event EventHandler EditValueChanged;
		public virtual bool IsDirty
		{
			get { return m_Dirty; }
		}

		public NumberCellEditor () : base ("CellEditor.Number")
		{
			ZIndex = CellEditorBase.ZIndex;
			CanFocus = true;
			ValueChanged += delegate
			{
				OnEditorValueChanged ();
			};
		}

		protected virtual void OnEditorValueChanged ()
		{
			if (m_Initializing)
				return;

			m_Dirty = true;
			if (EditValueChanged != null)
				EditValueChanged (this, EventArgs.Empty);
		}

		public void Initialize (DataGridView grid, DataGridColumn col, CellInfo cell)
		{
			m_Grid = grid;
			m_Column = col;
			m_Cell = cell;
			m_Dirty = false;
			m_HasValue = false;

			m_Initializing = true;
			try
			{
				string s = CellEditorBase.GetInitialValue (grid, col, cell);
				decimal d = 0;
				if (s != null && decimal.TryParse (s, NumberStyles.Any, CultureInfo.InvariantCulture, out d))
				{
					m_HasValue = true;					
				}

				if (col != null)
				{
					MinValue = col.Minimum;
					MaxValue = col.Maximum;
					Increment = col.Increment;
				}

				Value = m_HasValue ? d : 0;
			}
			finally
			{
				m_Initializing = false;
			}
		}

		public virtual object GetEditValue ()
		{
			if (m_Column != null && m_Column.AllowNull && !m_HasValue)
				return null;

			decimal v = Value;
			Type vt = m_Column != null ? m_Column.ValueType : null;

			if (vt == null)
				return v.ToString (CultureInfo.InvariantCulture);

			if (vt == typeof (int))
				return (int) v;
			if (vt == typeof (long))
				return (long) v;
			if (vt == typeof (double))
				return (double) v;
			if (vt == typeof (float))
				return (float) v;
			if (vt == typeof (decimal))
				return v;

			return v.ToString (CultureInfo.InvariantCulture);
		}


		/// <summary>Löst das EditCompleted-Event aus (Interface-Implementierung).</summary>
		public void FireEditCompleted (bool accept)
		{
			EventHandler<EditCompletedEventArgs> h = EditCompleted;
			if (h != null)
				h (this, new EditCompletedEventArgs (accept));
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
	}
}
