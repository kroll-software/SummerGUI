﻿using System;
using System.Globalization;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Input;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using KS.Foundation;
using SummerGUI.Scrolling;
using SummerGUI.DataGrid;

namespace SummerGUI
{
	public class DataGridView : ScrollableContainer, IDataProviderOwner, ISupportsSelection, ISupportsFind
	{
		public event EventHandler<EventArgs> HasDataChanged;
		bool m_HasData;
		public bool HasData 
		{ 
			get {
				return m_HasData;
			}
			private set {
				if (value != HasData) {
					m_HasData = value;
					//Concurrency.WaitSpinning (50);
					Enabled = value;
					CanFocus = value;
					if (value && HasDataChanged != null)
						HasDataChanged (this, EventArgs.Empty);
				}
			}
		}

		// these objects are valid when HasData returns true
		public IRowManager RowManager { get; private set; }
		public IColumnManager ColumnManager { get; private set; }
		public ISelectionManager SelectionManager { get; private set; } 
		public IDataProvider DataProvider { get; private set; }

		public DataGridView (string name)
			: base(name, Docking.Fill, new DataGridViewWidgetStyle())
		{			
			ScrollBars = ScrollBars.Both;
			//VScrollBar.Visible = false;
			//HScrollBar.Visible = false;

			Font = SummerGUIWindow.CurrentContext.FontManager.DefaultFont;
			//Font = SummerGUIWindow.CurrentContext.FontManager.SerifFont;
			CanFocus = true;
			//Focus();

			RowHeaders = true;
			ColumnHeaders = true;
			HeaderFont = SummerGUIWindow.CurrentContext.FontManager.StatusFont;
			VerticalLines = true;
			m_RowBorderPen = new Pen(Color.FromArgb(128, Theme.Colors.Base1));
			HighlightSelection = true;
			SelectedRowColor = Theme.Colors.Blue;
			AlternatingRowColor = Color.FromArgb (45, Theme.Colors.Cyan);		
			CellPadding = new Padding (8, 4, 8, 4);
			CellToolTips = true;
		}


		public void SaveSettings()
		{			
			if (RowManager == null || RowManager.RowCount <= 0 || Columns.IsNullOrEmpty () || String.IsNullOrEmpty (this.Name))
				return;
			// check for corrupted settings, perhaps after an error. 
			// We shouldn't save such values.
			if (Columns.Any (col => col.Visible && col.Width <= 0)) {
				this.LogWarning ("Datagrid has columns with zero width. Settings are not saved.");
				return;
			}
			ConfigFile conf = ConfigurationService.Instance.ConfigFile;
			if (conf == null)
				return;
			conf.ClearSection(this.Name);
			foreach (var col in Columns) {				
				if (col.Width > 1f) {
					conf.SetSetting (this.Name, col.Key, String.Format ("visible:{0};width:{1};sort:{2}", 
						col.Visible.ToLowerString (), (col.Width / ScaleFactor).ToString (CultureInfo.InvariantCulture), col.SortDirection.ToShortString ()));
				}
			}
		}

		public void LoadSettings()
		{
			// RowManager == null || RowManager.RowCount <= 0 || 
			if (Columns.IsNullOrEmpty () || String.IsNullOrEmpty (this.Name))
				return;
			ConfigFile conf = ConfigurationService.Instance.ConfigFile;
			if (conf == null)
				return;

			try {
				this.SuspendLayout();
				int i = 1;
				Dictionary<string, string> dict = conf.ReadSection (this.Name)
					.ToDictionary (kv => kv.Key, kv => kv.Value.ToString() + ";" + i++);
				if (!dict.IsNullOrEmpty()) {
					foreach (var col in Columns) {
						string str;
						if (dict.TryGetValue (col.Key, out str)) {
							string[] a = str.Split (';');
							if (a.Length == 4) {
								col.Visible = a [0].StrMid (a [0].IndexOf (':') + 2).SafeBool();
								col.Width = Math.Max(16, a [1].StrMid (a [1].IndexOf (':') + 2).SafeFloat());
								col.SortDirection = a [2].StrMid (a [2].IndexOf (':') + 2).ToSortDirection();
								col.Position = Math.Max(0, a [3].StrMid (a [3].IndexOf (':') + 2).SafeInt());
							}
						}
					}
					Columns.Sort ();
				}
			} catch (Exception ex) {
				ex.LogWarning ();
			} finally {
				if (ColumnManager != null)
					ColumnManager.OnColumnsChanged ();
				this.ResumeLayout ();
				Invalidate ();
			}
		}

		/***
		public override bool OnSetupContextMenu (string widgetname)
		{						
			this.SetContextMenu ("Edit");
			return base.OnSetupContextMenu (widgetname);
		}
		***/

		public void SetDataProvider(IDataProvider value)
		{ 			
			if (HasData) {
				if (DataProvider == value)
					return;
				HasData = false;
			}

			DataProvider = value;
			if (DataProvider != null) {
				RowManager = DataProvider.RowManager;
				ColumnManager = DataProvider.ColumnManager;
				SelectionManager = DataProvider.SelectionManager;

				RowManagerObserver = new Observer<EventMessage> (RowManagerOnNext, RowManagerOnError, RowManagerOnCompleted);
				RowManagerObserver.Subscribe(RowManager);

				ColumnManagerObserver = new Observer<EventMessage> (ColumnManagerOnNext, ColumnManagerOnError, ColumnManagerOnCompleted);
				ColumnManagerObserver.Subscribe(ColumnManager);
			}
		}

		public event EventHandler<EventArgs> DataLoaded;
		public void OnDataLoaded()
		{
			if (RowManager != null && RowIndex < 0) {
				RowManager.MoveFirst ();
			}
			if (ColumnManager != null)
				ColumnManager.OnColumnsChanged ();			
			HasData = true;
			SetupScrollBars ();
			if (DataLoaded != null)
				DataLoaded (this, EventArgs.Empty);
		}			
				
		// ***************** MESSAGING *****************

		public Observer<EventMessage> RowManagerObserver { get; private set; }

		public void RowManagerOnNext(EventMessage message) 
		{
			switch (message.Subject) {
			case RowManagerMessages.RowsChanged:
				//int current = RowManager.CurrentRowIndex + 1;
				//int total = RowManager.RowCount;
				//UpdateRecordCount (current, total);
				//if (current > 0)
					//EnableControls (current);
				break;			
			}
		}

		public void RowManagerOnError(Exception ex) 
		{

		}

		public void RowManagerOnCompleted() 
		{

		}


		public Observer<EventMessage> ColumnManagerObserver { get; private set; }

		public void ColumnManagerOnNext(EventMessage message) 
		{

		}

		public void ColumnManagerOnError(Exception ex) 
		{

		}

		public void ColumnManagerOnCompleted() 
		{

		}


		public event EventHandler<EventArgs> ItemSelected;
		public void OnItemSelected()
		{
			if (ItemSelected != null)
				ItemSelected (this, EventArgs.Empty);
		}

		public event EventHandler<EventArgs> NewItem;
		public void OnNewItem()
		{
			if (NewItem != null)
				NewItem (this, EventArgs.Empty);
		}

		public event EventHandler<EventArgs> DeleteItem;
		public void OnDeleteItem()
		{
			if (DeleteItem != null)
				DeleteItem (this, EventArgs.Empty);
		}

		public DataGridColumnCollection Columns 
		{ 
			get {
				if (ColumnManager == null)
					return null;
				return ColumnManager.Columns;
			}
		}			

		public bool RowHeaders { get; set; }
		public bool ColumnHeaders { get; set; }

		public float HeadHeight { get; set; }
		public bool GridLines { get; set; }
		public bool VerticalLines { get; set; }

		// ToDo: Detect Keydown or Mousedown
		// Quadtree only when not Keydown / Mousedown.
		// then invalidate again on KeyUp / MouseUp !
		//
		// Alternating RowColors only when not Keydown / Mousedown, 
		// because of stroposcobe effect.

