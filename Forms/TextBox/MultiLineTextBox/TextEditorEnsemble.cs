using System;
using System.Drawing;
using System.Linq;
using KS.Foundation;
using SummerGUI.Editor;

namespace SummerGUI
{
	public interface ISupportsFindCall
    {
        public void Find();
    }

	public class TextEditorEnsemble : Container, ISupportsFindCall
	{
		public TextEditorToolBar Tools { get; private set; }
		public TextEditorRowColumn RowColumn { get; private set; }
		public MultiLineTextBox Editor { get; private set; }

		public CommandInputBar CommandInput { get; private set; } 

		public string Text
		{
			get{
				return Editor.Text;
			}
			set{
				Editor.Text = value;
			}
		}

        public string FilePath { get; set; }

		public TextEditorEnsemble (string name)
			: base(name, Docking.Fill, new EmptyWidgetStyle())
		{			
			Editor = new MultiLineTextBox("editor", new MultiLineTextEditWidgetStyle());
			//Tools = new TextEditorToolBar (Editor, null);
			RowColumn = new TextEditorRowColumn (Editor);

			//Children.Add (Tools);
			Children.Add (RowColumn);
			Children.Add (Editor);
			CommandInput = AddChild (new CommandInputBar ("commandinput", Editor, "Search for > "));
			CommandInput.Visible = false;
			CanFocus = true;
		}
			
		public override void Focus ()
		{
			// forward the focus
			Editor.Focus ();
		}

		public void TabInto()
		{	
			Editor.Focus ();
			Editor.SelectAll ();
		}

		protected override void LayoutChild (IGUIContext ctx, Widget child, RectangleF bounds)
		{
			child.OnLayout (ctx, bounds);
		}

		float lastTotalHeight = 0;
		protected override void LayoutChildren (IGUIContext ctx, RectangleF bounds)
		{
			base.LayoutChildren (ctx, bounds);

			float totalHeight = Editor.RowManager.Height + Editor.Padding.Height;
			if (totalHeight != lastTotalHeight) {
				lastTotalHeight = totalHeight;
				RowColumn.Update ();
				//Editor.Update ();
			}				
		}

		public void Find()
		{
			if (!Editor.CanFind)
				return;
			CommandInput.Visible = true;
			CommandInput.Focus ();
		}
	}
}

