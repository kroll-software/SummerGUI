using System;
using System.Reflection;
using OpenTK;
using OpenTK.Input;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using KS.Foundation;
using System.Drawing;
using SummerGUI.ColorPicker;

namespace SummerGUI
{
	public class ColorPickerDialog : ChildFormWindow
    {
        public ColorPickerWidget ColorPicker { get; private set; }

		public Color Color
		{
			get
			{
				return ColorPicker.Color;
			}
			set
			{
				ColorPicker.Color = value;
			}
		}

        public ColorPickerDialog (string name, SummerGUIWindow parent)
			: base(name, "Color Picker", 464, 305, parent, true)
		{						
			ShowInTaskBar = false;
            InitButtons();

            ColorPicker = this.AddChild(new ColorPickerWidget("ColorPicker", Docking.Fill, new EmptyWidgetStyle()));
			ColorPicker.Margin = new Padding(8);

			//ColorPicker.BackColor = Color.MistyRose;
		}

        protected virtual void InitButtons()
		{
			// panel
			ButtonContainer buttonContainer = this.Controls.AddChild (new ButtonContainer("buttoncontainer"));
			buttonContainer.BackColor = Theme.Colors.Base2;

			Button btnOK = buttonContainer.AddChild (new Button ("okbutton", "OK"));
			btnOK.Click += (sender, eOK) => OnOK();
			btnOK.HAlign = Alignment.Far;
			btnOK.MinSize = new System.Drawing.SizeF (96, btnOK.MinSize.Height);

            Button btnCancel = buttonContainer.AddChild (new Button ("cancelbutton", "Cancel"));
			btnCancel.Click += (sender, eCancel) => OnCancel();
			btnCancel.HAlign = Alignment.Far;
			btnCancel.MinSize = new System.Drawing.SizeF (96, btnCancel.MinSize.Height);
		}
    }
}