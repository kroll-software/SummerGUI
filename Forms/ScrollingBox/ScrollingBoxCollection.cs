using System;
using System.Collections.Generic;

namespace SummerGUI
{
	public class ScrollingBoxCollection : System.Collections.CollectionBase
	{
		public event EventHandler OnCollectionChanged;
		public ScrollingBoxCollection()
		{
		}
		public int Add(ScrollingBoxItem value)
		{
			int index = base.InnerList.Add(value);
			if (OnCollectionChanged != null)
			{
				OnCollectionChanged(this, new EventArgs());
			}
			return index;
		}
		public int Add(string Text)
		{
			return this.Add(new ScrollingBoxText(Text));
		}
		public void InsertAt(int index, ScrollingBoxItem value)
		{
			base.InnerList.Insert(index, value);
			if (OnCollectionChanged != null)
			{
				OnCollectionChanged(this, new EventArgs());
			}
		}
		public void Remove(ScrollingBoxItem value)
		{
			base.InnerList.Remove(value);
			if (OnCollectionChanged != null)
			{
				OnCollectionChanged(this, new EventArgs());
			}
		}
		public ScrollingBoxItem this[int index]
		{
			get
			{
				return (ScrollingBoxItem)base.InnerList[index];
			}
			set
			{
				base.InnerList[index] = value;
			}
		}
	}
}

