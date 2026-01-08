using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using KS.Foundation;


namespace SummerGUI
{
	[AttributeUsage(AttributeTargets.Property 
		| AttributeTargets.Field 
		| AttributeTargets.Interface
		| AttributeTargets.Module)]
	public class DpiScalableAttribute : Attribute {}

	public static class BreadthFirstSearchWidgetEnumerator
	{
		public static IEnumerable<Widget> EnumerateWidgets(this Widget root)
		{	
			var queue = new Queue<Widget>();
			queue.Enqueue(root);
			while(queue.Any())
			{
				var w = queue.Dequeue();
				yield return w;

				if ((w as Container) != null) {					
					(w as Container).Children.ForEach (queue.Enqueue);
					if ((w as TabContainer) != null) {
						(w as TabContainer).TabPages.ForEach(queue.Enqueue);
					}
				}
			}
		}

		/***
		public static void CountWidgetsWithFont(this Widget root)
		{
			Console.Clear ();
			Console.WriteLine ();
			Console.WriteLine (">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>");
			Console.WriteLine ();

			int wCount = 0;
			int wFontCount = 0;

			int maxPathToRoot = 0;

			EnumerateWidgets (root).ForEach (w => {
				wCount++;

				int ptr = 0;
				Widget p = w;
				while (p.Parent != null) {
					p = p.Parent;
					ptr++;
				}

				if (ptr > maxPathToRoot)
					maxPathToRoot = ptr;

				bool hasFont = false;
				if (ReflectionUtils.HasProperty(w.GetType(), "Font")) {
					hasFont = true;
					wFontCount++;
				}

				Console.WriteLine ("Name: {0}, Type: {1}, HasFont: {2}, PRT: {3}", w.Name, w.GetType().Name, hasFont.ToLowerString(), ptr);
			});

			Console.WriteLine ("Total Widgets: {0}, with Font: {1}", wCount.ToString("n0"), wFontCount.ToString("n0"));

			Console.WriteLine ();
			Console.WriteLine ("Max Path to Root: {0}", maxPathToRoot);
			Console.WriteLine ();

			Console.WriteLine ("######################################");
			Console.WriteLine ();
		}
		***/
	}		

	public class DpiScalingAutomat : DisposableObject
	{
		public IGUIContext Owner { get; private set; }

		public DpiScalableAttribute GetDpiScalableAttribute(PropertyInfo prop)
		{
			// we have to iterate through all BaseTypes.
			// System.Reflection wouldn't do that for us (which is quite unexpected behavior)

			DpiScalableAttribute attr = null;

			while (prop != null) {
				attr = prop.GetAttribute<DpiScalableAttribute> ();
				if (attr != null)
					return attr;
				prop = prop.ReflectedType.BaseType.GetProperty (prop.Name);
			}

			return null;
		}

		public object ScaleValue(object value, float scaling) {
			if (value == null)
				return null;

			switch (value.GetType ().Name) {
			case "Single":
				return ((float)value).Scale (scaling);
			case "Double": 
				return ((double)value).Scale (scaling);
			case "Int32":		
				return ((int)value).Scale (scaling);
			case "Int64":
				return ((long)value).Scale (scaling);
			case "Padding":
				return ((Padding)value).Scale (scaling);			
			case "Size":				
				return ((Size)value).Scale (scaling);
			case "SizeF":				
				return ((SizeF)value).Scale (scaling);
			}

			return value;
		}

		public DpiScalingAutomat (IGUIContext owner)
		{
			Owner = owner;
		}
			
		public void ScaleGUI(Widget root = null)
		{		
			if (Owner == null) {
				this.LogError ("ScaleGUI: Owner must not be null");
				return;
			}

			if (root == null && Owner as SummerGUIWindow != null)
				root = (Owner as SummerGUIWindow).Controls;

			float absFactor = Owner.ScaleFactor;

			SummerGUI.Scrolling.ScrollBar.ScrollBarWidth = 17.Scale(absFactor);

			Stopwatch sw = Stopwatch.StartNew();

			int widgetCount = 0;
			int propertyCount = 0;

			root.EnumerateWidgets().Where(w => Math.Abs(w.ScaleFactor - absFactor) > float.Epsilon).Reverse().ForEach(w => {
				widgetCount++;
				float relFactor = absFactor / w.ScaleFactor;

				Type wType = w.GetType();
				wType.GetProperties().ForEach(prop => {						
					DpiScalableAttribute attr = GetDpiScalableAttribute(prop);
					if (attr != null) {																					
						try {
							object originalValue = ReflectionUtils.GetPropertyValue(w, prop.Name);
							prop.SetValue(w, ScaleValue(originalValue, relFactor));
							propertyCount++;
						} catch (Exception ex) {
							ex.LogError("ScaleGUI, set value for property '{0}.{1}'", wType.Name, prop.Name);
						}
					}
				});

				if (w.Styles != null) {
					int styleID = 0;
					w.Styles.ForEach(style => {
						styleID++;
						if (style != null) {
							Type styleType = style.GetType();
							styleType.GetProperties().ForEach(prop => {
								DpiScalableAttribute attr = GetDpiScalableAttribute(prop);
								if (attr != null) {										
									try {		
										object originalValue = ReflectionUtils.GetPropertyValue(style, prop.Name);
										prop.SetValue(style, ScaleValue(originalValue, relFactor));
										propertyCount++;
									} catch (Exception ex) {
										ex.LogError("ScaleGUI, set value for style-property '{0}.{1}'", styleType.Name, prop.Name);
									}
								}
							});
						}
					});							
				}

				try {
					w.OnScaleWidget(Owner, absFactor);	
				} catch (Exception ex) {
					ex.LogError("OnScaleWidget");
				}
			});

			this.LogVerbose ("Scaling successfully applied to {0} properties of {1} widgets in {2} ms.", propertyCount.ToString("n0"), widgetCount.ToString("n0"), sw.ElapsedMilliseconds.ToString("n0"));

			Owner.Invalidate ();
			//root.Invalidate ();
			// ToDo:
		}

		protected override void CleanupUnmanagedResources ()
		{
			Owner = null;
			base.CleanupUnmanagedResources ();
		}
	}
}

