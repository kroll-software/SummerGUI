using System;
using System.Drawing;
using KS.Foundation;

namespace SummerGUI
{
	/// <summary>
	/// Drop-down host for <see cref="ColorComboBox"/>. A plain <see cref="Container"/>
	/// (overlay, ZIndex 10000) that lays out
	/// <see cref="ColorComboBoxDropDownList"/> (Docking.Fill) and a
	/// <see cref="ButtonContainer"/> (Docking.Bottom) holding a "More ..." button.
	/// Selecting a list item closes the drop-down and commits the selection; the
	/// button opens the modal <see cref="ColorPickerDialog"/> and, on OK, replaces the
	/// first item's colour with the picked one.
	/// </summary>
	public class ColorComboBoxDropDown : Container, IComboBoxDropDown
	{
		public ColorComboBoxDropDownList List { get; private set; }
		public ButtonContainer Buttons { get; private set; }
		public Button MoreButton { get; private set; }

		public ColorComboBoxDropDown(string name)
			: base(name, Docking.None, new DropDownWidgetStyle())
		{
			ZIndex = 10000;
			CanFocus = true;

			List = AddChild(new ColorComboBoxDropDownList("colorlist"));
			List.ItemSelected += (s, e) =>
			{
				OnItemSelected();
				OnClose();
			};

			Buttons = AddChild(new ButtonContainer("colormorebuttoncontainer"));
			MoreButton = Buttons.AddChild(new Button("morebutton", "More ..."));
			MoreButton.Click += More_Click;
		}

		// --- IComboBoxDropDown ---
		public int SelectedIndex
		{
			get { return List.SelectedIndex; }
			set { List.SelectedIndex = value; }
		}

		public event EventHandler<EventArgs> ItemSelected;
		public void OnItemSelected()
		{
			if (ItemSelected != null && !IsDisposed)
				ItemSelected(this, EventArgs.Empty);
		}

		// --- IOverlayWidget ---
		public OverlayModes OverlayMode { get; protected set; } = OverlayModes.Overlay;
		public event EventHandler<EventArgs> Closing;
		public virtual void OnClose()
		{
			if (Closing != null && !IsDisposed)
				Closing(this, EventArgs.Empty);
		}

		private void More_Click(object sender, EventArgs e)
		{
			SummerGUIWindow win = ParentWindow;
			if (win == null)
				return;

			ColorComboBox combobox = null;
			Widget p = this;
			while (p != null && combobox == null)
			{
				combobox = p as ColorComboBox;
				p = p.Parent;
			}
			if (combobox == null)
				return;

			var dlg = new ColorPickerDialog("colorpickerdialog", win);
			Color current = combobox.GetSelectedColor();
			if (current != Color.Empty)
				dlg.Color = current;

			dlg.ShowDialog(win);
			dlg.Dispose();

			if (dlg.Result == DialogResults.OK)
			{
				Color newColor = dlg.Color;

				// Spec: a fresh colour that is not yet in the list is appended
				// (Name = the color value, e.g. ARGB: (128, 243, 29, 69)).
				// Selecting it must raise SelectedIndexChanged so observers can
				// react to the newly selected color.
				ColorItem target = null;
				foreach (ComboBoxItem i in combobox.Items)
					if (i is ColorItem ci && ci.Color.ToArgb() == newColor.ToArgb())
						target = ci;

				if (target == null)
				{
					target = new ColorItem(newColor) { Text = ColorItem.FormatName(newColor) };
					combobox.Items.AddLast(target);
				}

				// Note: BinarySortedList.IndexOf uses the Text comparer
				// (binary search) — wrong for items appended out of sort order.
				// Find the position by reference instead.
				int idx = -1;
				for (int i = 0; i < combobox.Items.Count; i++)
				{
					if (ReferenceEquals(combobox.Items[i], target))
					{
						idx = i;
						break;
					}
				}

				// m_SelectedIndex change → OnSelectedIndexChanged (ComboBoxBase).
				combobox.SelectedIndex = idx;
				combobox.Invalidate();
				OnClose();
			}
		}
	}
}
