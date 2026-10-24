using System;
using OpenTK.Windowing.Common;
using KS.Foundation;
using OpenTK.Input;

namespace SummerGUI.DataGrid.Editors
{
	/// <summary>
	/// Icon-Combo editor (a variant of ImageComboBox for grid cells).
	/// Inherits ImageComboBox and implements ICellEditor.
	/// The icon key of an item, by convention, is <see cref="ValueListItem.Value"/> (string) or Text.
	/// Q2: value = ValueListItem.Value.</summary>
	public class ImageComboCellEditor : ImageComboBox, ICellEditor
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

		public ImageComboCellEditor () : base ("CellEditor.ImageCombo", null)
		{
			ZIndex = CellEditorBase.ZIndex;
			CanFocus = true;
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
				Items.Clear ();
				if (col != null && col.ValueList != null)
					foreach (ValueListItem item in col.ValueList)
						if (item != null)
							Items.Add (new ImageComboBoxItem (item.Text, item.Value, ImageKeyFor (item)));

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
			}
			finally
			{
				m_Initializing = false;
			}
		}

		static string ImageKeyFor (ValueListItem item)
		{
			string s = item.Value as string;
			if (s != null)
				return s;
			return item.Text;
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
		// Editor-Scoping: SetBounds hat kein Layout-Trigger; Fill-Kind (Button) manuell setzen.
		public override void SetBounds (float x, float y, float w, float h)
		{
			base.SetBounds (x, y, w, h);
			if (Button != null)
				Button.SetBounds (PaddingBounds);
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
