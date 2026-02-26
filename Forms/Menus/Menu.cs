using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Input;
using OpenTK.Windowing.GraphicsLibraryFramework;
using KS.Foundation;

namespace SummerGUI
{
	public interface IGuiMenuNode
	{
		IGuiMenu Children { get; }
	}

	public interface IGuiMenu : IList<IGuiMenuItem>
	{
		string Name { get; set; }
		IGuiMenu Children { get; }
		bool Visible { get; set; }
		bool Enabled { get; set; }
		IGuiMenuItem Add (string key, string text, char icon = (char)0);
		IGuiMenuItem Insert (int index, string key, string text, char icon = (char)0);
		IGuiMenuItem FindItem (string name);
		void InitEditMenu();
	}		
		
	public class GuiMenu : List<IGuiMenuItem>, IGuiMenu, IGuiMenuNode
	{
		public string Name { get; set; }
		public IGuiMenu Children 
		{ 
			get {
				return this;
			}
		}

		public bool Visible { get; set; }
		public bool Enabled { get; set; }

		public GuiMenu (string name)
		{
			Name = name;
			Enabled = true;
		}

		public void InitEditMenu()
		{
			IGuiMenuItem mnuEdit = Add ("Edit", "&Edit");
			IGuiMenuItem mnuCopy = mnuEdit.AddChild ("Copy", "Copy", (char)FontAwesomeIcons.fa_copy)
				.SetHotKey(KeyModifiers.Control, Keys.C);
			IGuiMenuItem mnuPaste = mnuEdit.AddChild ("Paste", "Paste", (char)FontAwesomeIcons.fa_paste)
				.SetHotKey(KeyModifiers.Control, Keys.V);
			IGuiMenuItem mnuCut = mnuEdit.AddChild ("Cut", "Cut", (char)FontAwesomeIcons.fa_cut)
				.SetHotKey(KeyModifiers.Control, Keys.X);
			mnuEdit.AddSeparator ();
			IGuiMenuItem mnuUndo = mnuEdit.AddChild ("Undo", "Undo", (char)FontAwesomeIcons.fa_undo)
				.SetHotKey(KeyModifiers.Control, Keys.Z);
			IGuiMenuItem mnuRedo = mnuEdit.AddChild ("Redo", "Redo", (char)FontAwesomeIcons.fa_repeat)
				.SetHotKey(KeyModifiers.Control, Keys.Y);
			mnuEdit.AddSeparator ();
			IGuiMenuItem mnuSelectAll = mnuEdit.AddChild ("SelectAll", "Select All", (char)0)
				.SetHotKey(KeyModifiers.Control, Keys.A);
			IGuiMenuItem mnuInvertSelection = mnuEdit.AddChild ("InvertSelection", "Invert Selection", (char)0)
				.SetHotKey(KeyModifiers.Control, Keys.I);

			// Test: Disable something
			mnuCopy.Enabled = false;
			mnuRedo.Enabled = false;
			mnuCut.Enabled = false;
		}

		public IGuiMenuItem Add (string key, string text, char icon = (char)0) 
		{
			IGuiMenuItem item = new GuiMenuItem (key, text, icon);
			this.Add (item);
			return item;
		}

		public IGuiMenuItem Insert (int index, string key, string text, char icon = (char)0) 
		{
			IGuiMenuItem item = new GuiMenuItem (key, text, icon);
			this.Insert (index, item);
			return item;
		}

		public IGuiMenuItem FindItem (string name)
		{						
			if (name == null || Children == null)
				return null;
			IGuiMenuItem found = null;
			for (int i = 0; i < Children.Count; i++) {
				IGuiMenuItem child = Children [i];
				if (child != null) {
					found = child.FindItem (name);
					if (found != null)
						return found;
				}
			}
			return null;
		}
	}	

	public static class GuiMenuItemExtensions
	{				
		public static IEnumerable<IGuiMenuItem> Items (this IGuiMenu menu)
		{			
			return menu.Children.SelectMany(me => me.EnumerateMenus());
		}

		// this is so fast, can be used with a million items..
		static IEnumerable<IGuiMenuItem> EnumerateMenus(this IGuiMenuItem root)
		{	
			var queue = new Queue<IGuiMenuItem>();
			queue.Enqueue(root);
			while(queue.Any())
			{
				var w = queue.Dequeue();
				yield return w;
				w.Children.ForEach (queue.Enqueue);
			}
		}

		public static void CollapseAll (this IGuiMenu menu)
		{			
			foreach (var child in menu)
				child.CollapseAll ();			
		}

		public static void ExpandAll (this IGuiMenu menu)
		{			
			foreach (var child in menu)
				child.ExpandAll ();
		}

		public static int ExpandedItemsWithoutSeparators (this IGuiMenu menu)
		{		
			var list = menu.Children.Where (c => !c.IsSeparator && (c.Parent == null || !c.Parent.Collapsed)).ToList ();
			return list.Count + list.Sum (c => c.Children.ExpandedItemsWithoutSeparators ());
		}
	}
}