		private Brush m_AlternatingRowColorBrush;
		public Color AlternatingRowColor
		{
			get{
				if (m_AlternatingRowColorBrush == null)
					return Color.Empty;
				return m_AlternatingRowColorBrush.Color;
			}
			set{
				if (m_AlternatingRowColorBrush == null)
					m_AlternatingRowColorBrush = new SolidBrush (value);
				else
					m_AlternatingRowColorBrush.Color = value;
			}
		}

		private IGUIFont m_Font;
		public IGUIFont Font
		{
			get{
				return m_Font;
			}
			set{
				if (m_Font != value) {
					m_Font = value;
					OnFontChanged ();
				}
			}
		}

		protected virtual void OnFontChanged()
		{
			ResetCachedLayout();
		}

		private IGUIFont m_HeaderFont;
		public IGUIFont HeaderFont
		{
			get{
				return m_HeaderFont;
			}
			set{
				if (m_HeaderFont != value) {
					m_HeaderFont = value;
					OnHeaderFontChanged ();
				}
			}
		}

		protected virtual void OnHeaderFontChanged()
		{
			ResetCachedLayout();
		}

		public float RowHeight { get; set; }

		public void OnRowCountChanged()
		{
			if (HasData) {
				SetupScrollBars ();
				Invalidate ();
			}
		}

		public void OnColumnsChanged ()
		{
			if (HasData) {
				SetupScrollBars ();
				Invalidate ();
			}
		}

		public RectangleF ScrollBounds
		{
			get{
				return new RectangleF (Left + RowHeaderWidth, Top + HeadHeight, Width - VScrollBarWidth - RowHeaderWidth, Height - HeadHeight - HScrollBarHeight);
			}
		}
			
		private bool ensureVisibleFlag = false;
		protected override void OnScaleWidget (IGUIContext ctx, float absoluteScaleFactor, float relativeScaleFactor)
		{
			base.OnScaleWidget (ctx, absoluteScaleFactor, relativeScaleFactor);
			if (Columns != null) {
				Columns.OfType<DataGridColumn> ().Where (col => col.Width > 1f).ForEach (col => col.Width *= relativeScaleFactor);
			}
			if (ColumnManager != null)
				ColumnManager.OnColumnsChanged ();
			ensureVisibleFlag = true;
		}			
			
		//private long ResetCachedLayoutCalled = 0;
		protected override void ResetCachedLayout()
		{			
			//ResetCachedLayoutCalled++;
			//Console.WriteLine ("ResetCachedLayout() # {0} on {1}", ResetCachedLayoutCalled.ToString("n0"), this.Name);

			if (m_Font != null)
				RowHeight = m_Font.TextBoxHeight;

			if (ColumnHeaders)
				m_HeaderHeight = RowHeight;
			else
				m_HeaderHeight = 0;			

			//if (HeaderFont != null)
			//m_HeaderHeight = Math.Max(RowHeight, (int)(HeaderFont.CaptionHeight));

			HeadHeight = m_HeaderHeight;
			if (RowHeaders)
				RowHeaderWidth = ScrollBar.ScrollBarWidth;
			else
				RowHeaderWidth = 0;
			SetupScrollBars ();
		}

		public void SetupScrollBars()
		{
			if (Bounds.IsEmpty)
				return;

			// ToDo:
			if (ColumnHeaders)
				VScrollBar.Margin = new Padding (0, m_HeaderHeight > 0 ? m_HeaderHeight : RowHeight, 0, 0);
			if (RowHeaders)
				HScrollBar.Margin = new Padding (RowHeaderWidth > 0 ? RowHeaderWidth : RowHeight, 0, 0, 0);
			ScrollBars = ScrollBars.Both;

			if (!HasData)
				return;
			VScrollBar.SetUp (Height - HeadHeight - HScrollBarHeight, RowManager.LastRowBottom, RowHeight);
			HScrollBar.SetUp (Width - RowHeaderWidth - VScrollBarWidth, m_MaxColumnWidth, ScrollBar.ScrollBarWidth);
		}

		public override void OnResize ()
		{
			base.OnResize ();
			SetupScrollBars ();

			// after rescaling
			if (ensureVisibleFlag) {
				ensureVisibleFlag = false;
				if (RowManager != null && RowManager.CurrentRowIndex >= 0)
					RowManager.EnsureRowindexVisible (RowManager.CurrentRowIndex);
			}
		}

		public override SizeF PreferredSize (IGUIContext ctx, SizeF proposedSize)
		{
			return base.PreferredSize (ctx, proposedSize);
		}

		// *** ISupportsSelection ***

		//public event EventHandler<EventArgs> SelectionChanged;
		public void OnSelectionChanged()
		{						
			//if (SelectionChanged != null)
			//	SelectionChanged(this, EventArgs.Empty);
		}

		public bool CanSelectAll
		{
			get{				
				return SelectionManager != null && SelectionManager.CanSelectAll;
			}
		}

		public bool CanInvertSelection
		{
			get{
				return SelectionManager != null && SelectionManager.CanInvertSelection;
			}
		}

		public void SelectAll()
		{			
			SelectionManager.SelectAll ();
		}

		public void InvertSelection()
		{
			SelectionManager.InvertSelection ();
		}

		// *** ISupportsFind ***

		//int LastFindIndex = -1;
		//string LastFindString;
		public bool CanFind
		{
			get{
				return RowManager != null && RowManager.RowCount > 0;
			}
		}

		public bool CanFindNext
		{
			get{
				//return LastFindString != null && RowManager.RowCount > 0;
				return false;
			}
		}

		public bool CanFindPrevious
		{
			get{
				//return LastFindString != null && RowManager.RowCount > 0;
				return false;
			}
		}

		public void Find()
		{
		}

		public void FindNext()
		{
		}

		public void FindPrevious()
		{
		}


		public event EventHandler CurrentRowChanged;
		private int lastCurrentRow = -1;
		public void OnCurrentRowChanged()
		{
			if (RowManager == null || RowManager.CurrentRowIndex != lastCurrentRow) {
				lastCurrentRow = RowManager.CurrentRowIndex;

				if (CurrentRowChanged != null)
					CurrentRowChanged (this, EventArgs.Empty);
			}
		}

		public event EventHandler IsEditingChanged;

		public event EventHandler UpdateEditMenu;
		public void OnUpdateEditMenu()
		{
			if (UpdateEditMenu != null)
				UpdateEditMenu(this, EventArgs.Empty);
		}

		public virtual float ScrollOffsetX
		{
			get
			{				
				return -HScrollBar.Value;
			}
			set
			{
				HScrollBar.Value = -value;
			}
		}

		public ScrollBar VScrollBuddy { get; set; }
		public float ScrollOffsetY
		{
			get
			{				
				if (VScrollBuddy != null)
					return -VScrollBuddy.Value;
				else
					return -VScrollBar.Value;
			}
			set
			{
				if (VScrollBuddy != null)
					VScrollBuddy.Value = -value;
				else
					VScrollBar.Value = -value;
			}
		}

		public float VerticalScrollOffset
		{
			get{
				return Top + HeadHeight + ScrollOffsetY;
			}
		}			

		protected float m_MaxColumnWidth = 0;
		protected int m_MaxColumnIndex = -1;

		public float RowHeaderWidth { get; set; }

		protected float m_HeaderHeight = 21;

		// Mouse Click
		protected int MouseX = 0;
		protected int MouseY = 0;
		protected MouseButton MouseButton;

		// Column Resizing
		protected DataGridColumn m_ColumnResizingColumn = null;
		protected float m_ColumnResizingXStart = 0;
		protected float m_ColumnResizingWidthStart = 0;
		protected bool m_ColumnResizingFlag = false;

		// Column Moving (reordering)
		protected DataGridColumn m_ColumnMovingColumn = null;        
		protected bool m_ColumnMovingFlag = false;
		//protected ImageList m_ImageListDrag = null;
		protected DataGridColumn m_LastDragHoverColumn = null;
		protected bool m_LastAllowDrop = false;

		public int[] SelectedRowIndices
		{
			get
			{
				if (!HasData)
					return null;
				return SelectionManager.SelectedRowIndices.ToArray();
			}
		}

