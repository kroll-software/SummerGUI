using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using KS.Foundation;

namespace SummerGUI
{
	public class MarqueBarWidgetStyle : WidgetStyle
	{
		public override void InitStyle ()
		{
			base.InitStyle ();
		}
	}

	// ToDo:	Not completely implemented yet

	public class MarqueBar : Widget
	{
		public MarqueBar (string name)
			: base(name, Docking.Fill, new MarqueBarWidgetStyle())
		{
		}

		public bool AutoHide { get; set; }
		public string Message { get; protected set; } 

		public bool IsRunning { get; private set; }

		public void StartAnimation()
		{
			if (IsRunning)
				return;
			IsRunning = true;
		}

		public void PauseAnimation()
		{
			if (!IsRunning)
				return;
			IsRunning = false;
		}

		public void ShowMessage(string message, int Seconds = 0)
		{
			Message = message;

			if (String.IsNullOrEmpty (message)) {
				if (AutoHide)
					Visible = false;
				return;
			}

			if (AutoHide)
				Visible = true;
			StartAnimation ();
		}
	}
}

