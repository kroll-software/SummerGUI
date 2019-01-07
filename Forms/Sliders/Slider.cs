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
	public class Slider : SliderBase
	{
		public Slider (string name)
			: base(name, Docking.Fill, new WidgetStyle())
		{
		}
	}
}

