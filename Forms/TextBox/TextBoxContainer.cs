using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Input;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System.Drawing;
using KS.Foundation;

namespace SummerGUI
{
	public abstract class TextBoxContainer : Container
	{
		public event EventHandler<EventArgs> TextChanged;
		public void OnTextChanged()
		{
			if (TextChanged != null)
				TextChanged (this, EventArgs.Empty);			
		}

		protected TextBox TB { get; private set; }

		protected TextBoxContainer (string name)
			: this (name, Docking.Fill)
		{			
		}

		protected TextBoxContainer (string name, Docking dock)
			: base (name, dock, null)
		{
			TB = new TextBox ("text", null, true);
			TB.Dock = Docking.Fill;
			AddChild (TB);
			MaxSize = TB.MaxSize;

			/*** ***/
			//Styles = new WidgetStyleProvider (new TextBoxWidgetStyle ());
			Styles.Clear();
			Styles.SetStyle (new TextBoxWidgetStyle (), WidgetStates.Default);
			Styles.SetStyle (new TextBoxActiveWidgetStyle (), WidgetStates.Active);
			Styles.SetStyle(new TextBoxDisabledWidgetStyle(), WidgetStates.Disabled);
		}			

		public bool ReadOnly { 
			get {
				if (TB == null)
					return false;
				return TB.ReadOnly;
			}
			set {
				if (TB != null)
					TB.ReadOnly = value;
			}
		}

		public override SizeF PreferredSize (IGUIContext ctx, SizeF proposedSize)
		{
			return TB.PreferredSize(ctx, proposedSize);
		}

		public virtual string Text {
			get {
				return TB.Text;
			}
			set {
				if (TB.Text != value) {
					TB.Text = value;
					OnTextChanged ();
				}
			}
		}

		public override bool IsFocused {
			get {				
				return TB.IsFocused;
			}
			set {
				TB.IsFocused = value;
			}
		}
	}
}

