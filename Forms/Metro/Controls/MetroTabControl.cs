/**
 * MetroFramework - Modern UI for WinForms
 * 
 * The MIT License (MIT)
 * Copyright (c) 2011 Sven Walter, http://github.com/viperneo
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy of 
 * this software and associated documentation files (the "Software"), to deal in the 
 * Software without restriction, including without limitation the rights to use, copy, 
 * modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, 
 * and to permit persons to whom the Software is furnished to do so, subject to the 
 * following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in 
 * all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, 
 * INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A 
 * PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
 * CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE 
 * OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

// Based on original work by 
// (c) Mick Doherty / Oscar Londono
// http://dotnetrix.co.uk/tabcontrol.htm
// http://www.pcreview.co.uk/forums/adding-custom-tabpages-design-time-t2904262.html
// http://www.codeproject.com/Articles/12185/A-NET-Flat-TabControl-CustomDraw
// http://www.codeproject.com/Articles/278/Fully-owner-drawn-tab-control

using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Design;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Windows.Forms;
using System.Drawing.Imaging;

using MetroFramework.Components;
using MetroFramework.Drawing;
using MetroFramework.Interfaces;
using MetroFramework.Native;

using System.Linq;

namespace MetroFramework.Controls
{
    #region MetroTabPageCollection

    [ToolboxItem(false)]
    [Editor("MetroFramework.Design.MetroTabPageCollectionEditor, " + AssemblyRef.MetroFrameworkDesignSN, typeof(UITypeEditor))]
    public class MetroTabPageCollection : TabControl.TabPageCollection
    {
        public MetroTabPageCollection(MetroTabControl owner) : base(owner)
        { }
    }

    #endregion

    [Designer("MetroFramework.Design.Controls.MetroTabControlDesigner, " + AssemblyRef.MetroFrameworkDesignSN)]
    [ToolboxBitmap(typeof(TabControl))]
    public class MetroTabControl : TabControl, IMetroControl
    {
        #region Interface

        [Category(MetroDefaults.PropertyCategory.Appearance)]
        public event EventHandler<MetroPaintEventArgs> CustomPaintBackground;
        protected virtual void OnCustomPaintBackground(MetroPaintEventArgs e)
        {
            if (GetStyle(ControlStyles.UserPaint) && CustomPaintBackground != null)
            {
                CustomPaintBackground(this, e);
            }
        }

        [Category(MetroDefaults.PropertyCategory.Appearance)]
        public event EventHandler<MetroPaintEventArgs> CustomPaint;
        protected virtual void OnCustomPaint(MetroPaintEventArgs e)
        {
            if (GetStyle(ControlStyles.UserPaint) && CustomPaint != null)
            {
                CustomPaint(this, e);
            }
        }

        [Category(MetroDefaults.PropertyCategory.Appearance)]
        public event EventHandler<MetroPaintEventArgs> CustomPaintForeground;
        protected virtual void OnCustomPaintForeground(MetroPaintEventArgs e)
        {
            if (GetStyle(ControlStyles.UserPaint) && CustomPaintForeground != null)
            {
                CustomPaintForeground(this, e);
            }
        }

        private MetroColorStyle metroStyle = MetroColorStyle.Default;
        [Category(MetroDefaults.PropertyCategory.Appearance)]
        [DefaultValue(MetroColorStyle.Default)]
        public MetroColorStyle Style
        {
            get
            {
                if (DesignMode || metroStyle != MetroColorStyle.Default)
                {
                    return metroStyle;
                }

                if (StyleManager != null && metroStyle == MetroColorStyle.Default)
                {
                    return StyleManager.Style;
                }
                if (StyleManager == null && metroStyle == MetroColorStyle.Default)
                {
                    return MetroDefaults.Style;
                }

                return metroStyle;
            }
            set { metroStyle = value; }
        }

        private MetroThemeStyle metroTheme = MetroThemeStyle.Default;
        [Category(MetroDefaults.PropertyCategory.Appearance)]
        [DefaultValue(MetroThemeStyle.Default)]
        public MetroThemeStyle Theme
        {
            get
            {
                if (DesignMode || metroTheme != MetroThemeStyle.Default)
                {
                    return metroTheme;
                }

                if (StyleManager != null && metroTheme == MetroThemeStyle.Default)
                {
                    return StyleManager.Theme;
                }
                if (StyleManager == null && metroTheme == MetroThemeStyle.Default)
                {
                    return MetroDefaults.Theme;
                }

                return metroTheme;
            }
            set { metroTheme = value; }
        }

        private MetroStyleManager metroStyleManager = null;
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public MetroStyleManager StyleManager
        {
            get { return metroStyleManager; }
            set { metroStyleManager = value; }
        }

        private bool useCustomBackColor = false;
        [DefaultValue(false)]
        [Category(MetroDefaults.PropertyCategory.Appearance)]
        public bool UseCustomBackColor
        {
            get { return useCustomBackColor; }
            set { useCustomBackColor = value; }
        }

        private bool useCustomForeColor = false;
        [DefaultValue(false)]
        [Category(MetroDefaults.PropertyCategory.Appearance)]
        public bool UseCustomForeColor
        {
            get { return useCustomForeColor; }
            set { useCustomForeColor = value; }
        }

        private bool useStyleColors = false;
        [DefaultValue(false)]
        [Category(MetroDefaults.PropertyCategory.Appearance)]
        public bool UseStyleColors
        {
            get { return useStyleColors; }
            set { useStyleColors = value; }
        }

        [Browsable(false)]
        [Category(MetroDefaults.PropertyCategory.Behaviour)]
        [DefaultValue(false)]
        public bool UseSelectable
        {
            get { return GetStyle(ControlStyles.Selectable); }
            set { SetStyle(ControlStyles.Selectable, value); }
        }

        #endregion

        #region Fields

        private SubClass scUpDown = null;
        private bool bUpDown = false;

        private const int TabBottomBorderHeight = 3;

        private MetroTabControlSize metroLabelSize = MetroTabControlSize.Medium;
        [DefaultValue(MetroTabControlSize.Medium)]
        [Category(MetroDefaults.PropertyCategory.Appearance)]
        public MetroTabControlSize FontSize
        {
            get { return metroLabelSize; }
            set { metroLabelSize = value; }
        }

        private MetroTabControlWeight metroLabelWeight = MetroTabControlWeight.Light;
        [DefaultValue(MetroTabControlWeight.Light)]
        [Category(MetroDefaults.PropertyCategory.Appearance)]
        public MetroTabControlWeight FontWeight
        {
            get { return metroLabelWeight; }
            set { metroLabelWeight = value; }
        }

        private ContentAlignment textAlign = ContentAlignment.MiddleLeft;
        [DefaultValue(ContentAlignment.MiddleLeft)]
        [Category(MetroDefaults.PropertyCategory.Appearance)]
        public ContentAlignment TextAlign
        {
            get
            {
                return textAlign;
            }
            set
            {
                textAlign = value;
            }
        }

        [Editor("MetroFramework.Design.MetroTabPageCollectionEditor, " + AssemblyRef.MetroFrameworkDesignSN, typeof(UITypeEditor))]
        public new TabPageCollection TabPages
        {
            get
            {
                return base.TabPages;
            }
        }


        private bool isMirrored;
        [DefaultValue(false)]
        [Category(MetroDefaults.PropertyCategory.Appearance)]
        public new bool IsMirrored
        {
            get
            {
                return isMirrored;
            }
            set
            {
                if (isMirrored == value)
                {
                    return;
                }
                isMirrored = value;
                UpdateStyles();
            }
        }

        #endregion

        #region Constructor

        public MetroTabControl()
        {
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.SupportsTransparentBackColor, true);

            //Padding = new Point(6, 8);            
            Padding = new Point(11, 8); // this has huge impact. why ?
        }                

        #endregion

        #region Paint Methods

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            try
            {
                Color backColor = BackColor;

                if (!useCustomBackColor)
                {
                    backColor = MetroPaint.BackColor.Form(Theme);
                }

                if (backColor.A == 255 && BackgroundImage == null)
                {
                    e.Graphics.Clear(backColor);                    
                    return;
                }

                base.OnPaintBackground(e);

                OnCustomPaintBackground(new MetroPaintEventArgs(backColor, Color.Empty, e.Graphics));
            }
            catch
            {
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            try
            {
                if (GetStyle(ControlStyles.AllPaintingInWmPaint))
                {
                    OnPaintBackground(e);
                }

                OnCustomPaint(new MetroPaintEventArgs(Color.Empty, Color.Empty, e.Graphics));
                OnPaintForeground(e);
            }
            catch
            {
                Invalidate();
            }
        }

        protected virtual void OnPaintForeground(PaintEventArgs e)
        {
            for (var index = 0; index < TabPages.Count; index++)
            {
                if (index != SelectedIndex)
                {
                    DrawTab(index, e.Graphics);
                }
            }
            if (SelectedIndex <= -1)
            {
                return;
            }

            DrawTabBottomBorder(SelectedIndex, e.Graphics);            
            DrawTab(SelectedIndex, e.Graphics);
            DrawTabSelected(SelectedIndex, e.Graphics);

            if (TabPages.Count == 0)
                return;

            Rectangle r = GetTabRect(TabPages.Count - 1);
            Rectangle r2 = new Rectangle(r.Right, r.Top, (ClientRectangle.Right - TabPages[0].Margin.Right) - r.Right, r.Height);
            DrawGlassEffect(r2, e.Graphics, false);

            OnCustomPaintForeground(new MetroPaintEventArgs(Color.Empty, Color.Empty, e.Graphics));
        }        

        private Size MeasureText(string text)
        {
            Size preferredSize;
            using (Graphics g = CreateGraphics())
            {
                Size proposedSize = new Size(int.MaxValue, int.MaxValue);
                preferredSize = TextRenderer.MeasureText(g, text, MetroFonts.TabControl(metroLabelSize, metroLabelWeight),
                                                         proposedSize,
                                                         MetroPaint.GetTextFormatFlags(TextAlign) |
                                                         TextFormatFlags.NoPadding);
            }            

            return preferredSize;
        }

        private int m_LastHoverIndex = -1;
        protected override void OnMouseMove(MouseEventArgs e)
        {            
            for (int i = 0; i < TabPages.Count; i++)
            {
                if (GetTabRect(i).Contains(new Point(e.X, e.Y)))
                {
                    this.Cursor = Cursors.Hand;

                    if (m_LastHoverIndex >= 0 && m_LastHoverIndex < TabPages.Count)
                    {                        
                        TabPages[m_LastHoverIndex].Invalidate();
                    }

                    m_LastHoverIndex = i;
                    TabPages[i].Invalidate();
                    
                    return;
                }
            }

            m_LastHoverIndex = -1;
            this.Cursor = Cursors.Default;

            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            this.Cursor = Cursors.Default;

            if (m_LastHoverIndex >= 0 && m_LastHoverIndex < TabPages.Count)            
                TabPages[m_LastHoverIndex].Invalidate();            

            m_LastHoverIndex = -1;

            this.Invalidate();
        }


        // ********************* BOTTOM BORDER **************************

        private void DrawTabBottomBorder(int index, Graphics graphics)
        {
            Rectangle borderRectangle = new Rectangle(DisplayRectangle.X, GetTabRect(index).Bottom + 2 - TabBottomBorderHeight, DisplayRectangle.Width, TabBottomBorderHeight);

            //graphics.DrawLine(Pens.MidnightBlue, borderRectangle.Left, borderRectangle.Top + 2, borderRectangle.Right, borderRectangle.Top + 2);

            //using (Pen pen = new Pen(Color.FromArgb(255, Color.PaleTurquoise)))
            //    graphics.DrawLine(pen, ClientRectangle.Left, 0, ClientRectangle.Right, 0);

            //graphics.DrawLine(Pens.Gray, ClientRectangle.Left, -1, ClientRectangle.Right, -1);
            graphics.DrawLine(Pens.Silver, ClientRectangle.Left, 0, ClientRectangle.Right, 0);

            //graphics.DrawLine(Pens.DimGray, ClientRectangle.Left, borderRectangle.Top - 1, ClientRectangle.Right, borderRectangle.Top - 1);            

            //using (Brush bgBrush = new SolidBrush(MetroPaint.BorderColor.TabControl.Normal(Theme)))
            //{                
            //    graphics.FillRectangle(bgBrush, borderRectangle);   // this paints the ugly gray
            //}

            using (Brush bgBrush = new SolidBrush(Color.Silver))
            {
                graphics.FillRectangle(bgBrush, borderRectangle);   // this paints the ugly gray
            }

            return;

            borderRectangle.Offset(0, 1);
            borderRectangle.Height += 0;

            using (LinearGradientBrush lgb = new LinearGradientBrush(borderRectangle, Color.Silver, Color.DarkGray, LinearGradientMode.Vertical))
            {
                graphics.FillRectangle(lgb, borderRectangle);

                //bgHalf.Inflate(-1, -1);
                //using (Pen pen = new Pen(lgb, 1))
                //{
                //    graphics.DrawLine(pen, bgHalf.Left, bgHalf.Top, bgHalf.Left, bgHalf.Bottom);
                //    graphics.DrawLine(pen, bgHalf.Right, bgHalf.Top, bgHalf.Right, bgHalf.Bottom);
                //}
            }
        }

        // ********************* SELECTION BORDER (ORANGE) **************************
        
        private void DrawTabSelected(int index, Graphics graphics)
        {
            //return;

            using (Brush selectionBrush = new SolidBrush(MetroPaint.GetStyleColor(Style)))
            {
                // ToDo: tab auch in dunkelblau oder orange
                Rectangle selectedTabRect = GetTabRect(index);
                //Rectangle borderRectangle = new Rectangle(selectedTabRect.X + ((index == 0) ? 2 : 0), GetTabRect(index).Bottom + 2 - TabBottomBorderHeight, selectedTabRect.Width + ((index == 0) ? 0 : 2), TabBottomBorderHeight);
                //Rectangle borderRectangle = new Rectangle(selectedTabRect.X + ((index == 0) ? 2 : 0), GetTabRect(index).Bottom + 2 - TabBottomBorderHeight, selectedTabRect.Width + ((index == 0) ? -2 : -2), TabBottomBorderHeight);
                //Rectangle borderRectangle = new Rectangle(selectedTabRect.X + ((index == 0) ? 2 : -2), GetTabRect(index).Bottom + 2 - TabBottomBorderHeight, selectedTabRect.Width + 2, TabBottomBorderHeight);
                Rectangle borderRectangle = new Rectangle(selectedTabRect.X + 1, GetTabRect(index).Bottom + 2 - TabBottomBorderHeight, selectedTabRect.Width - 1, TabBottomBorderHeight);
                graphics.FillRectangle(selectionBrush, borderRectangle);

                //borderRectangle.Offset(0, -borderRectangle.Top);
                //graphics.FillRectangle(selectionBrush, borderRectangle);
            }
        }

        // (2) ********************* GLASS EFFECT **************************

        private void DrawGlassEffect(Rectangle tabRect, Graphics graphics, bool bselected)
        {
            Rectangle rect = new Rectangle(tabRect.Location, tabRect.Size);

            rect.Width -= 1;

            rect.Y = 2;
            rect.Height += 2;

            using (LinearGradientBrush lgb = new LinearGradientBrush(rect, Color.FromArgb(128, Color.GhostWhite), Color.Transparent, LinearGradientMode.Vertical))
            {
                graphics.FillRectangle(lgb, rect);

                //if (!bselected)
                //{
                    //rect.Inflate(-1, -1);
                    using (Pen pen = new Pen(lgb, 1))
                    {
                        graphics.DrawLine(pen, rect.Left, rect.Top, rect.Left, rect.Bottom - 3);
                        //graphics.DrawLine(pen, rect.Right, rect.Top, rect.Right, rect.Bottom);
                    }

                    graphics.DrawLine(Pens.Black, rect.Right, rect.Top, rect.Right, rect.Bottom - 3);
                //}
            }            
        }

        // (1) ********************* TAB **************************                

        private void DrawTab(int index, Graphics graphics)
        {
            ////graphics.CompositingMode = CompositingMode.SourceOver;
            //graphics.CompositingQuality = CompositingQuality.;
            //graphics.InterpolationMode = InterpolationMode.HighQualityBilinear;

            //if (!Enabled)
            //{
            //    graphics.CompositingMode = CompositingMode.SourceOver;
            //    graphics.CompositingQuality = CompositingQuality.HighQuality;
            //    graphics.InterpolationMode = InterpolationMode.HighQualityBilinear;
            //}
            
            graphics.SmoothingMode = SmoothingMode.HighQuality;            
            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;


            Color foreColor;
            Color backColor = BackColor;

            if (!useCustomBackColor)
            {
                backColor = MetroPaint.BackColor.Form(Theme);
            }

            TabPage tabPage = TabPages[index];
            Rectangle tabRect = GetTabRect(index);

            int iTeszt = tabPage.Margin.Left;
            tabPage.Margin = new System.Windows.Forms.Padding(0);

            bool bSelected = index == SelectedIndex;
            bool bMouseOver = m_LastHoverIndex == index;

            if (!Enabled)
            {
                //foreColor = MetroPaint.ForeColor.Label.Disabled(Theme);
                foreColor = Color.Gray;
                //backColor = Color.Black;
            }
            else
            {
                if (useCustomForeColor)
                {
                    foreColor = DefaultForeColor;
                }
                else
                {
                    foreColor = !useStyleColors ? MetroPaint.ForeColor.TabControl.Normal(Theme) : MetroPaint.GetStyleColor(Style);
                }
            }

            if (index == 0)
            {
                tabRect.X = DisplayRectangle.X;
            }

            Rectangle bgRect = tabRect;
            bgRect.Height -= 3;

            if (index == 0)
            {
                //int delta = tabRect.Left;
                //tabRect.Offset(-delta, 0);
                //bgRect.Width += delta;
                bgRect.Width--;
                bgRect.Width--;
            }

            

            using (Brush bgBrush = new SolidBrush(backColor))
            {
                graphics.FillRectangle(bgBrush, bgRect);
            }            
            
            //if (false & (bMouseOver || bSelected))
            if (bMouseOver || bSelected)
            {                                

                Color color = MetroPaint.GetStyleColor(Style);

                if (bSelected)  // draw some kind of hot
                {
                    int halfHeight = bgRect.Height / 2;
                    Rectangle bgHalf = new Rectangle(bgRect.Location, bgRect.Size);
                    bgHalf.Height = halfHeight - 2;
                    bgHalf.Offset(0, halfHeight);
                    bgHalf.Inflate(-1, -1);
                    using (LinearGradientBrush lgb = new LinearGradientBrush(bgHalf, Color.Transparent, Color.FromArgb(80, color), LinearGradientMode.Vertical))
                    {
                        //graphics.FillRectangle(lgb, bgHalf);
                        graphics.FillRectangle(lgb, bgHalf);

                        bgHalf.Inflate(-1, -1);
                        using (Pen pen = new Pen(lgb, 1))
                        {
                            graphics.DrawLine(pen, bgHalf.Left, bgHalf.Top, bgHalf.Left, bgHalf.Bottom);
                            //graphics.DrawLine(pen, bgHalf.Right, bgHalf.Top, bgHalf.Right, bgHalf.Bottom);
                            //graphics.DrawLine(pen, bgHalf.Right - 1, bgHalf.Top, bgHalf.Right - 1, bgHalf.Bottom);
                            //graphics.DrawLine(pen, bgHalf.Right - 1, bgRect.Top, bgHalf.Right - 1, bgHalf.Top);
                        }


                        //graphics.DrawLine(Pens.Black, bgRect.Right - 1, bgRect.Top, bgRect.Right - 1, bgRect.Bottom);                        
                    }
                }

                //if (bMouseOver)
                    

                //if (bMouseOver)  // draw even hotter
                //{
                //    Rectangle rect = new Rectangle(tabRect.Location, tabRect.Size);
                //    rect.Width -= 1;

                //    using (LinearGradientBrush lgb = new LinearGradientBrush(rect, Color.FromArgb(128, Color.GhostWhite), Color.Transparent, LinearGradientMode.Vertical))
                //    {
                //        graphics.FillRectangle(lgb, rect);                        
                //    }
                //}                
            }            

            // Kroll: bring back image support
            if (this.ImageList != null)
            {
                Image img = null;
                if (TabPages[index].ImageIndex >= 0 && TabPages[index].ImageIndex < this.ImageList.Images.Count)                
                {
                    img = this.ImageList.Images[TabPages[index].ImageIndex];
                }
                else if (!String.IsNullOrEmpty(TabPages[index].ImageKey) && this.ImageList.Images.ContainsKey(TabPages[index].ImageKey))
                {
                    img = this.ImageList.Images[TabPages[index].ImageKey];
                }

                if (img != null)
                {
                    if (this.Enabled)
                    {
                        graphics.DrawImage(img, tabRect.Left + 8, ((tabRect.Height - img.Height) / 2));
                        int offset = img.Width + 12;
                        tabRect.Offset(offset, 0);
                        tabRect.Width -= offset;
                    }
                    else
                    {
                        using (Image imgDisabled = Helpers.CreateDisabledImage(img))
                        {
                            graphics.DrawImage(imgDisabled, tabRect.Left + 8, ((tabRect.Height - imgDisabled.Height) / 2));
                            int offset = imgDisabled.Width + 12;
                            tabRect.Offset(offset, 0);
                            tabRect.Width -= offset;
                        }
                    }
                }
            }

            tabRect.Height -= 3;

            StringFormat sf = new StringFormat();
            sf.Alignment = StringAlignment.Near;
            sf.LineAlignment = StringAlignment.Center;

            using (Brush br = new SolidBrush(foreColor))
            {
                graphics.DrawString(tabPage.Text, MetroFonts.TabControl(metroLabelSize, metroLabelWeight), br, tabRect, sf);
            }

            //TextRenderer.DrawText(graphics, tabPage.Text, MetroFonts.TabControl(metroLabelSize, metroLabelWeight),
            //                      tabRect, foreColor, Color.Transparent, MetroPaint.GetTextFormatFlags(TextAlign));                                  

            DrawGlassEffect(bgRect, graphics, bSelected || bMouseOver);
        }

        [SecuritySafeCritical]
        private void DrawUpDown(Graphics graphics)
        {
            Color backColor = Parent != null ? Parent.BackColor : MetroPaint.BackColor.Form(Theme);

            Rectangle borderRect = new Rectangle();
            WinApi.GetClientRect(scUpDown.Handle, ref borderRect);

            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;

            graphics.Clear(backColor);

            using (Brush b = new SolidBrush(MetroPaint.BorderColor.TabControl.Normal(Theme)))
            {
                GraphicsPath gp = new GraphicsPath(FillMode.Winding);
                PointF[] pts = { new PointF(6, 6), new PointF(16, 0), new PointF(16, 12) };
                gp.AddLines(pts);

                graphics.FillPath(b, gp);

                gp.Reset();

                PointF[] pts2 = { new PointF(borderRect.Width - 15, 0), new PointF(borderRect.Width - 5, 6), new PointF(borderRect.Width - 15, 12) };
                gp.AddLines(pts2);

                graphics.FillPath(b, gp);

                gp.Dispose();
            }
        }

        #endregion

        #region Overridden Methods

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            Invalidate();
        }

        protected override void OnParentBackColorChanged(EventArgs e)
        {
            base.OnParentBackColorChanged(e);
            Invalidate();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Invalidate();
        }

        [SecuritySafeCritical]
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (!DesignMode)
            {
                WinApi.ShowScrollBar(Handle, (int)WinApi.ScrollBar.SB_BOTH, 0);
            }
        }

        protected override CreateParams CreateParams
        {
            [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
            get
            {
                const int WS_EX_LAYOUTRTL = 0x400000;
                const int WS_EX_NOINHERITLAYOUT = 0x100000;
                var cp = base.CreateParams;
                if (isMirrored)
                {
                    cp.ExStyle = cp.ExStyle | WS_EX_LAYOUTRTL | WS_EX_NOINHERITLAYOUT;
                }
                return cp;
            }
        }

        private new Rectangle GetTabRect(int index)
        {
            if (index < 0)
                return new Rectangle();

            Rectangle baseRect = base.GetTabRect(index);
            return baseRect;
        }

        //protected override void OnMouseWheel(MouseEventArgs e)
        //{
        //    if (SelectedIndex != -1)
        //    {
        //        if (!TabPages[SelectedIndex].Focused)
        //        {
        //            bool subControlFocused = false;
        //            foreach (Control ctrl in TabPages[SelectedIndex].Controls)
        //            {
        //                if (ctrl.Focused)
        //                {
        //                    subControlFocused = true;
        //                    return;
        //                }
        //            }

        //            if (!subControlFocused)
        //            {
        //                TabPages[SelectedIndex].Select();
        //                TabPages[SelectedIndex].Focus();
        //            }
        //        }
        //    }
            
        //    base.OnMouseWheel(e);
        //}

        protected override void OnCreateControl()
        {
            base.OnCreateControl();
            this.OnFontChanged(EventArgs.Empty);
            FindUpDown();
        }

        protected override void OnControlAdded(ControlEventArgs e)
        {
 	         base.OnControlAdded(e);
             FindUpDown();
             UpdateUpDown();
        }

        protected override void OnControlRemoved(ControlEventArgs e)
        {
 	        base.OnControlRemoved(e);
            FindUpDown();
            UpdateUpDown();
        }

        protected override void  OnSelectedIndexChanged(EventArgs e)
        {
 	        base.OnSelectedIndexChanged(e);
            UpdateUpDown();
            Invalidate();
        }

        //send font change to properly resize tab page header rects
        //http://www.codeproject.com/Articles/13305/Painting-Your-Own-Tabs?msg=2707590#xx2707590xx
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        private const int WM_SETFONT = 0x30;
        private const int WM_FONTCHANGE = 0x1d;

        [SecuritySafeCritical]
        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            IntPtr hFont = MetroFonts.TabControl(metroLabelSize, metroLabelWeight).ToHfont();
            SendMessage(this.Handle, WM_SETFONT, hFont, (IntPtr)(-1));
            SendMessage(this.Handle, WM_FONTCHANGE, IntPtr.Zero, IntPtr.Zero);
            this.UpdateStyles();
        }
        #endregion

        #region Helper Methods

        [SecuritySafeCritical]
        private void FindUpDown()
        {
            if (!DesignMode)
            {
                bool bFound = false;

                IntPtr pWnd = WinApi.GetWindow(Handle, WinApi.GW_CHILD);

                while (pWnd != IntPtr.Zero)
                {
                    char[] className = new char[33];

                    int length = WinApi.GetClassName(pWnd, className, 32);

                    string s = new string(className, 0, length);

                    if (s == "msctls_updown32")
                    {
                        bFound = true;

                        if (!bUpDown)
                        {
                            this.scUpDown = new SubClass(pWnd, true);
                            this.scUpDown.SubClassedWndProc += new SubClass.SubClassWndProcEventHandler(scUpDown_SubClassedWndProc);

                            bUpDown = true;
                        }
                        break;
                    }

                    pWnd = WinApi.GetWindow(pWnd, WinApi.GW_HWNDNEXT);
                }

                if ((!bFound) && (bUpDown))
                    bUpDown = false;
            }
        }

        [SecuritySafeCritical]
        private void UpdateUpDown()
        {
            if (bUpDown && !DesignMode)
            {
                if (WinApi.IsWindowVisible(scUpDown.Handle))
                {
                    Rectangle rect = new Rectangle();
                    WinApi.GetClientRect(scUpDown.Handle, ref rect);
                    WinApi.InvalidateRect(scUpDown.Handle, ref rect, true);
                }
            }
        }

        [SecuritySafeCritical]
        private int scUpDown_SubClassedWndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case (int)WinApi.Messages.WM_PAINT:

                    IntPtr hDC = WinApi.GetWindowDC(scUpDown.Handle);

                    Graphics g = Graphics.FromHdc(hDC);

					DrawUpDown(g);

					g.Dispose();

                    WinApi.ReleaseDC(scUpDown.Handle, hDC);

                    m.Result = IntPtr.Zero;

                    Rectangle rect = new Rectangle();

                    WinApi.GetClientRect(scUpDown.Handle, ref rect);
                    WinApi.ValidateRect(scUpDown.Handle, ref rect);

                    return 1;
            }

            return 0;
        }

        #endregion

    }
}