		public virtual int RowIndex
		{
			get
			{
				if (!HasData)
					return -1;
				return RowManager.CurrentRowIndex;
			}
			set
			{
				if (!HasData)
					return;
				if (value != RowManager.CurrentRowIndex)
				{					
					RowManager.CurrentRowIndex = value;
					StartSelectionTimer ();
					//OnSelectionChanged();
				}
			}
		}
			
		public virtual int ColumnIndex
		{
			get
			{
				if (!HasData)
					return -1;
				return ColumnManager.CurrentColumnIndex;
			}
			set
			{
				if (!HasData)
					return;
				if (value != ColumnManager.CurrentColumnIndex)
				{
					ColumnManager.CurrentColumnIndex = value;
					StartSelectionTimer ();
					//OnSelectionChanged();
				}
			}
		}

		// Editing
		protected bool m_IsEditing = false;
		public bool IsEditing
		{
			get
			{
				return m_IsEditing;
			}            
		}

		protected virtual void SetIsEditing(bool bEditing)
		{
			if (m_IsEditing != bEditing)
			{
				m_IsEditing = bEditing;

				if (IsEditingChanged != null)
					IsEditingChanged(this, EventArgs.Empty);

				//OnUpdateEditMenu();
			}
		}

		protected Widget m_EditControl = null;
		public Widget EditControl
		{
			get
			{
				return m_EditControl;
			}
		}

		protected DataGridColumn m_EditDataColumn = null;
		public DataGridColumn EditDataColumn
		{
			get
			{
				return m_EditDataColumn;
			}
		}

		protected int m_EditControlTop = 0;
		protected int m_EditControlScrollY = 0;

		protected int m_EditControlRowIndex = 0;
		protected Rectangle m_EditControlTextBounds = Rectangle.Empty;
		protected int m_EditControlIndent = 0;        

		private int m_AllowEditSuspendCounter = 0;
		public void SuspendAllowEdit()
		{
			if (m_AllowEditSuspendCounter == 0)
				this.EndEdit(false);

			unchecked
			{
				m_AllowEditSuspendCounter++;
			}
		}

		public void ResumeAllowEdit()
		{
			unchecked
			{
				m_AllowEditSuspendCounter--;
			}

			if (m_AllowEditSuspendCounter < 0)
				m_AllowEditSuspendCounter = 0;
		}

		public bool IsAllowEditSuspended
		{
			get
			{
				return m_AllowEditSuspendCounter > 0;
			}
		}

		public bool AllowEdit { get; set; }
		public bool AllowRowReorder { get; set; }
		public bool AllowColumnReorder { get; set; }
		public bool AllowSort { get; set; }			

		public Brush RowColor
		{
			get
			{
				return Theme.Brushes.Base3;
			}            
		}        
			
		public Brush GroupRowColor
		{
			get
			{
				return Theme.Brushes.Base01;
			}            
		}        
			
		public Color SelectedRowColor
		{
			get{
				if (SelectedRowBrush == null)
					return Theme.Colors.HighLightBlue;
				return SelectedRowBrush.Color;
			}
			set{
				if (SelectedRowBrush == null)
					SelectedRowBrush = new SolidBrush (value);
				else
					SelectedRowBrush.Color = value;

				if (SelectedRowInactiveBrush == null)
					SelectedRowInactiveBrush = new SolidBrush (value.ToGray());
				else
					SelectedRowInactiveBrush.Color = value.ToGray();
			}
		}

		public Brush SelectedRowBrush { get; private set; }
		public Brush SelectedRowInactiveBrush { get; private set; }
			
		private Pen m_RowBorderPen = null;
		public Pen RowBorder
		{
			get
			{
				return m_RowBorderPen;
			}
		}

		[DpiScalable]
		public float RowBorderWidth
		{
			get{
				return m_RowBorderPen.Width;
			}
			set{
				m_RowBorderPen.Width = value;
			}
		}

		public bool HighlightSelection { get; set; }
		public bool HideSelection { get; set; }

		public Brush HeaderBackground
		{
			get
			{
				return Theme.Brushes.Base1;
			}
		}

		public Brush HeaderForeground
		{
			get{
				return Theme.Brushes.Base01;
			}
		}

		public Pen HeaderBorder
		{
			get{
				return Theme.Pens.Base0;
			}
		}

		public int GroupIndent { get; set; }
		public int TaskIndent { get; set; }
		public bool CellToolTips { get; set; }

		Brush GetSelectedRowColor(bool active)
		{
			if (active)
				return SelectedRowBrush;
			else
				return SelectedRowInactiveBrush;            
		}

		protected int iColumnsLayoutSuspended = 0;        
		public virtual void SuspendColumnLayout()
		{
			//if (iColumnsLayoutSuspended == 0)
			//{   
			//    // reset more flags here
			//}

			iColumnsLayoutSuspended++;
		}

		public bool IsColumnLayoutSuspended
		{
			get
			{
				return iColumnsLayoutSuspended > 0;
			}
		}

		public virtual void ResumeColumnLayout(bool invalidate = true)
		{
			iColumnsLayoutSuspended--;
			if (iColumnsLayoutSuspended <= 0)
			{
				iColumnsLayoutSuspended = 0;
				if (invalidate)
					this.Invalidate();
			}
		}

		public virtual bool EndEdit(bool SaveData)
		{
			return true;
		}

		public virtual bool BeginEdit()
		{
			PointF pt = this.CurrentPoint();
			return BeginEdit(pt.X, pt.Y);
		}

		public virtual bool BeginEdit(float X, float Y)
		{
			return true;
		}

		public virtual void SelectNextCell()
		{
			if (SelectionManager.SelectionMode != DataGridSelectionModes.Cell || RowManager == null)
				return;

			ColumnIndex++;
			if (ColumnIndex > m_MaxColumnIndex && RowIndex < RowManager.RowCount - 1)
			{
				ColumnIndex = 0;
				RowIndex++;
			}
			if (ColumnIndex > m_MaxColumnIndex)
				ColumnIndex = m_MaxColumnIndex;

			DataGridColumn col = ColumnManager.ColumnFromColumnIndex(ColumnIndex);
			if (col != null && col.Key == "Icons" && Columns.Count > 1 && ColumnIndex != m_MaxColumnIndex)
				SelectNextCell();
		}

		public virtual void SelectPreviousCell()
		{
			if (SelectionManager.SelectionMode != DataGridSelectionModes.Cell)
				return;

			ColumnIndex--;
			if (ColumnIndex < 0 && RowIndex > 0)
			{
				ColumnIndex = m_MaxColumnIndex;
				RowIndex--;
			}
			if (ColumnIndex < 0)
				ColumnIndex = 0;

			DataGridColumn col = ColumnManager.ColumnFromColumnIndex(ColumnIndex);
			if (col != null && col.Key == "Icons" && Columns.Count > 1 && ColumnIndex != 0)
				SelectPreviousCell();
		}

		protected virtual PointF CurrentPoint()
		{
			PointF p = PointF.Empty;

			List<DataGridColumn> sortedVisibleColumns = Columns.SortedVisibleColumns.ToList();

			if (ColumnIndex >= 0 && ColumnIndex < sortedVisibleColumns.Count && RowManager != null)
			{
				p.X = sortedVisibleColumns[ColumnIndex].ColumnHeaderBounds.X + 1;

				RowInfo info = RowManager.RowInfoByRowIndex(RowIndex);
				if (info != RowInfo.Empty)
					p.Y = info.RowTop + VerticalScrollOffset + 1;
			}

			return p;
		}

		public virtual void EnsureColumnVisible(DataGridColumn column)
		{
			if (column == null)
				return;            

			//this.m_EditControlIndent

			float clr = ScrollBounds.Width;
			float cw = column.AbsoluteWidth (clr);

			if (column.Left - RowHeaderWidth <= 0 && column.Left + cw >= clr)
				return;

			if (column.Left - RowHeaderWidth < 0)
			{                
				this.ScrollOffsetX -= column.Left - RowHeaderWidth;
			}
			else if (column.Left + cw > clr)
			{                
				this.ScrollOffsetX = clr - (column.Left + cw) + this.ScrollOffsetX - 4;                
			}            
		}

