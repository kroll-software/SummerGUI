using System;

namespace SummerGUI
{
	public class TextEditorToolBar : ComponentToolBar
	{
		public MultiLineTextBox Editor { get; set; }

		public TextEditorToolBar (MultiLineTextBox editor, IGuiMenu menu = null)
			: base("editortoolbar", menu)
		{
			Editor = editor;
		}

		protected override void CleanupManagedResources ()
		{
			Editor = null;
			base.CleanupManagedResources ();
		}			
	}
}

