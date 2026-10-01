using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using OpenTK;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using KS.Foundation;

namespace SummerGUI
{
	// ====================================================================
	// EventArgs
	// ====================================================================

	public class TreeEventArgs : EventArgs
	{
		public TreeViewItem Node { get; }
		protected TreeEventArgs (TreeViewItem node) { Node = node; }
	}

	public class TreeNodeEventArgs : TreeEventArgs
	{
		public bool IsNowExpanded { get; }
		public TreeNodeEventArgs (TreeViewItem node, bool isNowExpanded)
			: base (node) { IsNowExpanded = isNowExpanded; }
	}

	public class TreeItemMouseEventArgs : TreeEventArgs
	{
		public MouseButton Button { get; }
		public TreeItemMouseEventArgs (TreeViewItem node, MouseButton button)
			: base (node) { Button = button; }
	}

	// ====================================================================
	// TreeViewItem
	// ====================================================================

	/// <summary>
	/// A node in a <see cref="TreeView"/>. Has a text, optional tag, glyph, and a
	/// thread-safe collection of child nodes.
	/// </summary>
	public class TreeViewItem
	{
		string m_Text;
		public string Text
		{
			get { return m_Text; }
			set
			{
				if (m_Text == value)
					return;
				m_Text = value;
				if (m_Owner != null)
					m_Owner.OnTreeInvalidate ();
			}
		}

		/// <summary>Arbitrary user data attached to this node.</summary>
		public object Tag { get; set; }

		/// <summary>
		/// Glyph that identifies this node. For branch nodes use
		/// <c>fa_folder</c>/<c>fa_folder_open</c> and for leaf nodes
		/// <c>fa_file_o</c> (default) or any custom glyph.
		/// </summary>
		public char Glyph { get; set; }

		/// <summary>
		/// Marks a branch folder; controls the default glyph behaviour for this node
		/// (true => <c>fa_folder</c>/<c>fa_folder_open</c>, false => <c>fa_file_o</c>).
		/// </summary>
		public bool IsFolder
		{
			get
			{
				if (Glyph == (char)FontAwesomeIcons.fa_folder || Glyph == (char)FontAwesomeIcons.fa_folder_open)
					return true;
				return m_IsFolder;
			}
			set
			{
				if (value)
				{
					m_IsFolder = true;
					Glyph = (char)FontAwesomeIcons.fa_folder;
				}
				else
				{
					m_IsFolder = false;
					Glyph = (char)FontAwesomeIcons.fa_file_o;
				}
				if (m_Owner != null)
					m_Owner.OnTreeInvalidate ();
			}
		}
		bool m_IsFolder = false;

		TreeViewItem m_Parent;
		public TreeViewItem Parent
		{
			get { return m_Parent; }
			internal set { m_Parent = value; }
		}

		TreeView m_Owner;
		/// <summary>The TreeView this node belongs to, if any.</summary>
		public TreeView Owner
		{
			get { return m_Owner; }
			internal set { m_Owner = value; }
		}

		TreeViewItemCollection m_Children;
		/// <summary>
		/// Child nodes, in insertion order (uses KS.Foundation's <see cref="BinarySortedList{T}"/>
		/// so all list operations are thread-safe). Use <c>Add</c>/<c>AddLast</c> to keep insertion order.
		/// </summary>
		public TreeViewItemCollection Children
		{
			get { return m_Children; }
		}

		public bool HasChildren
		{
			get { return m_Children.Count > 0; }
		}

		/// <summary>Append a child node (insertion order preserved; thread-safe).</summary>
		public TreeViewItem AddChild (TreeViewItem child)
		{
			if (child == null)
				throw new ArgumentNullException ("child");
			child.Parent = this;
			child.Owner = m_Owner;
			m_Children.AddLast (child);
			if (m_Owner != null)
			{
				m_Owner.AssignOwnerToSubtree (child);
				m_Owner.OnTreeStructureChanged ();
			}
			return child;
		}

		/// <summary>Append a child node with the given text/glyph.</summary>
		public TreeViewItem AddChild (string text, object tag = null, char glyph = '\0', bool isFolder = false)
		{
			return AddChild (new TreeViewItem (text, tag, glyph, isFolder));
		}

		/// <summary>Remove a child node (by reference).</summary>
		public void Remove (TreeViewItem child)
		{
			if (child == null)
				return;
			int idx = -1;
			for (int i = 0; i < m_Children.Count; i++)
			{
				if (ReferenceEquals (m_Children.UnlockedItemByIndex (i), child))
				{
					idx = i;
					break;
				}
			}
			if (idx < 0)
				return;
			m_Children.RemoveAt (idx);
			child.Parent = null;
			child.Owner = null;
			if (m_Owner != null)
				m_Owner.OnTreeStructureChanged ();
		}