		public virtual void ScrollLeft()
		{
			if (HScrollBar.Visible)
			{
				HScrollBar.Value = Math.Max(HScrollBar.Minimum, HScrollBar.Value - HScrollBar.SmallChange);
			}
		}

		public virtual void ScrollRight()
		{
			if (HScrollBar.Visible)
			{
				HScrollBar.Value = Math.Max(HScrollBar.Minimum, Math.Min(HScrollBar.Maximum - HScrollBar.LargeChange, HScrollBar.Value + HScrollBar.SmallChange));
			}
		}

		public void BestFitTreeColumn()
		{
			DataGridColumn col = Columns.TreeColumn;            
			if (col != null)
				BestFitColumn(col);
		}

		//[ROXANA] best fit column
		public virtual void BestFitColumn(DataGridColumn col, bool allowWordWrap = true)
		{
			float width = 0;
			//int Indent = 0;            

			StringFormat sf;
			if (allowWordWrap)
				sf = StringFormat.GenericDefault;
			else
				sf = new StringFormat(StringFormatFlags.NoWrap | StringFormatFlags.DisplayFormatControl);

			//int iconWidth = 0;

			/**
			if (col.IsTreeColumn && m_GanttControl.GanttItemImages != null)
				iconWidth = m_GanttControl.GanttItemImages.ImageSize.Width + 2;

			using (Font FontBold = new Font(this.Font.FontFamily, this.Font.Size, FontStyle.Bold))            
			using (Graphics gfx = this.CreateGraphics())
			{
				GanttControl.CurrentView.Items.ForEach(itm =>
					{
						string text = GetItemPropertyString(itm, col.Key, col.DisplayFormatString);
						if (!string.IsNullOrEmpty(text))
						{
							int rowIndex = itm.RowIndex;
							int itemIconIndent = itm.ImageKey.IsNullOrEmpty() ? 0 : iconWidth;

							if (col.IsTreeColumn)
							{
								switch (itm.ItemType)
								{
								case GanttItemType.GroupItem:
								case GanttItemType.ResourceItem:                                
									if (((IExpandableItem)itm).ChildCountVisible > 0)
										Indent = itm.GroupLevel * m_GroupIndent + itemIconIndent + 16;
									else
										Indent = (itm.GroupLevel - 1) * m_GroupIndent + itemIconIndent + m_TaskIndent;
									break;

								case GanttItemType.TaskItem:                                    
									if (itm.GroupLevel > 0)
										Indent = itm.GroupLevel * m_GroupIndent + itemIconIndent + m_TaskIndent;
									else
										Indent = itemIconIndent;                                    
									break;
								}
							}

							SizeF layoutArea = new SizeF(Int32.MaxValue, itm.RowInfo(GanttControl.CurrentView).RowHeight);                        
							int internalWidth = Math.Max(col.DesiredWidth, Indent + (int)gfx.MeasureString(text, FontBold, layoutArea, sf).Width);
							width = Math.Max(internalWidth, width);
						}
					});
			}
			***/

			if (width > 16)
			{
				col.Width = Math.Max(col.MinWidth, width + 8);
			}
		}

		protected virtual float ColumnStartX()
		{
			return Left + RowHeaderWidth + ScrollOffsetX;
		}			

		public RectangleF RowBoundsByRowIndex(int rowIndex)
		{
			if (rowIndex < 0 || RowManager == null)
				return RectangleF.Empty;

			RowInfo info = RowManager.RowInfoByRowIndex(rowIndex);
			return new RectangleF (Bounds.Left, info.RowTop + VerticalScrollOffset, Bounds.Width, info.RowHeight);
		}

		protected virtual MouseControlItem FindControlItem(int x, int y)
		{            
			if (ControlItems == null)
				return MouseControlItem.Empty;

			ILayoutItem li = ControlItems.Query(new RectangleF(x, y, 1, 1)).FirstOrDefault();

			if (li == null)
				return MouseControlItem.Empty;

			return (MouseControlItem)li;
		}

		protected QuadTree ControlItems = null;
		protected RectangleF LastTooltipBounds = RectangleF.Empty;

		[DpiScalable]
		public Padding CellPadding { get; set; }

		protected virtual void DrawColumnHeaders(IGUIContext gfx, RectangleF RH, RectangleF Bounds)
		{			
			// Column Headers Captions
			float ColumnDeviderX;
			DataGridColumnCollection columns = Columns;
			if (columns == null)
				return;

			float iColStart = ColumnStartX();
			float crw = Width - VScrollBarWidth - RowHeaderWidth;

			IGUIFont font = HeaderFont;
			if (font == null)
				font = Font;

			using (SolidBrush ColumnTextBrush = new SolidBrush(HeaderForeground.Color))
			{
				m_MaxColumnWidth = 0;
				m_MaxColumnIndex = -1;
				float lastColWidth = 2;
				foreach (DataGridColumn col in columns)
				{
					float colWidth = col.AbsoluteWidth (crw);

					if (col.Visible)
					{						
						float rightIndent = 0;
						if (col.AllowSort && col.SortDirection != SortDirections.None)
							//rightIndent = 16.Scale(ScaleFactor);
							rightIndent = ScrollBar.ScrollBarWidth;

						col.ColumnHeaderBounds = new RectangleF (iColStart, RH.Top, colWidth, RH.Height);

						RectangleF RT = new RectangleF (iColStart + CellPadding.Left, RH.Top, colWidth - CellPadding.Right - rightIndent - 1, RH.Height);
						if (RT.Right > Left && RT.Left < Right) {
							float stringWidth = gfx.DrawString (col.Caption, font, ColumnTextBrush, RT, FontFormat.DefaultSingleLine).Width;
						}

						//if (CellToolTips && stringWidth > RT.Width)
						//	this.ControlItems.Add(new MouseControlItem(RT, MouseControlItemTypes.Tooltip, col.Caption));

						ColumnDeviderX = iColStart + colWidth;
						if (ColumnDeviderX > Bounds.Left && ColumnDeviderX < Bounds.Right) {							

							gfx.DrawLine (HeaderBorder, ColumnDeviderX, RH.Top + 4, ColumnDeviderX, RH.Top + RH.Height - 6);

							if (col.AllowResize) {
								// resizing / border
								ControlItems.Add (new MouseControlItem (new RectangleF (ColumnDeviderX - 2, RT.Top + 4, 4, RT.Height - 8), MouseControlItemTypes.ColumnHeaderBorder, col));

								// reordering
								RectangleF rColumnInside = new RectangleF (col.ColumnHeaderBounds.X + 2, col.ColumnHeaderBounds.Top, col.ColumnHeaderBounds.Width - 4, col.ColumnHeaderBounds.Height);
								ControlItems.Add (new MouseControlItem (rColumnInside, MouseControlItemTypes.ColumnHeader, col));
							}

							if (col.AllowSort && col.SortDirection != SortDirections.None) {
								// Draw a Triangle
								// nothing could be simpler..                            

								List<PointF> triAngle = new List<PointF> ();

								float triHeight = (6f * ScaleFactor).Ceil ();
								float triWidth = (10f * ScaleFactor).Ceil ();

								float iTop = col.ColumnHeaderBounds.Top + (col.ColumnHeaderBounds.Height / 2) - (triHeight / 2) - 1;
								float iBottom = iTop + triHeight;
								float iLeft = col.ColumnHeaderBounds.Right - triWidth - triHeight;
								float iRight = iLeft + triWidth;
								float iCenter = iLeft + (triWidth / 2);

								switch (col.SortDirection) {
								case SortDirections.Ascending:                                    
									triAngle.Add (new PointF (iCenter, iTop));
									triAngle.Add (new PointF (iLeft, iBottom));
									triAngle.Add (new PointF (iRight, iBottom));                                    
									break;

								case SortDirections.Descending:
									triAngle.Add (new PointF (iLeft, iTop));
									triAngle.Add (new PointF (iRight, iTop));
									triAngle.Add (new PointF (iCenter, iBottom));
									break;
								}

								if (triAngle.Count > 0) {
									// draw sunken 3d effect
									//using (SolidBrush brush = new SolidBrush(Color.FromArgb(128, Color.White)))
									//{
									//    gfx.TranslateTransform(1, 1);
									//    gfx.FillPolygon(brush, triAngle.ToArray());
									//    gfx.ResetTransform();
									//}

									gfx.FillPolygon (Theme.Brushes.Base01, triAngle.ToArray ());
									triAngle.Clear ();
								}                            
							}
						}

						col.Left = iColStart;

						int w = col.AbsoluteWidth (crw);
						iColStart += w;
						m_MaxColumnWidth += w;
						m_MaxColumnIndex++;
						lastColWidth = col.Width;
					}
					else
						col.ColumnHeaderBounds = Rectangle.Empty;
				}

				if (lastColWidth > 1)
					//m_MaxColumnWidth += 16;
					m_MaxColumnWidth += ScrollBar.ScrollBarWidth;
			}
		}

