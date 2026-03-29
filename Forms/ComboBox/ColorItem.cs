using System;
using System.Drawing;
using KS.Foundation;

namespace SummerGUI
{
    /// <summary>
    /// Special item type for storing color information
    /// </summary>
    public class ColorItem : ComboBoxItem
    {
        public Color Color { get; private set; }
        public int Alpha { get; private set; }
        
        public ColorItem(Color color, int alpha = 255)
            : base(color.ToString(), color)
        {
            Color = color;
            Alpha = Math.Max(0, Math.Min(255, alpha));
        }
        
        public override string ToString()
        {
            return Color.Name;
        }
    }
}