		/// <summary>
		/// Depth in the tree (0 = root). Cached by the TreeView when the tree is flattened.
		/// </summary>
		public int Depth { get; internal set; }

		bool m_IsExpanded = true;
		/// <summary>
		/// True when child nodes are expanded and visible (only meaningful when <see cref="HasChildren"/>).
		/// Changing this value fires <see cref="TreeView.NodeExpandStateChanged"/> and causes the
		/// owning TreeView to re-layout.
		/// </summary>
		public bool IsExpanded
		{
			get { return m_IsExpanded; }
			set
			{
				if (m_IsExpanded == value)
					return;
				m_IsExpanded = value;
				if (m_Owner != null)
					m_Owner.OnNodeExpandChanged (this, value);
			}
		}

		public TreeViewItem (string text, object tag = null, char glyph = '\0', bool isFolder = false)
		{
			m_Text = text ?? String.Empty;
			Tag = tag;
			m_Children = new TreeViewItemCollection ();
			m_IsFolder = isFolder;
			Glyph = (glyph != 0) ? glyph : (isFolder ? (char)FontAwesomeIcons.fa_folder : (char)FontAwesomeIcons.fa_file_o);
		}

		internal void ResetExpanded (bool expanded)
		{
			m_IsExpanded = expanded;
		}

		public override string ToString ()
		{
			return Text;
		}
	}

	// ====================================================================
	// TreeViewItemCollection
	// ====================================================================

	/// <summary>
	/// Thread-safe collection of <see cref="TreeViewItem"/> nodes for a single level of a <see cref="TreeView"/>.
	/// Backed by KS.Foundation's <see cref="BinarySortedList{T}"/> so that all read/write operations
	/// are thread-safe. Use <c>Add</c> or <c>AddLast</c> to append a new node (preserving insertion order).
	/// </summary>
	public class TreeViewItemCollection : BinarySortedList<TreeViewItem>
	{
		public new void Add (TreeViewItem node)
		{
			AddLast (node);
		}

		public TreeViewItem Add (string text, object tag = null, char glyph = '\0', bool isFolder = false)
		{
			var p = new TreeViewItem (text, tag, glyph, isFolder);
			AddLast (p);
			return p;
		}
	}

	// ====================================================================
	// TreeView
	// ====================================================================

	/// <summary>
	/// A hierarchical node list with expand/collapse support, FontAwesome glyphs, vertical and horizontal
	/// scrolling, and full keyboard navigation.
	/// </summary>
	/// <remarks>
	/// Mirrors WinForms TreeView:
	/// <list type="bullet">
	///   <item>Select with left mouse click or Up/Down/Home/End/PageUp/PageDown.</item>
	///   <item>Expand/collapse a node with Left/Right, Space, Enter, or by clicking its [+/-] indicator.</item>
	///   <item>Double-click or Enter activates (fires <see cref="ItemDoubleClicked"/>) — WinForms-Parität, wie im <see cref="ListView"/> ItemActivated.</item>
	///   <item>Right-click shows a context menu with "Expand All" and "Collapse All".</item>
	///   <item>Delete removes the selected node (if a parent exists). Insert adds a child under the selection.</item>
	///   <item>Mouse wheel scrolls; arrow keys navigate.</item>
	/// </list>
	/// </remarks>
	public class TreeView : ScrollableContainer
	{
		#region Styles & Metrics

		TreeViewSelectedStyle m_SelectedActive;
		WidgetStyle m_SelectedInactive;

		public TreeViewSelectedStyle SelectedStyleActive
		{
			get { return m_SelectedActive; }
		}
		public WidgetStyle SelectedStyleInactive
		{
			get { return m_SelectedInactive; }
		}

		IGUIFont m_Font;
		/// <summary>Text font used to render node labels.</summary>
		public IGUIFont Font
		{
			get { return m_Font; }
			set
			{
				if (m_Font == value)
					return;
				m_Font = value;
				OnTreeInvalidate ();
			}
		}

		IGUIFont m_IconFont;
		/// <summary>Icon font for FontAwesome glyphs (+/-, folder, file). Defaults to FontManager's SmallIcons.</summary>
		public IGUIFont IconFont
		{
			get { return m_IconFont; }
			set
			{
				if (m_IconFont == value)
					return;
				m_IconFont = value;
				OnTreeInvalidate ();
			}
		}

		[DpiScalable]
		public float Indent { get; set; }

