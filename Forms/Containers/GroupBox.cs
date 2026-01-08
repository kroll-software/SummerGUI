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
	public enum GroupBoxBorderStyles
	{
		None,
		Single,
		Double
	}

	public class GroupBox : Container
	{
		public string Text { get; set; }
		public GroupBoxBorderStyles BorderStyle { get; set; }

		public GroupBox (string name)
			: base (name)
		{
		}

		public virtual void DrawBorder(IGUIContext ctx, System.Drawing.RectangleF bounds)
		{
			
		}

		public virtual void DrawCaption(IGUIContext ctx, System.Drawing.RectangleF bounds)
		{

		}

		public override void OnPaintBackground (IGUIContext ctx, System.Drawing.RectangleF bounds)
		{
			base.OnPaintBackground (ctx, bounds);
			DrawBorder (ctx, bounds);
			DrawCaption (ctx, bounds);
		}			
	}
}

