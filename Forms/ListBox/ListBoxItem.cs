using System;
using KS.Foundation;

namespace SummerGUI
{
	public class ListBoxItem : IComparable<ListBoxItem>
	{
		public int CompareTo (ListBoxItem other)
		{
			if (Text == null || other == null || other.Text == null)
				return 0;
			return Text.CompareTo (other.Text);
		}

		public string Text { get; set; }
		public object Value { get; set; }
		public bool Selected { get; set; }
        public bool Checked { get; set; }

		public ListBoxItem (string text, object value)
		{
			Text = text;
			Value = value;
		}

		public override string ToString ()
		{
			return Text + String.Empty;
		}
	}

	public class ListBoxItemCollection : BinarySortedList<ListBoxItem>
	{
		public void Add(string text)
		{
			base.Add(new ListBoxItem(text, null));
		}

		public void Add(string text, object value)
		{
			base.Add(new ListBoxItem(text, value));
		}

		public void AddUnsorted(string text)
		{
			base.AddLast(new ListBoxItem(text, null));
		}

		public ListBoxItem AddUnsorted(string text, object value)
		{
			var item = new ListBoxItem(text, value);
			base.AddLast(item);
			return item;
		}
	}
}

