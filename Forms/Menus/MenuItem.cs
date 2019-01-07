using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using OpenTK.Input;
using KS.Foundation;

namespace SummerGUI
{
	public interface IGuiMenuItem : IComparable<IGuiMenuItem>
	{				
		string Name { get; set; }
		string Text { get; set; }
		string DisplayString { get; }
		char Mnemonic { get; }
		string Description { get; set; }
		object Tag { get; set; }
		int Rank { get; set; }

		IGuiMenuItem Parent { get; set; }
		IGuiMenu Children { get; }
		int Level { get; set; }
		bool Collapsed { get; set; }
		bool HasChildren { get; }

		bool Enabled { get; set; }
		bool Visible { get; set; }

		bool IsSeparator { get; }
		bool IsToggleButton { get; set; }
		bool IsOptionGroup { get; set; }
		bool IsFireButton { get; set; }
		bool Checked { get; set; }

		string ImageKey { get; set; }
		uint ImageIndex { get; set; }
		bool HasImage { get; }
		bool ShowOnToolBar { get; set; } 

		event EventHandler<EventArgs> Click;
		bool OnClick (bool raisEvent = true);
		int ClickCount { get; set; }

		event EventHandler<EventArgs> CheckedChanged;
		event EventHandler<EventArgs> Expanding;

		Key HotKey { get; set; }
		KeyModifiers ModifierKey { get; set; }

		IGuiMenuItem AddChild (string key, string text, char icon = (char)0);
		IGuiMenuItem InsertChild (int index, string key, string text, char icon = (char)0);
		IGuiMenuItem AddSeparator ();
		IGuiMenuItem AddSeparator (string key);
		IGuiMenuItem InsertSeparator (int index);
		IGuiMenuItem InsertSeparator (int index, string key);

		IGuiMenuItem FindItem (string name);

		bool CanCollapse { get; }
		bool CanExpand { get; }
		void Collapse();
		void Expand();
		void ToggleCollapse ();
	}

	public class GuiMenuItem : IGuiMenuItem, IGuiMenuNode, IComparable<IGuiMenuItem>
	{		
		public string Name { get; set; }
		public string Description { get; set; }
		public string ImageKey { get; set; }
		public uint ImageIndex { get; set; }
		public bool HasImage 
		{ 
			get {
				return ImageIndex > 0 || !String.IsNullOrEmpty (ImageKey);
			}
		}

		public int CompareTo(IGuiMenuItem other)
		{
			return this.ClickCount.CompareTo (other.ClickCount);
		}

		public object Tag { get; set; }
		public int Rank { get; set; }

		public bool Enabled { get; set; }
		public bool Visible { get; set; }
		public bool Selected { get; set; }
		public bool IsToggleButton { get; set; }
		public bool IsOptionGroup { get; set; }
		public bool IsFireButton { get; set; }

		public Key HotKey { get; set; }
		public KeyModifiers ModifierKey { get; set; }
		public char Mnemonic { get; private set; }

		public IGuiMenuItem Parent { get; set; }
		public IGuiMenu Children { get; private set; }
		public bool Collapsed { get; set; }

		public bool HasChildren
		{ 
			get {
				return Children != null && Children.Count > 0;
			}
		}
			
		public IGuiMenuItem FindItem (string name)
		{
			if (Name == name)
				return this;
			if (HasChildren)
				return (Children.FindItem (name));
			return null;
		}

		public bool ShowOnToolBar { get; set; }
		public int ClickCount { get; set; }

		public string DisplayString { get; private set; }

		private string m_Text;
		public string Text {
			get {
				return m_Text;
			}
			set {
				if (m_Text != value) {
					m_Text = value;
					Mnemonic = m_Text.ParseMnemonic ();
					DisplayString = Text.Replace ("&", "");
				}
			}
		}

		public event EventHandler<EventArgs> Click;
		public bool OnClick(bool raisEvent = true)
		{
			if (raisEvent && !Enabled)
				return false;

			if (IsToggleButton) {				
				if (!CanCheck || (IsOptionGroup && Checked))
					return false;
				Checked = !Checked;
				if (IsOptionGroup && Parent != null) {
					int i = Parent.Children.IndexOf (this);
					for (int k = i - 1; k >= 0; k--) {
						IGuiMenuItem item = Parent.Children [k];
						if (item.IsSeparator)
							break;
						if (item.IsToggleButton && item.IsOptionGroup && item.Checked)
							item.Checked = false;						
					}
					for (int k = i + 1; k < Parent.Children.Count; k++) {
						IGuiMenuItem item = Parent.Children [k];
						if (item.IsSeparator)
							break;
						if (item.IsToggleButton && item.IsOptionGroup)
							item.Checked = false;						
					}
				}
			} else if (!CanClick) {
				return false;
			}

			if (raisEvent && Enabled && !HasChildren) {
				ClickCount++;
			}
			if (Click != null && CanClick)
				Click (this, EventArgs.Empty);
			return true;
		}

		public event EventHandler<EventArgs> CheckedChanged;
		private void OnCheckedChanged ()
		{
			if (CheckedChanged != null)
				CheckedChanged (this, EventArgs.Empty);
		}

		private bool m_Checked;
		public bool Checked 
		{ 
			get {
				return m_Checked;
			}
			set {
				if (m_Checked != value) {
					m_Checked = value;
					OnCheckedChanged ();
				}
			}
		}