		[DpiScalable]
		public float IndicatorWidth
		{
			get; set;
		}

		[DpiScalable]
		public float GlyphWidth
		{
			get; set;
		}

		[DpiScalable]
		public float MarginLeft
		{
			get; set;
		}

		float m_LineHeight;
		float LineHeight
		{
			get { return (m_Font != null && m_Font.LineHeight > 0) ? m_Font.LineHeight : 16f; }
		}

		#endregion

		#region Item storage

		TreeViewItemCollection m_Items;
		/// <summary>
		/// Top-level nodes. Add nodes with <c>Add</c> or <c>AddLast</c> to keep insertion order;
		/// all reads and writes are thread-safe.
		/// </summary>
		public TreeViewItemCollection Items
		{
			get { return m_Items; }
		}

		public int Count
		{
			get { return m_Items.Count; }
		}

		TreeViewItem m_SelectedItem;
		/// <summary>Selected node (null when nothing is selected).</summary>
		public TreeViewItem SelectedItem
		{
			get { return m_SelectedItem; }
			set
			{
				if (ReferenceEquals (m_SelectedItem, value))
					return;
				TreeViewItem old = m_SelectedItem;
				m_SelectedItem = value;
				OnSelectionChanged (new TreeItemMouseEventArgs (m_SelectedItem, MouseButton.Left));
			}
		}

		#endregion

		#region Events

		public event EventHandler<TreeItemMouseEventArgs> SelectionChanged;
		public event EventHandler<TreeItemMouseEventArgs> ItemClicked;
		public event EventHandler<TreeItemMouseEventArgs> ItemDoubleClicked;
		public event EventHandler<TreeNodeEventArgs> NodeExpandStateChanged;

		protected virtual void OnSelectionChanged (TreeItemMouseEventArgs e)
		{
			if (SelectionChanged != null && !IsDisposed)
				SelectionChanged (this, e);
		}

		protected virtual void OnItemClicked (TreeItemMouseEventArgs e)
		{
			if (ItemClicked != null && !IsDisposed)
				ItemClicked (this, e);
		}

		protected virtual void OnItemDoubleClicked (TreeItemMouseEventArgs e)
		{
			if (ItemDoubleClicked != null && !IsDisposed)
				ItemDoubleClicked (this, e);
		}

		protected virtual void OnNodeExpandStateChanged (TreeNodeEventArgs e)
		{
			if (NodeExpandStateChanged != null && !IsDisposed)
				NodeExpandStateChanged (this, e);
		}

		#endregion

		#region Public API

		public TreeViewItem Add (string text, object tag = null, char glyph = '\0', bool isFolder = false)
		{
			var n = new TreeViewItem (text, tag, glyph, isFolder) { Owner = this };
			m_Items.AddLast (n);
			AssignOwnerToSubtree (n);
			OnTreeStructureChanged ();
			return n;
		}

		public TreeViewItem Add (TreeViewItem node)
		{
			if (node == null)
				throw new ArgumentNullException ("node");
			node.Owner = this;
			node.Parent = null;
			AssignOwnerToSubtree (node);
			m_Items.AddLast (node);
			OnTreeStructureChanged ();
			return node;
		}

		public new void Clear ()
		{
			Items.Clear ();
			m_FlatRows.Clear ();
			if (m_SelectedItem != null)
			{
				TreeViewItem old = m_SelectedItem;
				m_SelectedItem = null;
				OnSelectionChanged (new TreeItemMouseEventArgs (null, MouseButton.Left));
			}
			OnTreeStructureChanged ();
		}

		public void ExpandAll ()
		{
			ExpandRec (m_Items, true);
			OnTreeStructureChanged ();
		}

		public void CollapseAll ()
		{
			ExpandRec (m_Items, false);
			OnTreeStructureChanged ();
		}

		static void ExpandRec (TreeViewItemCollection coll, bool expanded)
		{
			for (int i = 0; i < coll.Count; i++)
			{
				var n = coll.UnlockedItemByIndex (i);
				if (n == null)
					continue;
				if (n.HasChildren)
					n.ResetExpanded (expanded);
				ExpandRec (n.Children, expanded);
			}
		}