		protected virtual void DrawHeaderTopSpace(IGUIContext ctx, RectangleF r)
		{
			if (r.Width > 0 && r.Height > 0)				
				ctx.FillRectangle(HeaderBackground, r);
		}
			
		private FontFormat TextCellStringFormat(DataGridColumn col)
		{   
			FontFormatFlags flags = FontFormatFlags.Elipsis;
			if (col.EditType == EditTypes.MultilineText)
				flags |= FontFormatFlags.WrapText;

			return new FontFormat (col.TextAlignment, col.LineAlignment, flags);
		}        

		protected virtual void DrawTextCell(IGUIContext gfx, DataGridColumn col, RectangleF RText, string text, RectangleF Bounds, bool bHighLight)
		{
			if (!(RText.Left < Right && RText.Right > Left))
				return;

			FontFormat sf = TextCellStringFormat(col);

			float fontHeight = 0;
			float TextIndent = 0; // ToDo: is always 0
			float multilineYOffset = 5;   // 3

			if (col.TextAlignment == Alignment.Far)
				RText.Width -= CellPadding.Right;

			if (col.LineAlignment == Alignment.Near)
			{
				if (col.EditType != EditTypes.MultilineText)
					RText.Offset(0, multilineYOffset);
				else
				{
					fontHeight = (int)(Font.LineHeight / 2f + 0.5f);
					RText.Offset(0, fontHeight);
				}
			}

			// Hier kommen die Styles zu den Fonts also her...

			float stringWidth;
			if (bHighLight)
				stringWidth = gfx.DrawString(text, this.Font, Theme.Brushes.White, RText, sf).Width;
			else				
				stringWidth = gfx.DrawString(text, this.Font, Theme.Brushes.Base02, RText, sf).Width;

			if (CellToolTips || col.AutoMinWidth)
			{				
				if (CellToolTips && stringWidth > RText.Width)
					this.ControlItems.Add(new MouseControlItem(RText, MouseControlItemTypes.Tooltip, text));
				if (col.AutoMinWidth)
					col.DesiredWidth = (int)Math.Max(col.DesiredWidth, TextIndent + 8 + stringWidth);
			}            
		}

		private bool m_PrintingFlag { get; set; }

		public int FirstRowOnScreen
		{
			get
			{
				if (m_PrintingFlag || RowManager == null)
					return 0;
				else
					return RowManager.RowIndexFromPoint(Top + HeadHeight);
			}
		}

		public int LastRowOnScreen
		{
			get
			{
				if (RowManager == null)
					return -1;

				if (m_PrintingFlag)
				{                    
					return RowManager.RowCount - 1;
				}
				else
				{
					int maxRow = RowManager.RowIndexFromPoint(Bottom + HeadHeight);

					if (maxRow < 0 || maxRow > RowManager.RowCount - 1)
						maxRow = RowManager.RowCount - 1;

					return maxRow;
				}
			}
		}


		//private StringFormat textFormat;

		public override void OnPaintBackground (IGUIContext ctx, RectangleF bounds)
		{
			base.OnPaintBackground (ctx, bounds);

			using (var br = new SolidBrush (Color.FromArgb(128, Color.GhostWhite))) {
				ctx.FillRectangle (br, bounds);
			}
		}

