using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using KS.Foundation;
using System.Drawing;
using SummerGUI;

namespace SummerGUI.DataGrid
{
    public class VisibleColumnsDialog : ChildFormWindow
    {
        DataGridColumnCollection m_Columns;

        Panel m_buttonContainer;
        //ButtonContainer m_buttonContainer;

        public DefaultButton CmdOK { get; private set; }
        public DefaultButton CmdCancel { get; private set; }

        private DefaultButton CmdMoveUp { get; set; }
        private DefaultButton CmdMoveDown { get; set; }

        private CheckedListBox LB { get; set; }
        

        public VisibleColumnsDialog(SummerGUIWindow parent, DataGridColumnCollection columns)
            : base ("VisibleColumnsDialog", "Visible Columns", 340, 240, parent, modal: true, sizable: true)
        {
            m_Columns = columns;

            m_buttonContainer = Controls.AddChild(new Panel("m_buttonContainer", Docking.Right, null));
            m_buttonContainer.Padding = new Padding(8);
            
            //m_buttonContainer = Controls.AddChild(new ButtonContainer("m_buttonContainer", Docking.Right, null));
            //m_buttonContainer.FlexDirection = FlexDirections.Column;            
            //m_buttonContainer.ItemDistance = 8;            

            CmdOK = m_buttonContainer.AddChild(new DefaultButton("cmdOK", "&OK", ColorContexts.Default));
            CmdCancel = m_buttonContainer.AddChild(new DefaultButton("cmdCancel", "C&ancel", ColorContexts.Default));

            CmdOK.Dock = Docking.Top;
            CmdCancel.Dock = Docking.Top;            
            
            CmdOK.Click += (object sender, MouseButtonEventArgs args) => this.OnOK();
            CmdCancel.Click += (object sender, MouseButtonEventArgs args) => this.OnCancel();

            CmdMoveDown = m_buttonContainer.AddChild(new DefaultButton("cmdMoveDown", "Move Down", (char)FontAwesomeIcons.fa_arrow_circle_down));
            CmdMoveUp = m_buttonContainer.AddChild(new DefaultButton("cmdMoveUp", "Move Up", (char)FontAwesomeIcons.fa_arrow_circle_up));            

            CmdMoveUp.Dock = Docking.Bottom;
            CmdMoveDown.Dock = Docking.Bottom;


            CmdMoveUp.IsAutofire = true;
            CmdMoveUp.Fire += MoveUp;

            CmdMoveDown.IsAutofire = true;
            CmdMoveDown.Fire += MoveDown;

            foreach(var child in m_buttonContainer.Children)
                child.Margin = new Padding(4);                

            m_buttonContainer.Padding = new Padding(0, 6, 6, 6);            

            LB = Controls.AddChild(new CheckedListBox("Columns"));
            LB.Margin = new Padding(6);
            LB.SelectionChanged += SelectionChanged;
        }        

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            
            var orderedCols = m_Columns.OrderBy(col => col.Position).ToArray();
            
            for (int i = 0; i < orderedCols.Length; i++)
            {
                var col = orderedCols[i];
                var item = LB.Items.AddUnsorted(col.Text, col.Key);
                item.Checked = col.Visible;
            }

            LB.SelectedIndex = 0;
            LB.Focus();
            EnableControls();
        }

        public override void OnOK()
        {            
            for (int i = 0; i < LB.Items.Count; i++)
            {
                var item = LB.Items[i];
                var col = m_Columns.First(col => col.Key == (string)item.Value);                
                col.Visible = item.Checked;
                col.Position = i;
            }

            base.OnOK();
        }

        void MoveUp(object sender, EventArgs args)
        {
            int idx = LB.SelectedIndex;
            if (idx <= 0 || LB.Count == 0)
                return; 

            var item = LB.Items[idx];

            LB.Items.RemoveAt(idx);
            LB.Items.Insert(idx - 1, item);
            LB.SelectedIndex--;
            LB.EnsureIndexVisible(LB.SelectedIndex);            
            
            EnableControls();
            Invalidate();            
        }

        void MoveDown(object sender, EventArgs args)
        {
            int idx = LB.SelectedIndex;
            if (idx >= LB.Count - 1 || LB.Count == 0)
                return;

            var item = LB.Items[idx];

            LB.Items.RemoveAt(idx);
            LB.Items.Insert(idx + 1, item);
            LB.SelectedIndex++;
            LB.EnsureIndexVisible(LB.SelectedIndex);            
            
            EnableControls();
            Invalidate();
        }

        void SelectionChanged(object sender, EventArgs args)
        {
            EnableControls();
        }

        void EnableControls()
        {
            CmdMoveUp.Enabled = LB.SelectedIndex > 0;
            CmdMoveDown.Enabled = LB.SelectedIndex < LB.Count - 1;
        }

        protected override void Dispose(bool manual)
        {
            m_Columns = null;
            base.Dispose(manual);
        }
    }
}
