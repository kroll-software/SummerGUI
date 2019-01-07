/*
{*******************************************************************}
{                                                                   }
{          KS-Foundation Library                                    }
{          Build rock solid DotNet applications                     }
{          on a threadsafe foundation without the hassle            }
{                                                                   }
{          Copyright (c) 2014 - 2015 by Kroll-Software,             }
{          Altdorf, Switzerland, All Rights Reserved                }
{          www.kroll-software.ch                                    }
{                                                                   }
{   Licensed under the MIT license                                  }
{   Please see LICENSE.txt for details                              }
{                                                                   }
{*******************************************************************}
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KS.Foundation
{
    public class BinarySortedList<T> : List<T> where T : IComparable<T>
    {
		// faster than SortedList when below 25k items.
		// 10 times faster with 1k items
		// 2 times faster with 10k items

        public readonly object SyncObject = new object();

        protected IComparer<T> m_Comparer = null;
        public IComparer<T> Comparer
        {
            get
            {
                return m_Comparer;
            }
            set
            {
                m_Comparer = value;
            }
        }

		// Avoid to create huge arrays for each element on creation
		// 3 should be fine for most smaller Containers
		public static int DefaultCapacity = 3;

		public BinarySortedList() : base(DefaultCapacity) { }
        public BinarySortedList(int capacity) : base(capacity) { }
        
        public BinarySortedList(IEnumerable<T> collection) 
            : base(collection) 
        {
            Sort();
        }   
        
		public BinarySortedList(IComparer<T> comparer) : base(DefaultCapacity) 
        {
            m_Comparer = comparer;
        }

        public BinarySortedList(int capacity, IComparer<T> comparer)
            : base(capacity) 
        {
            m_Comparer = comparer;
        }

        public BinarySortedList(IEnumerable<T> collection, IComparer<T> comparer) : base(collection) 
        {
            m_Comparer = comparer;
            Sort(m_Comparer);
        }

        public T First
        {
            get
            {
                if (this.Count == 0)
                    return default(T);
                else
                    return this[0];
            }
        }

        public T Last
        {
            get
            {
                if (this.Count == 0)
                    return default(T);
                else
                    return this[this.Count - 1];
            }
        }

        public void RemoveFirst()
        {
            if (this.Count > 0)
                this.RemoveAt(0);
        }

        public void RemoveLast()
        {
            if (this.Count > 0)
                this.RemoveAt(Count - 1);
        }        

        /// <summary>
        /// This always returns a value, no bounds checks required
        /// Except when the list is empty, returns default(T)
        /// </summary>
        /// <param name="search"></param>
        /// <returns></returns>
        public T FindSafe(T search)
        {
            lock (SyncObject)
            {
                if (Count == 0)
                    return default(T);
                
                if (m_Comparer == null)
                    m_Comparer = Comparer<T>.Default;

                int a = 0;
                int b = Count;
                while (b - a > 1)
                {
                    int mid = (a + b) / 2;
                    if (m_Comparer.Compare(this[mid], search) > 0)
                        b = mid;
                    else
                        a = mid;
                }

                return this[a];
            }
        }

        /// <summary>
        /// This always returns a value, no bounds checks required
        /// Except when the list is empty, returns default(T)
        /// </summary>
        /// <param name="search"></param>
        /// <returns></returns>
        public T Find(T elem)
        {
            lock (SyncObject)
            {
                int i = IndexOf(elem);
                if (i >= 0)
                    return this[i];

                return default(T);
            }
        }
        

        public int AddOrUpdate(T elem)
        {
            lock (SyncObject)
            {
                int index = IndexOf(elem);               
                if (index < 0)
                    index = ~index;

                if (index >= Count)
                    base.Add(elem);
                else
                    this[index] = elem;

                return index;
            }
        }

		public virtual void OnInsert(T elem)
		{
		}

        /// <summary>
        /// You can't find or delete after this
        /// </summary>
        /// <param name="elem"></param>
        public void AddUnsorted(T elem)
        {
            base.Add(elem);
			OnInsert (elem);
        }

        public new void Add(T elem)
        {
            lock (SyncObject)
            {
                int index = IndexOf(elem);
                if (index < 0)                
                    index = ~index;

                if (index >= Count)
                    base.Add(elem);
                else
                    base.Insert(index, elem);
            }

			OnInsert (elem);
        }

        public int IndexOfElementOrPredecessor(T item)
        {
            lock (SyncObject)
            {
                int index = IndexOf(item);
                if (index >= 0)
                {
                    if (index < Count)
                        return index;

                    return -1;
                }

                return ~index - 1;
            }
        }

        public int IndexOfElementOrSuccessor(T item)
        {
            lock (SyncObject)
            {
                int index = IndexOf(item);
                if (index >= 0)
                    return index;

                return ~index;
            }
        }

        // The C# language is missing the most basic operators in math:
        // The predecessor and the successor operator !!
        // 
        // In mathematics, this is the fundamental precondition to count or compare elements.
        // Everything, that has a successor defined, can be counted and compared
        //
        // We would want to return the predecessor or the successor with the following functions,
        // if an element could not be found. Unfortunately we can't do that and have to return default(T), 
        // which is not the same and not what we want..

        public T FindElementOrPredecessor(T item)
        {
            lock (SyncObject)
            {
                int index = IndexOfElementOrPredecessor(item);
                if (index < 0)
                    return default(T);
                else
                    return this[index];
            }
        }

        public T FindElementOrSuccessor(T item)
        {
            lock (SyncObject)
            {
                int index = IndexOfElementOrSuccessor(item);
                if (index >= Count)
                    return default(T);
                else
                    return this[index];
            }
        }        

        // Microsoft DotNet BinarySearch is times solwer
        //public void AddSorted(T newitem)        
        //{
        //    int binraySearchIndex = this.BinarySearch(newitem, m_Comparer);
        //    if (binraySearchIndex < 0)
        //        base.Insert(~binraySearchIndex, newitem);
        //    else
        //        base.Insert(binraySearchIndex, newitem);
        //}

        /// <summary>
        /// returns IndexOf or -1
        /// </summary>
        /// <param name="elem"></param>
        /// <returns></returns>
        public new int IndexOf(T elem)
        {
            lock (SyncObject)
            {                
                if (m_Comparer == null)
                    m_Comparer = Comparer<T>.Default;

                int a = 0;
                int b = Count;
                while (true)
                {
                    if (a == b)
                        return ~a;

                    int mid = a + ((b - a) / 2);                    
                    switch (m_Comparer.Compare(this[mid], elem))
                    {
                        case -1:
                            a = mid + 1;
                            break;

                        case 1:
                            b = mid;
                            break;

                        case 0:
                            return mid;
                    }
                }
            }
        }

        public void NaturalMergeSort()
        {
            NaturalMergeSorter<T> sorter = new NaturalMergeSorter<T>();
            T[] res = this.ToArray();
            sorter.Sort(ref res, m_Comparer);
            lock (SyncObject)
            {
                for (int i = 0; i < Count; i++)
                    this[i] = res[i];
            }
        }
    }

    public class NaturalMergeSorter<T> where T : IComparable<T>
    {
        private T[] a;
        private T[] b;    // Hilfsarray
        private int n;

        IComparer<T> comp = null;

        public void Sort(ref T[] a, IComparer<T> comparer)
        {
            this.a = a;
            n = a.Length;
            b = new T[n];

            comp = comparer;
            if (comp == null)
                comp = Comparer<T>.Default;

            naturalmergesort();
        }

        private bool mergeruns(ref T[] a, ref T[] b)
        {
            int i = 0, k = 0;
            bool asc = true;
            T x;

            while (i < n)
            {
                k = i;
                //do x = a[i++]; while (i < n && x <= a[i]);  // aufsteigender Teil
                do x = a[i++]; while (i < n && comp.Compare(x, a[i]) <= 0);  // aufsteigender Teil
                //while (i < n && x >= a[i]) x = a[i++];      // absteigender Teil
                while (i < n && comp.Compare(x, a[i]) >= 0)
                    x = a[i++];      // absteigender Teil

                merge(ref a, ref b, k, i - 1, asc);
                asc = !asc;
            }
            return k == 0;
        }

        private void merge(ref T[] a, ref T[] b, int lo, int hi, bool asc)
        {
            int k = asc ? lo : hi;
            int c = asc ? 1 : -1;
            int i = lo, j = hi;

            // jeweils das nächstgrößte Element zurückkopieren,
            // bis i und j sich überkreuzen
            while (i <= j)
            {
                //if (a[i] <= a[j])
                if (comp.Compare(a[i], a[j]) <= 0)
                    b[k] = a[i++];
                else
                    b[k] = a[j--];
                k += c;
            }
        }

        private void naturalmergesort()
        {
            // abwechselnd von a nach b und von b nach a verschmelzen
            while (!mergeruns(ref a, ref b) & !mergeruns(ref b, ref a)) ;
        }

    }    // end class NaturalMergeSorter
}
