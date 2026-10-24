using System;
using OpenTK.Windowing.Common;
using KS.Foundation;
using System.Globalization;
using OpenTK.Input;

namespace SummerGUI.DataGrid.Editors
{
	/// <summary>
	/// Time-Editor (EditType Time). Inherits TimePicker, implements ICellEditor.
	/// Q10: GetEditValue is controlled by col.ValueType — default string (legacy),
	/// TimeSpan/DateTime per config. Q9: AllowNull + empty = null.</summary>
	public class TimeCellEditor : TimePicker, ICellEditor
	{
		//protected DateTime m_Value = DateTime.MinValue;
		protected bool m_HasValue = false;

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
			// Der Picker ist die einzige Wert-Quelle: lebenden Value ins Editorfeld spiegeln.
			m_Value = Value;
			if (m_Initializing)
				return;

			m_Dirty = true;
			if (EditValueChanged != null)
				EditValueChanged (this, EventArgs.Empty);
		}

		public TimeCellEditor () : base ("CellEditor.Time")
		{
			ZIndex = CellEditorBase.ZIndex;
			CanFocus = true;
			ValueChanged += delegate
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
			m_HasValue = false;

			m_Initializing = true;
			try
			{
				string s = CellEditorBase.GetInitialValue (grid, col, cell);
				if (s != null && DateTime.TryParse (s, CultureInfo.InvariantCulture, DateTimeStyles.None, out m_Value))
					m_HasValue = true;
				else
					m_Value = DateTime.MinValue;

				Value = m_Value;
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

			Type vt = m_Column != null ? m_Column.ValueType : null;
			if (vt != null && vt == typeof (TimeSpan))
				return m_Value.TimeOfDay;
			if (vt != null && vt == typeof (DateTime))
				return m_Value;

			// Default: string (legacy, Q10)
			return m_Value.ToString ("t", CultureInfo.InvariantCulture);
		}


		/// <summary>Löst das EditCompleted-Event aus (Interface-Implementierung).</summary>
		public void FireEditCompleted (bool accept)
		{
			EventHandler<EditCompletedEventArgs> h = EditCompleted;
			if (h != null)
				h (this, new EditCompletedEventArgs (accept));
		}
		// Editor-Scoping: SetBounds hat kein Layout-Trigger; Fill-Kind (TimeBox) manuell setzen.
		public override void SetBounds (float x, float y, float w, float h)
		{
			base.SetBounds (x, y, w, h);
			if (TimeBox != null)
				TimeBox.SetBounds (PaddingBounds);
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
