using System;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using KS.Foundation;

namespace SummerGUI.DataGrid.Editors
{
	/// <summary>
	/// CheckBox-Editor (EditType CheckBox). Inherits CheckBox,
	/// implements ICellEditor. A click toggles; Enter confirms; Esc restores the initial state.
	/// Q5: in Phase 1b, with the effort of full legacy coverage approved.</summary>
	public class CheckCellEditor : CheckBox, ICellEditor
	{
		protected bool m_CheckedInitial = false;

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

		public CheckCellEditor () : base ("CellEditor.Check", " ")
		{
			ZIndex = CellEditorBase.ZIndex;
			CanFocus = true;
			CheckedChanged += delegate
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
				Checked = CellEditorBase.ParseBool (CellEditorBase.GetInitialValue (grid, col, cell));
			}
			finally
			{
				m_Initializing = false;
			}
			m_CheckedInitial = Checked;
		}

		public virtual object GetEditValue ()
		{
			return Checked;
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
			if (e.Key == Keys.Space)
				return base.OnKeyDown (e);
			
			if (e.Key == Keys.Enter)
			{
				FireEditCompleted (true);
				return true;
			}

			if (e.Key == Keys.Escape)
			{
				Checked = m_CheckedInitial;
				FireEditCompleted (false);
				return true;
			}

			return base.OnKeyDown (e);
		}

	}
}
