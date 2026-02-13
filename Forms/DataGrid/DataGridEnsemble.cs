using System;
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
	public class DataGridEnsemble : Container
	{
		public DataGridView DataGrid { get; private set; }
		public DataGridToolBar ToolBar { get; private set; }

		public DataGridSearchBar SearchBar { get; private set; }

		public DataGridEnsemble (string name)
			: base(name)
		{
			ToolBar = AddChild (new DataGridToolBar (name + "Toolbar", null));
			DataGrid = AddChild (new DataGridView (name));

			SearchBar = AddChild (new DataGridSearchBar (name + "SearchBar", DataGrid, "Search for:", Docking.Top));
			SearchBar.Visible = false;

			ToolBar.CmdFind.Click += CmdFind_Click;

			DataGrid.DataLoaded += (sender, e) => EnableControls ();
			ToolBar.ActiveDataGrid = DataGrid;
		}
						
		public void EnableControls()
		{
			CanFocus = DataGrid.HasData;
			ToolBar.OnDataLoaded ();
		}

		void CmdFind_Click (object sender, EventArgs e)
		{
			SearchBar.Visible = true;
			SearchBar.Focus();
		}

		public override void Focus ()
		{
			DataGrid.Focus ();
		}
	}
}