		/// <summary>
		/// Make sure a node is visible by scrolling (H and V) to it.
		/// </summary>
		public void EnsureNodeVisible (TreeViewItem node)
		{
			if (node == null)
				return;
			int row = FindRow (node);
			if (row < 0)
				return;

			float lh = LineHeight;
			float top = row * lh;
			float bottom = top + lh;

			float viewportH = Math.Max (1f, Bounds.Height - (HScrollBar != null && HScrollBar.IsVisibleEnabled ? HScrollBar.Height : 0));
			if (VScrollBar != null && VScrollBar.IsVisibleEnabled)
			{
				if (top < VScrollBar.Value)
					VScrollBar.Value = Math.Max (0, top);
				else if (bottom > VScrollBar.Value + viewportH)
					VScrollBar.Value = Math.Max (0, bottom - viewportH);
			}

			float viewportW = Math.Max (1f, Bounds.Width - (VScrollBar != null && VScrollBar.IsVisibleEnabled ? VScrollBar.Width : 0));
			if (HScrollBar != null && HScrollBar.IsVisibleEnabled)
			{
				float neededRight = MarginLeft + node.Depth * Indent + (node.HasChildren ? IndicatorWidth : 0) + GlyphWidth + 4f + Font.Measure (node.Text).Width;
				if (neededRight > HScrollBar.Value + viewportW)
					HScrollBar.Value = Math.Max (0, neededRight - viewportW);
			}

			Invalidate ();
		}

		#endregion

		#region Owner bookkeeping (internal)

		internal void AssignOwnerToSubtree (TreeViewItem root)
		{
			if (root == null)
				return;
			root.Owner = this;
			for (int i = 0; i < root.Children.Count; i++)
				AssignOwnerToSubtree (root.Children.UnlockedItemByIndex (i));
		}

		internal void OnNodeExpandChanged (TreeViewItem node, bool nowExpanded)
		{
			OnNodeExpandStateChanged (new TreeNodeEventArgs (node, nowExpanded));
			OnTreeStructureChanged ();
		}

		internal void OnTreeInvalidate ()
		{
			Invalidate ();
		}

		internal void OnTreeStructureChanged ()
		{
			RebuildFlatRows ();

			// After a structural change (Expand/Collapse/Add/Remove/All) the row count and
			// therefore the scrollable extent can shrink dramatically (e.g. Collapse All →
			// FlatRowCount==1). ScrollableContainer does NOT run SetUpScrollbars for a custom
			// widget like this one, so the VScrollBar.Value is NOT clamped automatically. Clamp
			// it here so the view never ends up scrolled past the (now tiny) document — which
			// would paint everything off-screen and leave the widget looking empty/blank.
			float rowH = LineHeight;
			float docH = rowH * Math.Max (1, FlatRowCount);
			float viewportH = Math.Max (1f, Bounds.Height - (HScrollBar != null && HScrollBar.IsVisibleEnabled ? HScrollBar.Height : 0));
			float maxScroll = Math.Min (docH, Math.Max (0f, docH - viewportH));
			if (VScrollBar != null && VScrollBar.IsVisibleEnabled && VScrollBar.Value > maxScroll)
				VScrollBar.Value = Math.Max (0f, maxScroll);
			if (HScrollBar != null && HScrollBar.IsVisibleEnabled)
			{
				float docW = Math.Max (0f, MaxRowRight + 4f);
				float viewportW = Math.Max (1f, Bounds.Width - (VScrollBar != null && VScrollBar.IsVisibleEnabled ? VScrollBar.Width : 0));
				float maxH = Math.Max (0f, docW - viewportW);
				if (HScrollBar.Value > maxH)
					HScrollBar.Value = Math.Max (0f, maxH);
			}

			// Keep the selected row visible, honouring the clamp above.
			EnsureNodeVisible (m_SelectedItem);

			ResetCachedLayout ();
			if (Parent != null)
				Parent.Invalidate ();
			Invalidate ();
		}

		#endregion

		#region Flat-row cache

		class FlatRow
		{
			public TreeViewItem Node;
			public int Depth;
			public bool IsLastInBranch;
		}

		List<FlatRow> m_FlatRows = new List<FlatRow> ();
		int FlatRowCount { get { return m_FlatRows.Count; } }

		void RebuildFlatRows ()
		{
			m_FlatRows.Clear ();
			Flatten (m_Items, 0);
		}

		void Flatten (TreeViewItemCollection coll, int depth)
		{
			int n = coll.Count;
			for (int i = 0; i < n; i++)
			{
				var node = coll.UnlockedItemByIndex (i);
				if (node == null)
					continue;
				node.Depth = depth;
				bool last = (i == n - 1);
				m_FlatRows.Add (new FlatRow { Node = node, Depth = depth, IsLastInBranch = last });
				if (node.HasChildren && node.IsExpanded)
					Flatten (node.Children, depth + 1);
			}
		}

		int FindRow (TreeViewItem node)
		{
			for (int i = 0; i < m_FlatRows.Count; i++)
				if (ReferenceEquals (m_FlatRows [i].Node, node))
					return i;
			return -1;
		}

