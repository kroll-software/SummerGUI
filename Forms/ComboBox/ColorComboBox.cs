using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using KS.Foundation;

namespace SummerGUI
{
    public class ColorComboBox : ComboBoxBase
    {
        public ColorComboBox (string name)
            : base (name)
        {
        }

        public override string Text
        {
            get
            {
                ComboBoxItem item = SelectedItem;
                if (item is ColorItem colorItem)
                {
                    return colorItem.Color.Name;
                }
                return string.Empty;
            }
            set
            {
                // Simple text setter
            }
        }

        public override void Clear()
        {
            Items.Clear();
        }

        public void Add(Color color)
        {
            Items.Add(new ColorItem(color));
        }

        public Color GetSelectedColor()
        {
            ComboBoxItem item = SelectedItem;
            if (item is ColorItem colorItem)
            {
                return colorItem.Color;
            }
            return Color.Empty;
        }

        public void SetSelectedColor(Color color)
        {
            ComboBoxItem item = Items.FirstOrDefault(i => i is ColorItem ci && ci.Color == color);
            if (item != null)
            {	
				SelectItem(item);
            }
        }

        public override void DrawItem(IGUIContext ctx, RectangleF bounds, ComboBoxItem item, IWidgetStyle style)
        {
            if (item is ColorItem colorItem)
            {
                RectangleF colorRect = new RectangleF(bounds.X, bounds.Y + 5, bounds.Height - 10, bounds.Height - 10);
                ctx.FillRectangle(new SolidBrush(colorItem.Color), colorRect);
                ctx.DrawRectangle(new Pen(Color.FromArgb(150, Color.Black)), colorRect);
            }
        }
    }
}