		public override void OnPaint (IGUIContext ctx, RectangleF bounds)
		{			
			// Top-Left Area
			RectangleF RTOP = new RectangleF (Left, Top, Width, HeadHeight - m_HeaderHeight);
			// overridable action
			DrawHeaderTopSpace (ctx, RTOP);

			// Column-Headers
			RectangleF RH = new RectangleF (Left, Top, Width, m_HeaderHeight);
			if (ColumnHeaders) {				
				ctx.DrawGrayButton (RH);
				ctx.DrawLine (Theme.Pens.Base01, RH.Left, RH.Top, RH.Right, RH.Top);

				if (RowHeaderWidth > 0)
					ctx.DrawLine (HeaderBorder, Left + RowHeaderWidth, RH.Top, Left + RowHeaderWidth, RH.Bottom - 3);

				// HeaderLine
				ctx.DrawLine (HeaderBorder, Bounds.Left, Top + HeadHeight, Bounds.Right, Top + HeadHeight);
			}

			// No Data.. EXIT
			if (RowManager == null)
				return;

 			int startRowIndex = FirstRowOnScreen;
			int endRowIndex = LastRowOnScreen;

			ControlItems = new QuadTree(Bounds);				

			RectangleF clientRectangle = ScrollBounds;
			float oldMaxColumnWidth = m_MaxColumnWidth;

			// Column Headers Captions
			RectangleF ClipRect = new RectangleF(Bounds.X + RowHeaderWidth, HeadHeight - m_HeaderHeight, Bounds.Width - RowHeaderWidth, Bounds.Height);
			using (var clip = new ClipBoundClip (ctx, ClipRect)) {

				// overridable action
				if (ColumnHeaders) {
					DrawColumnHeaders (ctx, RH, Bounds); // since here, column.Left is valid
				}

				// **********************

				if (RowManager == null || RowManager.RowCount <= 0)
					return;

				//int ColumnDeviderX;
				DataGridColumnCollection columns = Columns;
				if (columns == null)
					return;

				if (ColumnIndex > m_MaxColumnIndex)
					ColumnIndex = m_MaxColumnIndex;

				// *** Rows ***

				RectangleF Region1 = new RectangleF (Bounds.X, Top + HeadHeight, Bounds.Width - VScrollBarWidth, Bounds.Height - HeadHeight);	//  - HScrollBarHeight ?
				RectangleF Region2 = new RectangleF (Bounds.X + RowHeaderWidth, Top + HeadHeight, Bounds.Width - RowHeaderWidth - VScrollBarWidth, Bounds.Height - HeadHeight);	 //  - HScrollBarHeight ?
				RectangleF rClip = Bounds;
				rClip.Intersect (Region1);
				rClip.Intersect (Region2);
				//ctx.SetClip (rClip);

				using (var rowClip = new ClipBoundClip (ctx, rClip, false)) {
					using (Pen penTreeLines = new Pen (Color.White)) {
						columns.ForEach (c => c.DesiredWidth = 0);

						Stack<GroupStart> stackGroups = null;
						//int treeLinesMaxX = -1;
						float treeColumnX = -1;
						if (Columns.TreeColumn != null) {
							treeColumnX = Columns.TreeColumn.Left;
							stackGroups = new Stack<GroupStart> ();
							int groupLevel = DataProvider.GroupLevel (0);
							for (int k = 0; k < groupLevel; k++) {                 
								//stackGroups.Push (new GroupStart (k, (treeColumnX + (k * GroupIndent) + 10).Ceil(), 0, 0));
								stackGroups.Push (new GroupStart (k, (int)(treeColumnX + (k * GroupIndent) + 10), 0, 0));
							}
						}					
					
						//int CenterYCell = 0;                
						//int LastGroupLevel = 0;
						int colStartX = (int)(ColumnStartX () + 0.5);

						for (int rowIndex = startRowIndex; rowIndex <= endRowIndex; rowIndex++) {
							//ctx.SetClip (Region1);

							RowInfo info = RowManager.RowInfoByRowIndex (rowIndex);
							float top = info.RowTop + VerticalScrollOffset;
							RectangleF R = new RectangleF (Bounds.X, top, Bounds.Width, info.RowHeight);
							bool bHighLightRow = false;
							Brush highlightBrush = null;

							using (var reg1Clip = new ClipBoundClip (ctx, Region1, false)) {								
								if (RowHeaderWidth > 0) {
									RectangleF recRowHeader = new RectangleF (Left, top, RowHeaderWidth, info.RowHeight);
									ctx.DrawGrayButton (recRowHeader);
									ctx.DrawLine (HeaderBorder, recRowHeader.Left, recRowHeader.Bottom, recRowHeader.Right, recRowHeader.Bottom);
								}

								R.X += RowHeaderWidth;
								R.Width -= RowHeaderWidth;

								//int m_ItemIconIndent = 0;
								//if (ganttItemImages != null && item != null && !item.ImageKey.IsNullOrEmpty())
								//	m_ItemIconIndent = ganttItemImages.ImageSize.Width + 2;

								// left INDENT

								if (HighlightSelection && !m_PrintingFlag && !m_IsEditing && (RowManager.CurrentRowIndex == rowIndex || SelectionManager.IsRowSelected (rowIndex))) {
									if (IsFocused) {
										if (m_RowBorderPen.Width > 0) {
											highlightBrush = SelectedRowBrush;										
										} else {
											ctx.FillRectangle (SelectedRowBrush, R);
										}
										bHighLightRow = true;
									} else if (!HideSelection) {
										if (m_RowBorderPen.Width > 0) {
											highlightBrush = SelectedRowInactiveBrush;
										} else {
											ctx.FillRectangle (SelectedRowInactiveBrush, R);
										}
										bHighLightRow = true;
									}
								} else if (!AlternatingRowColor.IsEmpty && rowIndex % 2 > 0) {
									ctx.FillRectangle (m_AlternatingRowColorBrush, R);
								} else {
									//ctx.FillRectangle (RowColorBrush, R);
								}

								if (m_RowBorderPen.Width > 0)
									ctx.DrawLine (m_RowBorderPen, R.Left, R.Bottom, R.Right, R.Bottom);
							}

							// draw columns
							using (var reg2Clip = new ClipBoundClip (ctx, Region2, false)) {
								
								// DrawCellStart >>>>>>>>>>>>>>>>>>>>>>>
								int iColStart = colStartX;
								int INDENT = 0;
								string text = "";
								//int TextIndent = 0;                        

								int colNumber = -1;
								int iColl = -1;
								foreach (DataGridColumn col in Columns) {
									iColl++;
									int colWidth = col.AbsoluteWidth (clientRectangle.Width);
									//if (col.IsTreeColumn)
									//	treeLinesMaxX = iColStart + colWidth;

									Rectangle RectangleCell = new Rectangle (iColStart, (int)(R.Top + 0.5f), colWidth, (int)(R.Height + 0.5f));
									bool bCellVisible = RectangleCell.IntersectsWith (bounds.Ceil());

									if (col.Visible)
										colNumber++;

									if (col.Visible && (bCellVisible || col.IsTreeColumn)) {
										bool bSelectedCell = bCellVisible && HighlightSelection && this.IsFocused && SelectionManager.SelectionMode == DataGridSelectionModes.Cell && rowIndex == RowIndex && colNumber == ColumnIndex && m_EditControl == null;

										/***
										switch (col.LineAlignment)
										{
										case VerticalFontAlignment.Center:
											CenterYCell = R.Top - 1 + R.Height / 2;
											break;

										case VerticalFontAlignment.Top:
											CenterYCell = R.Top + (int)Font.LineHeight - 1;
											break;

										case VerticalFontAlignment.Bottom:
										case VerticalFontAlignment.Baseline:
											CenterYCell = R.Bottom - (int)Font.LineHeight - 1;
											break;
										}
										***/

										INDENT = (int)CellPadding.Left;

										if (bCellVisible) {
											//INDENT += 0;

											if (highlightBrush != null) {												
												ctx.FillRectangle (highlightBrush, new RectangleF (RectangleCell.X, RectangleCell.Y, RectangleCell.Width - m_RowBorderPen.Width, RectangleCell.Height - m_RowBorderPen.Width));
											}

											Rectangle RText = new Rectangle ((int)(iColStart + INDENT), (int)(R.Top), (int)(colWidth - INDENT - 4), (int)(R.Height));
											text = DataProvider.GetValue (rowIndex, iColl);
											using (var clpTextCell = new ClipBoundClip (ctx, RText)) {
												DrawTextCell (ctx, col, RText, text, Bounds, bHighLightRow);												
											}
										}

										//if (col.IsTreeColumn)
										//	ctx.SetClip (Region2);

										if (bSelectedCell) {                                    
											RectangleF rSelect = new RectangleF (RectangleCell.Location, RectangleCell.Size);
											DrawSelectionBorder (ctx, rSelect);
										}								
									}	

									if (col.Visible) {
										iColStart += colWidth;
									}
									if (iColStart > bounds.Right)
										break;
								}
							}

							// Selection Rectangle / RowHeader Selection Triangle                                                
							if (RowHeaders && rowIndex == RowIndex && RowHeaderWidth > 0 && !m_PrintingFlag) {
								using (var clipSelection = new ClipBoundClip (ctx, Region1, false)) {
									float iCenter = R.Top + (R.Height / 2f) - 1;
									PointF[] points = new PointF[3];

									float srDX = Bounds.Left + (6f * ScaleFactor).Ceil ();
									float srDY = (4f * ScaleFactor).Ceil ();

									points [0] = new PointF (srDX, iCenter - srDY);
									points [1] = new PointF (srDX, iCenter + srDY);
									points [2] = new PointF (srDX + srDY, iCenter);
									ctx.FillPolygon (Theme.Brushes.Base01, points);
								}						
							}						
						}						
						//LastGroupLevel = item.GroupLevel;					
					}
				}					
			}

			// finalize drawing, complete some vertical lines
			// Pay attention not to draw outside the screen bounds. This will slow down the entire program dramatically.
			//int LastBottomline = RowManager.LastRowBottom + VerticalScrollOffset;
			int LastBottomline = (RowManager.LastRowBottom + VerticalScrollOffset).Ceil();

			ClipRect = new RectangleF(Bounds.X + RowHeaderWidth, Top + HeadHeight - m_HeaderHeight, Bounds.Width, Bounds.Height - HeadHeight + m_HeaderHeight);
			using (var clip = new  ClipBoundClip(ctx, ClipRect)) {
				if (LastBottomline > 0)
					ctx.DrawLine(HeaderBorder, Bounds.X + RowHeaderWidth, Bounds.Top, Bounds.X + RowHeaderWidth, LastBottomline - 1);			

				if (VerticalLines) {
					// Vertical Lines
					float iColStartX = ColumnStartX ();
					foreach (DataGridColumn col in Columns) {
						if (col.Visible) {
							float colWidth = col.AbsoluteWidth (clientRectangle.Width);
							float ColumnDeviderX = iColStartX + colWidth;
							if (m_RowBorderPen.Width > 0 && ColumnDeviderX > Left) {
								ctx.DrawLine (this.m_RowBorderPen, ColumnDeviderX - 0.5f, Top + HeadHeight, ColumnDeviderX, LastBottomline);
							}
							iColStartX += colWidth;
							if (iColStartX > bounds.Right)
								break;
						}
					}
				}

			}

			if (m_MaxColumnWidth != oldMaxColumnWidth) {
				SetupScrollBars ();
			}
		}			

		public virtual void DrawSelectionBorder(IGUIContext ctx, RectangleF rSelect)
		{
		}

		public void EnsureRowindexVisible(int rowIndex)
		{
			if (HasData)
				RowManager.EnsureRowindexVisible (rowIndex);			
		}
			
		protected void StartSelectionTimer()
		{
			//OnCurrentRowChanged ();
			if (!HasData)
				return;
			//RowManager.OnRowsChanged ();
			SelectionManager.StartSelectionTimer ();
		}			
			
		public override void OnDoubleClick (MouseButtonEventArgs e)
		{
			base.OnDoubleClick (e);

			// ToDo: HitTest on Caption, ScrollBars, etc..

			if (RowManager != null && RowManager.CurrentRowIndex >= 0)
				OnItemSelected ();
		}			