		static int _rank = 0;
		public GuiMenuItem (string name, string text, char icon = (char)0)
		{
			Name = name;
			Text = text;
			ImageIndex = (uint)icon;
			Children = new GuiMenu("sub");

			Visible = true;
			Enabled = true;
			ShowOnToolBar = true;
			Rank = _rank++;
			//Collapsed = true;
		}	

		~GuiMenuItem()
		{
			Parent = null;
			Tag = null;
		}

		public IGuiMenuItem AddChild (string name, string text, char icon = (char)0)
		{
			GuiMenuItem item = new GuiMenuItem (name, text, icon);
			item.Parent = this;
			Children.Add (item);
			return item;
		}

		public IGuiMenuItem InsertChild (int index, string name, string text, char icon = (char)0)
		{
			GuiMenuItem item = new GuiMenuItem (name, text, icon);
			item.Parent = this;
			Children.Insert (index, item);
			return item;
		}

		private static int SeperatorCount = 0;
		public IGuiMenuItem AddSeparator ()
		{
			unchecked {
				return AddSeparator ("seperator_" + SeperatorCount++);
			}
		}			

		public IGuiMenuItem AddSeparator (string key)
		{
			GuiMenuItem item = new GuiMenuItem (key, "-");
			item.Parent = this;
			Children.Add (item);
			return item;
		}

		public IGuiMenuItem InsertSeparator (int index)
		{
			unchecked {
				return InsertSeparator (index, "seperator_" + SeperatorCount++);
			}
		}

		public IGuiMenuItem InsertSeparator (int index, string key)
		{
			GuiMenuItem item = new GuiMenuItem (key, "-");
			item.Parent = this;
			Children.Insert (index, item);
			return item;
		}

		public bool IsSeparator 
		{ 
			get {
				return Text == "-";
			}
		}

		public bool IsRoot
		{
			get{
				return Parent == null;
			}
		}

		public int Level { get; set; }

		/***
		public int Level 
		{ 
			get {
				int l = 0;
				IGuiMenuItem parent = Parent;
				while (parent != null) {
					l++;
					parent = parent.Parent;
				}
				return l;
			}
		}	
		***/

		public virtual bool CanClick
		{
			get{
				return Visible && Enabled && Children.IsNullOrEmpty();
			}
		}

		public virtual bool CanCheck
		{
			get{
				return Visible && Enabled && IsToggleButton;
			}
		}

		public bool CanCollapse 
		{ 
			get { 
				return Enabled && HasChildren && !Collapsed;
			}
		}

		public bool CanExpand 
		{ 
			get {
				return Enabled && HasChildren && Collapsed;
			}
		}

		public void Collapse()
		{
			if (CanCollapse)
				Collapsed = true;
		}


		public event EventHandler<EventArgs> Expanding;
		public void Expand()
		{
			if (CanExpand) {
				if (Expanding != null)
					Expanding (this, EventArgs.Empty);

				Collapsed = false;
				ClickCount++;
			}
		}

		public void ToggleCollapse ()
		{
			if (Collapsed)
				Expand ();
			else
				Collapse ();
		}
			
		public override string ToString ()
		{
			return string.Format ("{0}, {1}", Name, Text);
		}
	}

	public static class IGuiMenuItemExtensions
	{
		public static IGuiMenuItem SetHotKey (this IGuiMenuItem item, OpenTK.Input.KeyModifiers modifierKey, OpenTK.Input.Key hotKey)
		{
			item.ModifierKey = modifierKey;
			item.HotKey = hotKey;
			return item;
		}

		public static IGuiMenuItem SetHotKey (this IGuiMenuItem item, OpenTK.Input.Key hotKey)
		{
			item.ModifierKey = 0;
			item.HotKey = hotKey;
			return item;
		}

		public static IGuiMenuItem SetChecked (this IGuiMenuItem item, bool check = false)
		{
			item.IsToggleButton = true;
			item.Checked = check;
			item.ShowOnToolBar = false;
			return item;
		}

		public static IGuiMenuItem SetFireButton (this IGuiMenuItem item)
		{
			item.IsFireButton = true;
			return item;
		}

		public static IGuiMenuItem ShowOnToolbar (this IGuiMenuItem item)
		{
			item.ShowOnToolBar = true;
			return item;
		}

		public static IGuiMenuItem HideFromToolbar (this IGuiMenuItem item)
		{
			item.ShowOnToolBar = false;
			return item;
		}

		public static bool ProcessInputKey(this IGuiMenuItem item, OpenTK.Input.KeyboardKeyEventArgs e)
		{
			if (!(item.Visible && item.Enabled))
				return false;
			if (item.ModifierKey == e.Modifiers && item.HotKey == e.Key) {
				if (item.OnClick ()) {								
					return true;
				}
			}
			// recurse through all children
			foreach (var child in item.Children)
				if (child.ProcessInputKey (e))
					return true;			
			return false;
		}

		public static void CollapseAll (this IGuiMenuItem item)
		{			
			item.Collapsed = true;
			item.Children.CollapseAll ();
		}

		public static void ExpandAll (this IGuiMenuItem item)
		{
			item.Expand();
			item.Children.ExpandAll ();
		}			

		public static bool IsParentOf (this IGuiMenuItem item, IGuiMenuItem sub)
		{
			IGuiMenuItem p = sub;
			while (p != null && p != item) {
				p = p.Parent;
			}
			return p == item;
		}
	}
}

