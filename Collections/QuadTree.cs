using System.Collections.Generic;
using System.Drawing;

// found in this project under CBOL license
// http://www.codeproject.com/Articles/224231/Word-Cloud-Tag-Cloud-Generator-Control-for-NET-Win
// Thank you for sharing this great work!

namespace KS.Foundation
{
    /// <summary>
    ///   A Quadtree is a structure designed to partition space so
    ///   that it's faster to find out what is inside or outside a given 
    ///   area. See http://en.wikipedia.org/wiki/Quadtree
    ///   This QuadTree contains items that have an area (RectangleF)
    ///   it will store a reference to the item in the quad 
    ///   that is just big enough to hold it. Each quad has a bucket that 
    ///   contain multiple items.
    /// </summary>
    public class QuadTree<T>  where T : ILayoutItem
    {
        public delegate void QuadTreeAction(QuadTreeNode<T> obj);

        private readonly RectangleF m_Rectangle;
        private readonly QuadTreeNode<T> m_Root;

        public QuadTree(RectangleF rectangle)
        {
            m_Rectangle = rectangle;
            m_Root = new QuadTreeNode<T>(m_Rectangle);
        }        

        public int Count
        {
            get { return m_Root.Count; }
        }

        public void Add(T item)
        {
            m_Root.Insert(item);
        }

        //public void Insert(T item)
        //{
        //    m_Root.Insert(item);
        //}

        public IEnumerable<T> Query(RectangleF area)
        {
            return m_Root.Query(area);
        }

        public bool HasContent(RectangleF area)
        {
            bool result = m_Root.HasContent(area);
            return result;
        }        

        public void ForEach(QuadTreeAction action)
        {
            m_Root.ForEach(action);
        }
    }
}