		public override bool OnKeyDown (OpenTK.Input.KeyboardKeyEventArgs e)
		{
			if (!IsFocused)
				return false;
			
			if (RowManager == null)
				return base.OnKeyDown (e);

			Root.HideTooltip ();
			
			bool bHandled = false;
			bool bProcess = false;
			bool bEnsureVisible = true;
			bool bSeekRow = false;
			bool bSeekColumn = false;            
			int oldRowIndex = RowIndex;

			switch (e.Key) {
			case Key.Down:
				bHandled = true;
				if (RowManager.CanMoveForward) {
					RowManager.MoveNext ();
					if (ColumnIndex < 0)
						ColumnIndex = 0;
					bSeekRow = true;
					bProcess = true;
				}
				break;							

			case Key.Up:
				bHandled = true;
				if (RowManager.CanMoveBack) {
					RowManager.MovePrevious ();
					bSeekRow = true;
					bProcess = true;
				}
				break;

			case Key.PageUp:
				bHandled = true;
				if (RowManager.CanMoveBack) {
					RowManager.MovePageUp ();
					bSeekRow = true;
					bProcess = true;
				}
				break;
			case Key.PageDown:
				bHandled = true;
				if (RowManager.CanMoveForward) {
					RowManager.MovePageDown ();
					bSeekRow = true;
					bProcess = true;
				}
				break;

			case Key.Left:
				bHandled = true;
				if (SelectionManager.SelectionMode == DataGridSelectionModes.Row)
				{					
					ScrollLeft();
					bEnsureVisible = false;
				}
				else
				{
					SelectPreviousCell ();
					bSeekRow = true;
					bSeekColumn = true;
					bEnsureVisible = true;
				}

				bProcess = true;
				break;

			case Key.Right:
				bHandled = true;
				if (SelectionManager.SelectionMode == DataGridSelectionModes.Row)
				{					
					ScrollRight();
					bEnsureVisible = false;
				}
				else
				{
					SelectNextCell();
					bSeekRow = true;
					bSeekColumn = true;
					bEnsureVisible = true;
				}

				bProcess = true;
				break;

			case Key.Home:
				bHandled = true;
				if (e.Control) {
					RowManager.MoveFirst ();
				} else {
					HScrollBar.Value = 0;
					bSeekColumn = true;
				}
				bSeekRow = true;
				bEnsureVisible = true;
				bProcess = true;
				break;
			case Key.End:
				bHandled = true;
				if (e.Control) {
					RowManager.MoveLast ();
				} else {
					HScrollBar.Value = HScrollBar.Maximum;
					bSeekColumn = true;
				}
				bSeekRow = true;
				bEnsureVisible = true;
				bProcess = true;
				break;
			case Key.Enter:
				bHandled = true;
				if (RowManager != null && RowManager.CurrentRowIndex >= 0 && ItemSelected != null) {
					OnItemSelected ();
					bProcess = true;
				}
				break;
			}				

			if (bProcess)
			{
				if (bEnsureVisible)
				{
					if (SelectionManager.SelectionMode == DataGridSelectionModes.Cell)
						EnsureColumnVisible(ColumnManager.ColumnFromColumnIndex(ColumnIndex));

					//EnsureRowindexVisible(RowIndex);
				}

				if (bSeekRow)
				{					
					if (e.Shift)
					{										
						SelectionManager.SelectRow(oldRowIndex);
						SelectionManager.SelectRow(RowIndex);					
					}
					else
					{   
						SelectionManager.SelectNone();
					}

					this.Invalidate();
					this.StartSelectionTimer();
				}
				else if (bSeekColumn)
				{
					this.Invalidate();
				}
								
				return true;
			}
			else
			{					
				return bHandled;
			}
		}

		public override void OnKeyUp (KeyboardKeyEventArgs e)
		{
			base.OnKeyUp (e);
			Invalidate (1);
			OnSelectionChanged ();
		}

		protected void SetCurrentRowAndCellFromPoint(int x, int y)
		{
			if (y < HeadHeight || !HasData)
				return;

			int rowIndex = RowManager.RowIndexFromPoint(y);
			if (rowIndex >= 0 && rowIndex < RowManager.RowCount)
			{
				RowIndex = rowIndex;
				ColumnIndex = Math.Min(m_MaxColumnIndex, ColumnManager.ColumnIndexFromPoint(x));
			}
		}

		public override void OnMouseDown (MouseButtonEventArgs e)
		{			
			base.OnMouseDown (e);

			if (!HasData || RowManager.RowCount == 0)
				return;

			MouseX = e.X;
			MouseY = e.Y;
			MouseButton = e.Button;

			bool bControlPressed = ModifierKeys.ControlPressed;
			bool bShiftPressed = ModifierKeys.ShiftPressed;

			int lastSelectedRowIndex = this.RowIndex;

			HideTooltip (CurrentCell(e.Position), e.Position);


			//bool bWasFocussed = this.Focused || m_IsEditing;

			MouseControlItem mc = FindControlItem(e.X, e.Y);
			if (mc.ItemType != MouseControlItemTypes.Empty && mc.ItemType != MouseControlItemTypes.Tooltip)
			{
				//if (!bWasFocussed)    // cooler without
				//    Focus();

				// Special area clicked
				switch (mc.ItemType)
				{
				case MouseControlItemTypes.PlusMinus:
					//ToggleCollapse(mc.Tag as IExpandableItem);                        
					return;                        

				case MouseControlItemTypes.ColumnHeaderBorder:
					m_ColumnResizingColumn = (DataGridColumn)mc.Tag;
					m_ColumnResizingXStart = e.X - ScrollOffsetX;
					m_ColumnResizingWidthStart = m_ColumnResizingColumn.AbsoluteWidth(ScrollBounds.Width);
					m_ColumnResizingFlag = true;
					return;                        

				case MouseControlItemTypes.ColumnHeader:
					m_ColumnMovingColumn = (DataGridColumn)mc.Tag;
					return;				
				}
			}

			if (!this.IsFocused && this.CanFocus)
				this.Focus();

			if (e.Button == MouseButton.Left) {
				SetCurrentRowAndCellFromPoint (e.X, e.Y);

				if (bShiftPressed) {
					if (lastSelectedRowIndex < 0)
						lastSelectedRowIndex = 0;
				
					for (int i = Math.Min (lastSelectedRowIndex, RowIndex); i <= Math.Max (lastSelectedRowIndex, RowIndex); i++) {
						SelectionManager.SelectRow (i);
					}
				} else if (bControlPressed) {				
					if (SelectionManager.IsRowSelected (RowIndex) && SelectionManager.SelectedRowsCount > 1) {
						int idx = RowIndex;
						RowIndex = lastSelectedRowIndex;
						SelectionManager.UnselectRow (idx);
					} else
						SelectionManager.SelectRow (RowIndex);
				} else {
					SelectionManager.SelectNone ();
					//SelectionManager.SelectRow (RowIndex);
				}
			}
				
			Invalidate ();
		}			

		Point lastMousePos;
		void HideTooltip(CellInfo cell, Point pos)
		{									
			if ((cell != lastTooltipCell || pos.Distance(lastMousePos) > RowHeight * 2)) {												
				Concurrency.WaitSpinning (3);
				Tooltip = null;
				Root.HideTooltip ();
			}
		}

		CellInfo lastTooltipCell;
		void ShowTooltip(string text, PointF location, CellInfo cell, Point pos)
		{				
			if (cell == lastTooltipCell)
				return;
			lastTooltipCell = cell;
			HideTooltip (cell, pos);
			lastMousePos = pos;
			Tooltip = null;
			Root.ShowTooltip (text, location);
		}

