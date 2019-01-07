using System;
using System.Linq;

namespace SummerGUI
{
	public class ChildCollection : KS.Foundation.BinarySortedList<Widget>
	{
		public Container Parent { get; internal set; }

		public ChildCollection (Container parent)
			//: base(new ChildrenComparerByZIndex())
			: base()	// Default-Comparer does the job.
		{
			Parent = parent;
		}

		public Widget this [string name] 
		{
			get {
				for (int i = 0; i < Count; i++) {
					if (this[i].Name == name) {
						return this [i];
					}
				}
				return null;
			}
		}

		public override void OnInsert (Widget elem)
		{
			if (elem != null && Parent != null) {
				elem.Parent = Parent;
				if (Parent.Root != null)
					elem.Root = Parent.Root;
				else if (Parent as RootContainer != null)
					elem.Root = Parent as RootContainer;
			}
			//base.OnInsert (elem);
		}
	}
}

