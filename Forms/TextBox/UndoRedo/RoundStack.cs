#region license

/*
Copyright (c) 2007 Lu Yixiang

Permission is hereby granted, free of charge, to any person
obtaining a copy of this software and associated documentation
files (the "Software"), to deal in the Software without
restriction, including without limitation the rights to use,
copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the
Software is furnished to do so, subject to the following
conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
OTHER DEALINGS IN THE SOFTWARE.
 */

#endregion

// Modified Version by Kroll-Software: Added support for tracking of modified state.

using System;
using System.Collections.Generic;
using System.Text;

namespace GenericUndoRedo
{
    /// <summary>
    /// Stack with capacity, bottom items beyond the capacity are discarded.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Serializable]
    public class RoundStack<T>
    {
        //private T[] items; // items.Length is Capacity + 1        
        public T[] items; // items.Length is Capacity + 1        

        // top == bottom ==> full
        public int top = 1;
        public int bottom = 0;

        /// <summary>
        /// Gets if the <see cref="RoundStack&lt;T&gt;"/> is full.
        /// </summary>
        public bool IsFull
        {
            get
            {
                return top == bottom;
            }
        }

        /// <summary>
        /// Gets the number of elements contained in the <see cref="RoundStack&lt;T&gt;"/>.
        /// </summary>
        public int Count
        {
            get
            {
                int count = top - bottom - 1;
                if (count < 0)
                    count += items.Length;
                return count;
            }
        }

        /// <summary>
        /// Gets the capacity of the <see cref="RoundStack&lt;T&gt;"/>.
        /// </summary>
        public int Capacity
        {
            get
            {
                return items.Length - 1;
            }
        }

        /// <summary>
        /// Creates <see cref="RoundStack&lt;T&gt;"/> with given capacity
        /// </summary>
        /// <param name="capacity"></param>
        public RoundStack(int capacity)
        {
            if (capacity < 1)
                throw new ArgumentOutOfRangeException("Capacity need to be at least 1");
            items = new T[capacity + 1];
        }

        /// <summary>
        /// Removes and returns the object at the top of the <see cref="RoundStack&lt;T&gt;"/>.
        /// </summary>
        /// <returns></returns>
        public T Pop()
        {
            if (Count > 0)
            {
                T removed = items[top];
                items[top--] = default(T);
                if (top < 0)
                    top += items.Length;
                return removed;
            }
            else
                throw new InvalidOperationException("Cannot pop from emtpy stack");
        }

        /// <summary>
        /// Inserts an object at the top of the <see cref="RoundStack&lt;T&gt;"/>.
        /// </summary>
        /// <param name="item"></param>
        public void Push(T item)
        {
            if (IsFull)
            {
                bottom++;
                if (bottom >= items.Length)
                    bottom -= items.Length;
            }
            if (++top >= items.Length)
                top -= items.Length;
            items[top] = item;
        }

        /// <summary>
        /// Returns the object at the top of the <see cref="RoundStack&lt;T&gt;"/> without removing it.
        /// </summary>
        public T Peek()
        {
            return items[top];
        }

        /// <summary>
        /// Removes all the objects from the <see cref="RoundStack&lt;T&gt;"/>.
        /// </summary>
        public void Clear()
        {
            if (Count > 0)
            {
                for (int i = 0; i < items.Length; i++)
                {
                    items[i] = default(T);
                }
                top = 1;
                bottom = 0;
            }
        }

    }
}
