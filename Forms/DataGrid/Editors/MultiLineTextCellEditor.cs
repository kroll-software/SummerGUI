using System;
using OpenTK.Windowing.Common;
using KS.Foundation;
using OpenTK.Input;

namespace SummerGUI.DataGrid.Editors
{
	/// <summary>
	/// Mehrzeiliger Text-Editor (EditType MultiLineString). Erbt MultiLineTextBox
	/// und implementiert ICellEditor (Overlay, ZIndex 10000).
	/// </summary>
	public class MultiLineTextCellEditor : MultiLineTextBox, ICellEditor
	{
		protected DataGridView m_Grid = null;
		protected DataGridColumn m_Column = null;
		protected CellInfo m_Cell = CellInfo.Empty;
		protected bool m_Dirty = false;
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

		public MultiLineTextCellEditor () : base ("CellEditor.MultiLineText")
		{
			ZIndex = CellEditorBase.ZIndex;
			CanFocus = true;
			TextChanged += delegate
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

			m_Initializing = true;
			try
			{
				Text = CellEditorBase.GetInitialValue (grid, col, cell) ?? "";
			}
			finally
			{
				m_Initializing = false;
			}
			SelectAll ();
		}

		public virtual object GetEditValue ()
		{
			return Text;
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
