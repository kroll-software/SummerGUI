using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using KS.Foundation;


namespace SummerGUI
{
	public class WidgetByTaborderSortComparer : IComparer<Widget>
	{
		public int Compare(Widget w1, Widget w2)
		{
			int comp = w1.TabIndex.CompareTo (w2.TabIndex);
			if (comp == 0)
				comp = w1.ID.CompareTo (w2.ID);
			return comp;
		}
	}

	public static class SummerGuiFormsExtensions
	{
		public static bool CanTabInto (this Widget widget)
		{
			//return widget != null && widget.IsVisibleEnabled && ((widget.CanFocus || widget.CanSelect) && 
			return widget != null && widget.IsVisibleEnabled && ((widget.CanFocus) && 
				widget.TabIndex >= 0);
			/***	
			|| (widget as Container != null && (widget as Container).Children.Count > 0 && 
					(widget as Container).Children.Any(c => c.CanTabInto()));
					**/
		}

		public static IEnumerable<Widget> TabSupportingChildren(this Container container)
		{
			return container.Children.Where (CanTabInto).OrderBy(c => c, new WidgetByTaborderSortComparer());
		}
			
		public static bool TabIntoFirst(this Widget widget)
		{			
			if (widget as Container != null) {
				Widget temp = ((widget as Container).TabSupportingChildren ().FirstOrDefault ());
				if (temp != null)
					TabIntoFirst (temp);
				else
					widget.TabInto ();
				return true;
			} else if (widget.CanTabInto ()) {
				widget.TabInto ();
				return true;
			}
			return false;
		}

		public static bool TabIntoLast(this Widget widget)
		{			
			if (widget as Container != null) {
				Widget temp = ((widget as Container).TabSupportingChildren ().LastOrDefault ());
				if (temp != null)
					TabIntoLast (temp);
				else
					widget.TabInto ();
				return true;
			} else if (widget.CanTabInto ()) {
				widget.TabInto ();
				return true;
			}
			return false;
		}

		public static void TabInto(this Widget widget)
		{			
			if (widget == null)
				return;
			System.Reflection.MethodInfo info = ReflectionUtils.GetMethod (widget.GetType (), "TabInto", 0);
			if (info != null)
				info.Invoke (widget, null);
			else
				widget.Focus ();
		}
	}
}

