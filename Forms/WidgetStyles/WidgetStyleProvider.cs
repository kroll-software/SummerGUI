using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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
	public enum WidgetStates
	{
		Default,
		Disabled,
		Hover,
		Active,		// which means focused
		Selected,	// which also means focused most of the time
		Pressed,	// this is a PRESSED button or a DRAGGED scroll-grip.
		Custom1,
		Custom2
	}

	public class WidgetStyleProvider : IEnumerable<IWidgetStyle>
	{		
		public readonly static int WidgetStatesCount = Enum.GetValues(typeof(WidgetStates)).Length;

		public IWidgetStyle[] Styles { get; private set; }

		public WidgetStyleProvider () : this (null)
		{			
		}

		public WidgetStyleProvider (IWidgetStyle defaultStyle)
		{			
			Styles = new IWidgetStyle[WidgetStatesCount];
			if (defaultStyle == null)
				Styles [0] = new EmptyWidgetStyle ();
			else
				Styles [0] = defaultStyle;
		}			
			
		public IEnumerator<IWidgetStyle> GetEnumerator ()
		{
			// This call is not redundant.
			// Array.GetEnumerator returns a SimpleEnumerator 
			// which fails to be casted to IEnumerator<T>
			return Styles.OfType<IWidgetStyle>().GetEnumerator ();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
		{
			return Styles.GetEnumerator();
		}

		public IWidgetStyle this[WidgetStates state]  
		{  
			get { return GetStyle(state); }  
			set { SetStyle (value, state); }  
		}

		public IWidgetStyle GetStyle(WidgetStates state)
		{			
			return Styles[(int)state] ?? Styles [0];
		}
			
		public void SetStyle(IWidgetStyle style, WidgetStates state)
		{
			Styles[(int)state] = style;
		}

		public bool HasStyle(WidgetStates state)
		{			
			return Styles[(int)state] != null;
		}

		public virtual void RefreshStyles ()
		{
			for (int i = 0; i < Styles.Length; i++)
			if (Styles[i] != null)
					Styles[i].InitStyle ();
		}

		public virtual void Clear()
		{
			for (int i = 0; i < Styles.Length; i++)
				Styles [i] = null;
			SetStyle (new EmptyWidgetStyle (), WidgetStates.Default);
		}
	}
}

