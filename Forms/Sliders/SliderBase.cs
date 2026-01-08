using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using KS.Foundation;

namespace SummerGUI
{
	public abstract class SliderBase : Container
	{
		public float Value { get; set; }
		public float MinValue { get; set; }
		public float MaxValue { get; set; }

		public void SetValidValue(float value)
		{
			Value = value.Clamp (MinValue, MaxValue);
		}			

		protected SliderBase (string name, Docking dock, IWidgetStyle style)
			: base(name, dock, style)
		{			
			MinValue = 0;
			MaxValue = 1;
			Value = 0;
		}
	}
}

