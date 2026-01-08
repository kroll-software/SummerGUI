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
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using KS.Foundation;

namespace SummerGUI
{
	public class RadioButton : CheckBoxBase
	{		
		public override bool Checked
		{
			get{
				return m_Checked;
			}
			set{
				if (m_Checked != value) {
					m_Checked = value;

					if (m_Checked && Parent != null) {
						for (int i = 0; i < Parent.Children.Count; i++) {
							RadioButton rb = Parent.Children [i] as RadioButton;
							if (rb != null && rb != this && rb.RadioButtonGroup == RadioButtonGroup) {
								rb.Checked = false;
							}
						}
					}	

					Invalidate ();
				}
			}
		}

		public string RadioButtonGroup { get; set; }

		public RadioButton (string name, string text)
			: base (name, text)
		{
			//CheckChars = new char[2] {'(', ')'};
			CheckChars = new char[2] {(char)FontAwesomeIcons.fa_circle_thin, (char)FontAwesomeIcons.fa_dot_circle_o};
			//IconOffsetY = -0.25f;
		}

		public override void OnClick (MouseButtonEventArgs e)
		{
			if (!Checked) {
				Checked = true;
				Invalidate ();
			}
			base.OnClick (e);
		}
	}
}