		float MaxRowRight
		{
			get
			{
				float w = 0;
				for (int i = 0; i < m_FlatRows.Count; i++)
				{
					var r = RowRight (m_FlatRows [i]);
					if (r > w)
						w = r;
				}
				return w;
			}
		}

		float RowRight (FlatRow r)
		{
			var n = r.Node;
			float w = MarginLeft + r.Depth * Indent;
			if (n.HasChildren)
				w += IndicatorWidth + 2f;
			w += GlyphWidth + 2f;
			if (!string.IsNullOrEmpty (n.Text) && m_Font != null)
				w += m_Font.Measure (n.Text).Width;
			return w;
		}

		#endregion

		#region Constructor

		public TreeView (string name)
			: this (name, Docking.Fill)
		{
		}

		public TreeView (string name, Docking dock)
			: this (name, dock, new TreeViewStyle ())
		{
		}

		public TreeView (string name, Docking dock, IWidgetStyle style)
			: base (name, dock, style)
		{
			Styles.SetStyle (new TreeViewStyle (), WidgetStates.Default);

			m_Font = FontManager.Manager.DefaultFont;
			m_IconFont = FontManager.Manager.SmallIcons;
			Indent = 18f;
			IndicatorWidth = 18f;
			GlyphWidth = 18f;
			MarginLeft = 4f;

			m_Items = new TreeViewItemCollection ();
			m_FlatRows = new List<FlatRow> ();

			ScrollBars = ScrollBars.Both;
			AutoScroll = true;
			CanFocus = true;
			TabStop = true;

			m_SelectedActive = new TreeViewSelectedStyle ();
			m_SelectedInactive = new TreeViewSelectedStyleInactive ();

			// Default-Kontextmenü (WinForms-Parität, wie <see cref="ListView"/>):
			// "Expand All" + "Collapse All" — wird von RootContainer.OnMouseUp (Right)
			// über MenuManager.GetContextMenuForWidget → SubMenuOverlay angezeigt.
			BuildContextMenu ();

			OnTreeStructureChanged ();
		}

		/// <summary>
		/// Legt das Standard-Kontextmenü an: "Expand All" und "Collapse All".
		/// Kann vom Anwender überschrieben werden, indem
		/// <see cref="Widget.ContextMenu"/> vor der Anzeige neu zugewiesen wird —
		/// die hier erzeugten Items bleiben dann aber in diesem <see cref="TreeView"/>
		/// enthalten (und ihre Click-Handler rufen die Public-API <see cref="ExpandAll"/> /
		/// <see cref="CollapseAll"/> auf, die idempotent sind).
		/// </summary>
		private void BuildContextMenu()
		{
			GuiMenu m = new GuiMenu ("treecontext");
			m.Add ("ExpandAll",  "Expand All",  (char)FontAwesomeIcons.fa_expand).Click
				+= delegate { ExpandAll (); };
			m.Add ("CollapseAll","Collapse All",(char)FontAwesomeIcons.fa_compress).Click
				+= delegate { CollapseAll (); };
			ContextMenu = m;
		}

		#endregion

		#region Layout

		protected override void LayoutChildren (IGUIContext ctx, RectangleF bounds)
		{
			base.LayoutChildren (ctx, bounds);

			float sbw = (VScrollBar != null && VScrollBar.IsVisibleEnabled) ? VScrollBar.Width : 0;
			float sbh = (HScrollBar != null && HScrollBar.IsVisibleEnabled) ? HScrollBar.Height : 0;

			float docW = Math.Max (bounds.Width - sbw, MaxRowRight + 4f);
			float docH = Math.Max (bounds.Height - sbh, LineHeight * Math.Max (1, FlatRowCount));

			DocumentSize = new SizeF (docW, docH);
		}

		#endregion

		#region Hit-testing

		int HitTestRow (float clientX, float clientY)
		{
			float localX = clientX - Bounds.Left;
			float localY = clientY - Bounds.Top;
			float sbh = (HScrollBar != null && HScrollBar.IsVisibleEnabled) ? HScrollBar.Height : 0;
			float sbw = (VScrollBar != null && VScrollBar.IsVisibleEnabled) ? VScrollBar.Width : 0;

			if (localY < 0 || localY > (Bounds.Height - sbh - 1f))
				return -1;
			if (localX > (Bounds.Width - sbw))
				return -1;

			localY += (VScrollBar != null) ? VScrollBar.Value : 0;

			if (LineHeight <= 0)
				return -1;
			int row = (int)(localY / LineHeight);
			if (row < 0 || row >= FlatRowCount)
				return -1;
			return row;
		}