		public override void OnMouseMove (MouseMoveEventArgs e)
		{
			//base.OnMouseMove (e);

			//HideTooltip (CurrentCell(e.Position), e.Position);

			if (m_ColumnMovingFlag)
				return;            

			if (m_ColumnResizingFlag)
			{
				float NewWidth = m_ColumnResizingWidthStart + e.X - m_ColumnResizingXStart - ScrollOffsetX;
				if (NewWidth > m_ColumnResizingColumn.MinWidth)
				{
					if (m_ColumnResizingColumn.AutoMinWidth && NewWidth <= m_ColumnResizingColumn.DesiredWidth)
						NewWidth = m_ColumnResizingColumn.DesiredWidth;

					//m_ColumnResizingColumn.Width = NewWidth;
					m_ColumnResizingColumn.SetWidth(NewWidth, ScrollBounds.Width, Columns);

					this.Invalidate();
				}
				return;
			}            

			// *** Column Moving ***
			if (m_ColumnMovingColumn != null && AllowColumnReorder && (Math.Abs(e.X - MouseX) > 3 || Math.Abs(e.Y - MouseY) > 3))
			{
				// Start Column Dragging
				m_ColumnMovingFlag = true;

				// *** Take column picture ***
				/*************************************************************
				// Reset image list used for drag image
				if (this.m_ImageListDrag == null)
				{
					this.m_ImageListDrag = new ImageList();
					m_ImageListDrag.ColorDepth = System.Windows.Forms.ColorDepth.Depth32Bit;
					m_ImageListDrag.TransparentColor = System.Drawing.Color.Transparent;
				}
				else
					this.m_ImageListDrag.Images.Clear();

				this.m_ImageListDrag.Images.Clear();
				int iDragImageWidth = m_ColumnMovingColumn.ColumnHeaderBounds.Size.Width;

				// shouldn't be bigger than 256 px
				if (iDragImageWidth > 256) iDragImageWidth = 256;
				this.m_ImageListDrag.ImageSize = new Size(iDragImageWidth, m_ColumnMovingColumn.ColumnHeaderBounds.Height);

				using (Bitmap bmp = new Bitmap(m_ColumnMovingColumn.ColumnHeaderBounds.Width, m_ColumnMovingColumn.ColumnHeaderBounds.Height))
				{
					// Get graphics from bitmap
					using (Graphics gfx = Graphics.FromImage(bmp))
					{
						// Draw background
						m_HeaderBackground.FillRectangle(gfx, new Rectangle(0, 0, bmp.Width, bmp.Height));

						using (Pen p = new Pen(m_HeaderForeground.Color))
						{
							gfx.DrawRectangle(Pens.Black, new Rectangle(0, 0, bmp.Width - 1, bmp.Height - 1));
						}

						StringFormat textFormat = new StringFormat();
						textFormat.Trimming = StringTrimming.EllipsisCharacter;
						textFormat.FormatFlags = StringFormatFlags.NoWrap;
						textFormat.LineAlignment = StringAlignment.Center;

						// Draw caption into bitmap
						using (SolidBrush ColumnTextBrush = new SolidBrush(m_HeaderForeground.Color))
						{
							Rectangle RT = new Rectangle(4, 0, bmp.Width - 4, bmp.Height);
							gfx.DrawString(m_ColumnMovingColumn.Caption, m_HeaderForeground.Font, ColumnTextBrush, RT, textFormat);
						}
					}

					// Add bitmap to imagelist
					this.m_ImageListDrag.Images.Add(bmp);

					int dx = MouseX - m_ColumnMovingColumn.ColumnHeaderBounds.Left;
					int dy = MouseY - m_ColumnMovingColumn.ColumnHeaderBounds.Top;

					m_LastAllowDrop = this.AllowDrop;
					this.AllowDrop = true;

					// Begin dragging image
					if (DragHelper.ImageList_BeginDrag(this.m_ImageListDrag.Handle, 0, dx, dy))
					{
						// Begin dragging
						this.DoDragDrop(bmp, DragDropEffects.Move);
						// End dragging image
						DragHelper.ImageList_EndDrag();
						ResetDragMoveStates();
					}
				}
				*******************************/

				return;
			}


			// *** Clickable areas ***
			MouseControlItem mc = FindControlItem(e.X, e.Y);

			switch (mc.ItemType)
			{
			case MouseControlItemTypes.Empty:
				this.Cursor = Cursors.Default;
				LastTooltipBounds = RectangleF.Empty;
				lastTooltipCell = CellInfo.Empty;
				break;
			case MouseControlItemTypes.PlusMinus:
				//this.Cursor = Cursors.Hand;
				break;

			case MouseControlItemTypes.ColumnHeader:
				if (AllowSort) {
					//DataGridColumn col = ColumnFromPoint (e.X);
					/***
					if (col != null && col.AllowSort)
						this.Cursor = Cursors.Hand;
					else
						this.Cursor = Cursors.Default;
					***/
				} else {
					this.Cursor = Cursors.Default;					
				}
				break;

			case MouseControlItemTypes.ColumnHeaderBorder:
				this.Cursor = Cursors.VSplit;
				break;

			case MouseControlItemTypes.Tooltip:				
				if (!CellToolTips || m_IsEditing || mc.Bounds == LastTooltipBounds) {					
					return;	
				}
				LastTooltipBounds = mc.Bounds;
				ShowTooltip(mc.Tag.SafeString(), mc.Bounds.Location, CurrentCell(mc.Bounds.Location), e.Position);
				break;
			}
				
			base.OnMouseMove (e);
		}

		public override bool OnMouseWheel (MouseWheelEventArgs e)
		{
			HideTooltip (CurrentCell(e.Position), e.Position);
			return base.OnMouseWheel (e);
		}

		public CellInfo CurrentCell (PointF p)
		{			
			return CurrentCell (p.ToPoint());
		}

		public CellInfo CurrentCell (Point p)
		{			
			if (!HasData)
				return CellInfo.Empty;
			return new CellInfo (RowManager.RowIndexFromPoint(p.Y), ColumnManager.ColumnIndexFromPoint(p.X));
		}

		public override void OnClick (MouseButtonEventArgs e)
		{
			if (!m_ColumnResizingFlag && !m_ColumnMovingFlag && e.Button == MouseButton.Left)
			{
				MouseControlItem itm = FindControlItem(e.X, e.Y);
				//if (itm != null && itm.ItemType == MouseControlItemTypes.ColumnHeader)
				if (itm.ItemType == MouseControlItemTypes.ColumnHeader)
				{
					DataGridColumn col = itm.Tag as DataGridColumn;

					// Sort for this column                    
					if (col != null && col.AllowSort)
					{
						// TODO:
						bool bControlPressed = ModifierKeys.ControlPressed;
						if (!bControlPressed)
						{
							Columns.Where(t => t != col).ForEach(t => t.SortDirection = SortDirections.None);
						}

						switch (col.SortDirection)
						{
						case SortDirections.None:
							col.SortDirection = SortDirections.Ascending;
							break;

						case SortDirections.Ascending:
							if (bControlPressed)
								col.SortDirection = SortDirections.None;
							else
								col.SortDirection = SortDirections.Descending;
							break;

						case SortDirections.Descending:
							if (bControlPressed)
								col.SortDirection = SortDirections.None;
							else
								//col.SortDirection = GanttSortDirections.Ascending;
								col.SortDirection = SortDirections.None;
							break;
						}
					}

					if (m_IsEditing)
						this.EndEdit(true);
					else
						ApplySort();
				}
			}

			base.OnClick (e);
		}

		public override void OnMouseUp (MouseButtonEventArgs e)
		{
			base.OnMouseUp (e);
			// Sorting applied here

			ResetDragMoveStates();
			this.Invalidate();
			OnSelectionChanged ();
		}

		protected void ResetDragMoveStates()
		{
			m_ColumnResizingFlag = false;
			m_ColumnMovingFlag = false;

			m_ColumnResizingColumn = null;
			m_ColumnMovingColumn = null;
			m_LastDragHoverColumn = null;

			/**
			if (m_ImageListDrag != null)
				m_ImageListDrag.Images.Clear();

			m_IsDragging = false;
			this.AllowDrop = m_LastAllowDrop;
			**/
		}

		public virtual void ApplySort()
		{
			DataProvider.ApplySort ();
		}

		protected override void CleanupManagedResources ()
		{
			RowManager = null;
			ColumnManager = null;
			SelectionManager = null;

			base.CleanupManagedResources ();
		}
	}		
}

