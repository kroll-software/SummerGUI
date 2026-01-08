using System;
using System.ComponentModel;
using System.Security.Permissions;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using KS.Foundation;

namespace SummerGUI.DataGrid
{
	public class DataGridToolBar : ComponentToolBar, IObserver<EventMessage>
	{		
		public DataGridView ActiveDataGrid { get; set; }

		public ToolBarButton CmdFirst { get; private set; }
		public ToolBarButton CmdPrev { get; private set; }
		public ToolBarButton CmdNext { get; private set; }
		public ToolBarButton CmdLast { get; private set; }
		public ToolBarButton CmdNew { get; private set; }
		public ToolBarButton CmdDelete { get; private set; }
		public ToolBarButton CmdEdit { get; private set; }
		public ToolBarLabel LblRecordCount { get; private set; }

		public IRowManager RowManager { get; private set; }

		public DataGridToolBar (string name, IGuiMenu menu)
			: base(name, menu)
		{
			CmdFirst = AddChild(new ToolBarButton("first", "First Record", (char)FontAwesomeIcons.fa_fast_backward, 
				ButtonDisplayStyles.Image, Theme.Colors.Blue));			
			CmdPrev = AddChild(new ToolBarButton("prev", "Previous Record", (char)FontAwesomeIcons.fa_backward,
				ButtonDisplayStyles.Image, Theme.Colors.Blue));			
			CmdNext = AddChild(new ToolBarButton("next", "Next Record", (char)FontAwesomeIcons.fa_forward,
				ButtonDisplayStyles.Image, Theme.Colors.Blue));
			CmdLast = AddChild(new ToolBarButton("last", "Last Record", (char)FontAwesomeIcons.fa_fast_forward,
				ButtonDisplayStyles.Image, Theme.Colors.Blue));

			AddChild(new ToolBarSeparator("separator1", new ComponentToolBarSeparatorStyle()));

			CmdNew = AddChild(new ToolBarButton("new", "New..", (char)FontAwesomeIcons.fa_flash,
				ButtonDisplayStyles.ImageAndText, Theme.Colors.Yellow));			
			CmdEdit = AddChild(new ToolBarButton("edit", "Edit..", (char)FontAwesomeIcons.fa_wpforms,
				ButtonDisplayStyles.ImageAndText, Theme.Colors.Green));
			CmdDelete = AddChild(new ToolBarButton("delete", "Delete", (char)FontAwesomeIcons.fa_remove,
				ButtonDisplayStyles.ImageAndText, Theme.Colors.Red));			

			LblRecordCount = AddChild (new ToolBarLabel ("recordcount", "- / -"));
			LblRecordCount.Dock = Docking.Right;

			CmdPrev.IsAutofire = true;
			CmdNext.IsAutofire = true;

			CmdFirst.Click += CmdFirst_Click;
			CmdPrev.Fire += CmdPrev_Fire;
			CmdNext.Fire += CmdNext_Fire;
			CmdLast.Click +=  CmdLast_Click;

			CmdNew.Click += CmdNew_Click;
			CmdEdit.Click += CmdEdit_Click;
			CmdDelete.Click += CmdDelete_Click;

			// ***********
			//RowManagerObserver = new Observable<EventMessage>.Observer (OnNext, OnError, OnCompleted);

			Enabled = false;			
		}
			
		void CmdFirst_Click (object sender, EventArgs e)
		{
			RowManager.MoveFirst ();
			ActiveDataGrid.Focus ();
		}			

		void CmdPrev_Fire (object sender, EventArgs e)
		{
			RowManager.MovePrevious();
			ActiveDataGrid.Focus ();
			Invalidate ();
		}

		void CmdNext_Fire (object sender, EventArgs e)
		{
			RowManager.MoveNext();
			ActiveDataGrid.Focus ();
			Invalidate ();
		}

		void CmdLast_Click (object sender, EventArgs e)
		{
			RowManager.MoveLast();
			ActiveDataGrid.Focus ();
		}

		void CmdDelete_Click (object sender, EventArgs e)
		{
			if (ActiveDataGrid == null || ActiveDataGrid.RowManager == null)
				return;
			ActiveDataGrid.OnDeleteItem ();
			ActiveDataGrid.Focus ();
		}

		void CmdEdit_Click (object sender, EventArgs e)
		{
			if (ActiveDataGrid == null)
				return;
			ActiveDataGrid.OnItemSelected ();
			ActiveDataGrid.Focus ();
		}

		void CmdNew_Click (object sender, EventArgs e)
		{
			if (ActiveDataGrid == null)
				return;
			ActiveDataGrid.OnNewItem();
			ActiveDataGrid.Focus ();
		}
			
		public void UpdateRecordCount(int current, int total)
		{			
			LblRecordCount.Text = String.Format("{0} / {1}", current >= 0 ? (current + 1).ToString("n0") : "-", 
				total > 0 ? total.ToString("n0") : "-");
		}			

		private IDisposable subscription;
		public void SetRowManager(IRowManager manager)
		{
			RowManager = manager;
			subscription = RowManager.Subscribe (this);
		}

		public void OnDataLoaded()
		{
			Enabled = RowManager != null && RowManager.RowCount > 0;
			if (Enabled)
				EnableControls (RowManager.CurrentRowIndex);
		}

		public void OnNext(EventMessage message) 
		{			
			switch (message.Subject) {
			case RowManagerMessages.RowsChanged:
				int current = (int)message.Args [0];
				int total = (int)message.Args [1];
				UpdateRecordCount (current, total);
				EnableControls (current);
				break;
			}
		}			

		public void OnError(Exception ex) 
		{
		}

		public void OnCompleted() 
		{
		}
			
		public override bool OnKeyDown (KeyboardKeyEventArgs e)
		{
			if (base.OnKeyDown (e))
				return true;
			switch (e.Key) {
			case Keys.Delete:
				CmdDelete.OnClick ();
				return true;
			}
			return false;
		}
			
		private void EnableControls(int rowIndex)
		{			
			if (!Enabled)
				return;
			bool hasRow = rowIndex >= 0;
			CmdEdit.Enabled = hasRow;
			CmdDelete.Enabled = hasRow;
			CmdFirst.Enabled = RowManager.CanMoveBack;
			CmdPrev.Enabled = RowManager.CanMoveBack;
			CmdNext.Enabled = RowManager.CanMoveForward;
			CmdLast.Enabled = RowManager.CanMoveForward;
		}			
			
		protected override void CleanupUnmanagedResources ()
		{			
			if (subscription != null)
				subscription.Dispose ();
			ActiveDataGrid = null;
			base.CleanupUnmanagedResources ();
		}
	}
}

