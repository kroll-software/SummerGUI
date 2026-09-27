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
            : base(FormatName(color), color)
        {
            Color = color;
            Alpha = Math.Max(0, Math.Min(255, alpha));
        }

        /// <summary>
        /// Re-points this item at a different color (keeps it in place inside
        /// its ComboBoxItemCollection, used after a "More ..." colour pick).
        /// </summary>
        public void ResetColor(Color color)
        {
            Color = color;
            Value = color;
            Text = FormatName(color);
        }

        public static string FormatName(Color color)
        {
            // Named system colours report their name; custom ones render as hex.
            string name = color.Name;
            if (string.IsNullOrEmpty(name) || name == "Empty")
                return "#" + color.ToArgb().ToString("X8");
            return name;
        }
        
        public override string ToString()
        {
            return FormatName(Color);
        }
    }
}
