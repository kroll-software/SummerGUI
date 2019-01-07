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
	public class ToggleCheckBox : CheckBox
	{
		public ToggleCheckBox (string name, string text)
			: base(name, text)
		{
			CheckChars = new char[2] {(char)FontAwesomeIcons.fa_toggle_off, (char)FontAwesomeIcons.fa_toggle_on};
		}
	}
}

