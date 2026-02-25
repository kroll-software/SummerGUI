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
	public class CheckBox : CheckBoxBase
	{
		public CheckBox (string name, string text)
			: base (name, text)
		{			
			CheckChars = [(char)FontAwesomeIcons.fa_square_o, (char)FontAwesomeIcons.fa_check_square_o];
			//IconOffsetY = -1f;
		}

		public override void OnClick (MouseButtonEventArgs e)
		{
			if (Enabled)
				Checked = !Checked;
			base.OnClick (e);
		}
	}
}