		bool IsOnIndicator (float clientX, FlatRow row)
		{
			float localX = clientX - Bounds.Left;
			float indX0 = MarginLeft + row.Depth * Indent;
			float indX1 = indX0 + IndicatorWidth;
			return localX >= indX0 && localX < indX1;
		}

		#endregion

		#region Mouse

		public override void OnMouseDown (MouseButtonEventArgs e)
		{
			if (e.Button != MouseButton.Left)
				return;

			Focus ();

			int row = HitTestRow (e.X, e.Y);
			if (row < 0)
			{
				if (m_SelectedItem != null)
					SelectedItem = null;
				return;
			}

			var r = m_FlatRows [row];
			if (r.Node.HasChildren && IsOnIndicator (e.X, r))
			{
				r.Node.IsExpanded = !r.Node.IsExpanded;
				return;
			}

			TreeViewItem old = m_SelectedItem;
			m_SelectedItem = r.Node;
			OnSelectionChanged (new TreeItemMouseEventArgs (m_SelectedItem, e.Button));
			OnItemClicked (new TreeItemMouseEventArgs (m_SelectedItem, e.Button));
			EnsureNodeVisible (r.Node);
		}

		public override void OnDoubleClick (MouseButtonEventArgs e)
		{
			base.OnDoubleClick (e);

			int row = HitTestRow (e.X, e.Y);
			if (row < 0)
				return;

			var r = m_FlatRows [row];

			// Doppelklick auf den +/--Indikator ist reine Expand/Collapse-Operation und MUSS
			// keine Aktivierung (ItemDoubleClicked / ItemActivated) auslösen — sonst würde
			// jeder Doppelklick auf den Aufklapp-Pfeil neben dem Toggle auch ein "Item
			// wurde aktiviert"-Event auslösen. (OnMouseDown-Indikator-Pfad gibt bereits early
			// return; hier gilt das Gleiche für OnDoubleClick.)
			if (r.Node.HasChildren && IsOnIndicator (e.X, r))
				return;

			// Doppelklick auf das Item selbst aktiviert es (WinForms-Parität, wie in <see
			// cref="ListView"/> ItemActivated). Der Zustand des Knotens ändert sich NICHT —
			// Expand/Collapse bleibt dem User (+/--Indikator, Tasten, Kontextmenü) überlassen.
			OnItemDoubleClicked (new TreeItemMouseEventArgs (r.Node, e.Button));
		}

		#endregion

		#region Keyboard

