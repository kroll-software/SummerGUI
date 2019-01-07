using System;
using System.Drawing;
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
	public class GraphicalWidgetState
	{
		public GraphicalWidgetState(string name, WidgetStates widgetState)
		{
			Name = name;
			WidgetState = widgetState;
		}

		public string Name { get; set; }
		public WidgetStates WidgetState { get; set; }
		public Rectangle SourceRectangle { get; set; }
		public int ImageIndex { get; set; }
	}

	public class GraphicalWidgetStateCollection : List<GraphicalWidget>
	{
	}

	public class GraphicalWidget : Widget
	{
		public GraphicalWidgetStateCollection GraphicalWidgetStates { get; private set; }

		public GraphicalWidget (string name, Docking dock, IWidgetStyle style)
			: base(name, dock, style)
		{
			GraphicalWidgetStates = new GraphicalWidgetStateCollection ();
		}
	}
}

