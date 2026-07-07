using System;
using SummerGUI;
using KS.Foundation;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Drawing;

namespace SummerGUI.DataGrid
{

    public class DataGridSearchBar : Container
    {
        public CommandInputTextBox Input { get; private set; }
		public Button OkButton { get; private set; }
		public Button CancelButton { get; private set; }

		public DataGridView Owner { get; private set; }
		public string InfoText
		{
			get {
				return Input.InfoText;
			}
			set {
				Input.InfoText = value;
			}
		}

        public EventHandler<EventArgs> ApplySearch;
        public EventHandler<EventArgs> ResetSearch;

		public DataGridSearchBar (string name, DataGridView owner, string infoText, Docking dock = Docking.Bottom, IWidgetStyle style = null)
			: base(name, dock, style ?? new DarkPanelWidgetStyle())
		{
			Owner = owner;

			Input = AddChild(new CommandInputTextBox ("input", infoText));

			CancelButton = AddChild(new Button ("cancelbutton", "Cancel", ColorContexts.Default));
			CancelButton.Dock = Docking.Right;
			CancelButton.HandlesEscapeKey = true;
			//CancelButton.ApplyCommandButtonPadding ();
			CancelButton.Margin = new Padding (4, 0, 0, 0);
			CancelButton.Click += delegate {
				if (Owner != null) {					
                    ResetSearch?.Invoke(this, EventArgs.Empty);
					Visible = false;
                    Owner.Focus ();
				}
			};

			OkButton = AddChild(new Button ("actionbutton", "Find", ColorContexts.Default));
			OkButton.Dock = Docking.Right;
			OkButton.HandlesEnterKey = true;
			OkButton.ApplyCommandButtonPadding ();
			OkButton.MakeDefaultButton ();
			OkButton.Click += delegate {				
                if (String.IsNullOrWhiteSpace(Input.Text))
                {
                    ResetSearch?.Invoke(this, EventArgs.Empty);
                    Visible = false;                    
                }
                else{                        
                    ApplySearch?.Invoke(this, EventArgs.Empty);
                }

                Owner?.Focus ();
			};

			Input.Dock = Docking.Fill;
			BackColor = Theme.Colors.Base00;
			Padding = new Padding (4);
			CanFocus = true;

			Input.TextChanged += delegate {
				EnableWidgets();
			};

			EnableWidgets ();
		}

		protected void EnableWidgets()
		{
			OkButton.Enabled = !String.IsNullOrEmpty(Input.Text);
		}

		public override SizeF PreferredSize (IGUIContext ctx, SizeF proposedSize)
		{			
			return new SizeF(proposedSize.Width, Input.Font.TextBoxHeight + Padding.Height);
		}

		public override void Focus ()
		{
			Input.TabInto ();
		}
			
		public override bool OnKeyDown (KeyboardKeyEventArgs e)
		{
			switch (e.Key) {
			case Keys.Enter:				
				OkButton.OnClick ();
				return true;
			case Keys.Escape:
				CancelButton.OnClick ();
				return true;
			}
				
			return false;
		}
    }
}