		public override bool OnKeyDown (KeyboardKeyEventArgs e)
		{
			if (!IsFocused || FlatRowCount == 0)
				return base.OnKeyDown (e);

			int cur = (m_SelectedItem != null) ? FindRow (m_SelectedItem) : -1;

			switch (e.Key)
			{
				case Keys.Up:
					{
						int idx = (cur < 0) ? 0 : cur - 1;
						if (idx < 0) idx = 0;
						SelectedItem = m_FlatRows [idx].Node;
						EnsureNodeVisible (m_FlatRows [idx].Node);
						return true;
					}

				case Keys.Down:
					{
						int idx = (cur < 0) ? 0 : cur + 1;
						if (idx >= FlatRowCount) idx = FlatRowCount - 1;
						SelectedItem = m_FlatRows [idx].Node;
						EnsureNodeVisible (m_FlatRows [idx].Node);
						return true;
					}

				case Keys.Home:
					SelectedItem = m_FlatRows [0].Node;
					EnsureNodeVisible (m_FlatRows [0].Node);
					return true;

				case Keys.End:
					SelectedItem = m_FlatRows [FlatRowCount - 1].Node;
					EnsureNodeVisible (m_FlatRows [FlatRowCount - 1].Node);
					return true;

				case Keys.PageUp:
					{
						int lines = Math.Max (1, (int)Math.Round (Bounds.Height / LineHeight));
						int idx = Math.Max (0, cur - lines);
						SelectedItem = m_FlatRows [idx].Node;
						EnsureNodeVisible (m_FlatRows [idx].Node);
						return true;
					}

				case Keys.PageDown:
					{
						int lines = Math.Max (1, (int)Math.Round (Bounds.Height / LineHeight));
						int idx = Math.Min (FlatRowCount - 1, cur + lines);
						SelectedItem = m_FlatRows [idx].Node;
						EnsureNodeVisible (m_FlatRows [idx].Node);
						return true;
					}

				case Keys.Left:
					{
						var n = m_SelectedItem;
						if (n == null) { return true; }
						if (n.HasChildren && n.IsExpanded)
							n.IsExpanded = false;
						else if (n.Parent != null)
							SelectedItem = n.Parent;
						EnsureNodeVisible (SelectedItem);
						return true;
					}

				case Keys.Right:
					{
						var n = m_SelectedItem;
						if (n == null) { return true; }
						if (n.HasChildren)
						{
							if (!n.IsExpanded)
								n.IsExpanded = true;
							else
							{
								var first = n.Children.UnlockedItemByIndex (0);
								if (first != null)
									SelectedItem = first;
							}
						}
						EnsureNodeVisible (SelectedItem);
						return true;
					}

				case Keys.Space:
					{
						var n = m_SelectedItem;
						if (n != null && n.HasChildren)
							n.IsExpanded = !n.IsExpanded;
						EnsureNodeVisible (SelectedItem);
						return true;
					}

				case Keys.Enter:
					{
						var n = m_SelectedItem;
						if (n != null)
							OnItemDoubleClicked (new TreeItemMouseEventArgs (n, MouseButton.Left));
						return true;
					}

				case Keys.Delete:
					{
						var n = m_SelectedItem;
						if (n == null)
							return true;

						TreeViewItem parent = n.Parent;
						if (parent != null)
						{
							int idx = -1;
							for (int i = 0; i < parent.Children.Count; i++)
								if (ReferenceEquals (parent.Children.UnlockedItemByIndex (i), n))
								{
									idx = i;
									break;
								}
							if (idx >= 0)
								parent.Children.RemoveAt (idx);
						}
						else
						{
							int idx = -1;
							for (int i = 0; i < m_Items.Count; i++)
								if (ReferenceEquals (m_Items.UnlockedItemByIndex (i), n))
								{
									idx = i;
									break;
								}
							if (idx >= 0)
								m_Items.RemoveAt (idx);
						}

						m_SelectedItem = (n.Parent != null) ? n.Parent : null;
						OnTreeStructureChanged ();
						return true;
					}

				case Keys.Insert:
					{
						var n = m_SelectedItem;
						TreeViewItem target = (n == null) ? null : n;
						string label = "New Node";

						TreeViewItem child;
						if (target == null)
						{
							child = new TreeViewItem (label) { Owner = this };
							m_Items.AddLast (child);
						}
						else if (target.HasChildren || !target.IsFolder)
						{
							child = new TreeViewItem (label) { Owner = this, Parent = target };
							target.IsFolder = true;
							target.Children.AddLast (child);
							target.IsExpanded = true;
						}
						else
						{
							child = new TreeViewItem (label) { Owner = this, Parent = target };
							target.Children.AddLast (child);
							target.IsExpanded = true;
						}

						AssignOwnerToSubtree (child);
						SelectedItem = child;
						OnTreeStructureChanged ();
						EnsureNodeVisible (child);
						return true;
					}
			}

			return base.OnKeyDown (e);
		}

		#endregion

		#region Painting

