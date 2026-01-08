using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Input;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Drawing;
using KS.Foundation;
using SummerGUI.Editor;
using SummerGUI.Scrolling;

namespace SummerGUI
{
	public class CommandInputBar : Container
	{		
		public CommandInputTextBox Input { get; private set; }
		public Button OkButton { get; private set; }
		public Button CancelButton { get; private set; }

		public MultiLineTextBox Owner { get; private set; }
		public string InfoText
		{
			get {
				return Input.InfoText;
			}
			set {
				Input.InfoText = value;
			}
		}

		public CommandInputBar (string name, MultiLineTextBox owner, string infoText, Docking dock = Docking.Bottom, IWidgetStyle style = null)
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
					Owner.Focus ();
					Visible = false;
				}
			};

			OkButton = AddChild(new Button ("actionbutton", "Find", ColorContexts.Default));
			OkButton.Dock = Docking.Right;
			OkButton.HandlesEnterKey = true;
			OkButton.ApplyCommandButtonPadding ();
			OkButton.MakeDefaultButton ();
			OkButton.Click += delegate {
				if (Owner != null) {
					Owner.Focus ();
					Owner.FindText (Input.Text, FindDirections.Next);
					Visible = false;
				}
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

