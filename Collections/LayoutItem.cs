using System.Drawing;

namespace KS.Foundation
{
    public interface ILayoutItem
    {
        RectangleF Bounds { get; }
        ILayoutItem Clone();
    }

    public class LayoutItem<T> where T : ILayoutItem
    {
        public T Item { get; private set; }
        public RectangleF Bounds { get; private set; }

        public LayoutItem(RectangleF bounds, T item)
        {
            this.Bounds = bounds;
            this.Item = item;
        }

        public LayoutItem<T> Clone()
        {
            return new LayoutItem<T>(this.Bounds, this.Item);
        }
    }
}