		public override void OnPaintBackground (IGUIContext ctx, RectangleF bounds)
		{
			if (FlatRowCount == 0)
				return;

			base.OnPaintBackground (ctx, bounds);

			float sbw = (VScrollBar != null && VScrollBar.IsVisibleEnabled) ? VScrollBar.Width : 0;
			float sbh = (HScrollBar != null && HScrollBar.IsVisibleEnabled) ? HScrollBar.Height : 0;

			float clipW = Math.Max (0, bounds.Width - sbw);
			float clipH = Math.Max (0, bounds.Height - sbh);
			if (clipW <= 0 || clipH <= 0)
				return;

			using (var clip = new ClipBoundClip (ctx, new RectangleF (bounds.Left, bounds.Top, clipW, clipH), true))
			{
				float scrollX = (HScrollBar != null) ? HScrollBar.Value : 0;
				float scrollY = (VScrollBar != null) ? VScrollBar.Value : 0;
				float lh = LineHeight;

				int firstRow = (int)Math.Floor (scrollY / lh);
				int lastRow = (int)Math.Ceiling ((scrollY + clipH) / lh) - 1;
				firstRow = Math.Max (0, Math.Min (firstRow, FlatRowCount - 1));
				lastRow = Math.Max (firstRow, Math.Min (lastRow, FlatRowCount - 1));

				var dotPenNormal = new Pen (Style.ForeColorPen.Color, 1f, DashStyle.Dot);
				var dotPenSelected = new Pen (m_SelectedActive.ForeColorPen.Color, 1f, DashStyle.Dot);

				char chPlus = (char)FontAwesomeIcons.fa_plus;
				char chMinus = (char)FontAwesomeIcons.fa_minus;
				char chFolder = (char)FontAwesomeIcons.fa_folder;
				char chFolderOpen = (char)FontAwesomeIcons.fa_folder_open;

				Brush glyphBrushActive = m_SelectedActive.ForeColorBrush;
				Brush glyphBrushNormal = Style.ForeColorBrush;
				Brush textBrushActive = m_SelectedActive.ForeColorBrush;
				Brush textBrushNormal = Style.ForeColorBrush;

				for (int i = firstRow; i <= lastRow; i++)
				{
					var r = m_FlatRows [i];
					var node = r.Node;
					bool isSelected = ReferenceEquals (node, m_SelectedItem);

					float rowTop = bounds.Top + (i * lh) - scrollY;
					float rowBottom = rowTop + lh;
					float rowX = bounds.Left;

					// Row background when selected
					if (isSelected)
					{
						var selStyle = IsFocused ? m_SelectedActive : m_SelectedInactive;
						ctx.FillRectangle (selStyle.BackColorBrush,
							new RectangleF (rowX, rowTop, clipW, lh));
					}

					var dotPen = isSelected ? dotPenSelected : dotPenNormal;

					float baseX = rowX + MarginLeft;
					float rowCenterY = rowTop + lh / 2f;

					// Vertical dotted tree lines for ancestor levels
					for (int d = 0; d < node.Depth; d++)
					{
						float xl = baseX + d * Indent + IndicatorWidth / 2f;
						// The line continues up to the previous row to be visually connected with the parent's indicator area.
						ctx.DrawLine (dotPen, xl, rowTop, xl, rowBottom);
					}

					// Horizontal connector from the last ancestor's vertical line to this row's indicator area
					float connectorStart;
					float connectorEnd;
					if (node.Depth > 0)
					{
						connectorStart = baseX + (node.Depth - 1) * Indent + IndicatorWidth / 2f;
						connectorEnd = baseX + node.Depth * Indent + IndicatorWidth / 2f;
						ctx.DrawLine (dotPen, connectorStart, rowCenterY, connectorEnd, rowCenterY);
					}
					else
					{
						connectorStart = baseX;
						connectorEnd = baseX + IndicatorWidth / 2f;
					}

					float iconX = baseX + node.Depth * Indent;

					if (node.HasChildren)
					{
						// Vertical line at this row's own indicator level (so children's vertical lines continue)
						ctx.DrawLine (dotPen,
							iconX + IndicatorWidth / 2f,
							rowTop,
							iconX + IndicatorWidth / 2f,
							rowBottom);

						char c = node.IsExpanded ? chMinus : chPlus;
						RectangleF indRect = new RectangleF (iconX, rowTop, IndicatorWidth, lh);
						ctx.DrawString (c.ToString (), m_IconFont,
							isSelected ? glyphBrushActive : glyphBrushNormal,
							indRect, FontFormat.DefaultIconFontFormatCenter);

						// Folder / folder-open glyph in the next cell.
						char g = node.IsFolder ? (node.IsExpanded ? chFolderOpen : chFolder)
							: (node.Glyph == 0 || (int)node.Glyph < 0x20 ? chFolder : node.Glyph);
						RectangleF glyphRect = new RectangleF (iconX + IndicatorWidth + 2f, rowTop, GlyphWidth, lh);
						ctx.DrawString (g.ToString (), m_IconFont,
							isSelected ? glyphBrushActive : glyphBrushNormal,
							glyphRect, FontFormat.DefaultIconFontFormatCenter);
					}
					else
					{
						char g = node.Glyph == 0 ? (char)FontAwesomeIcons.fa_file_o : node.Glyph;
						RectangleF glyphRect = new RectangleF (iconX, rowTop, GlyphWidth, lh);
						ctx.DrawString (g.ToString (), m_IconFont,
							isSelected ? glyphBrushActive : glyphBrushNormal,
							glyphRect, FontFormat.DefaultIconFontFormatCenter);
					}

					// Node text (drawn after the icon)
					if (!string.IsNullOrEmpty (node.Text))
					{
						float textX = iconX + (node.HasChildren ? (IndicatorWidth + 2f) : 0) + GlyphWidth + 2f;
						float maxW = clipW - (textX - rowX) - 2f;
						if (maxW > 0)
						{
							RectangleF textRect = new RectangleF (textX, rowTop, maxW, lh);
							ctx.DrawString (node.Text, m_Font,
								isSelected ? textBrushActive : textBrushNormal,
								textRect, FontFormat.DefaultSingleLine);
						}
					}
				}
			}

			base.OnPaint (ctx, bounds);
		}

		#endregion
	}
}
