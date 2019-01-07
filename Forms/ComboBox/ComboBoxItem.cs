using System;
using KS.Foundation;

namespace SummerGUI
{
	public class ComboBoxItem : IComparable<ComboBoxItem>
	{
		public int CompareTo (ComboBoxItem other)
		{
			if (Text == null || other == null || other.Text == null)
				return 0;
			return Text.CompareTo (other.Text);
		}

		public string Text { get; set; }
		public object Value { get; set; }
		public bool Selected { get; set; }

		public ComboBoxItem (string text, object value)
		{
			Text = text;
			Value = value;
		}

		public override string ToString ()
		{
			return Text + String.Empty;
		}
	}

	public class ComboBoxItemCollection : BinarySortedList<ComboBoxItem>
	{
		public void Add(string text)
		{
			base.Add(new ComboBoxItem(text, null));
		}

		public void Add(string text, object value)
		{
			base.Add(new ComboBoxItem(text, value));
		}

		public void AddUnsorted(string text)
		{
			base.AddLast(new ComboBoxItem(text, null));
		}

		public void AddUnsorted(string text, object value)
		{
			base.AddLast(new ComboBoxItem(text, value));
		}
	}
}

