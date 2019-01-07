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
	public class CommandInputTextBox : TextBox
	{

		string m_InfoText;
		public string InfoText 
		{ 
			get {
				return m_InfoText;
			}
			set {
				if (m_InfoText != value) {
					m_InfoText = value;
					if (String.IsNullOrEmpty (m_InfoText))
						TextMargin = DefaultTextMargin;
					else
						TextMargin = new SizeF(DefaultTextMargin.Width + Font.Measure(m_InfoText).Width + (DefaultTextMargin.Width / 2), DefaultTextMargin.Height);
				}
			}
		}

		protected Brush InfoTextBrush { get; private set; }
		public Color InfoTextColor 
		{ 
			get {
				return InfoTextBrush.Color;
			}
			set {
				InfoTextBrush.Color = value;
			}
		}

		protected SizeF DefaultTextMargin;

		public CommandInputTextBox (string name, string infoText, bool transparent = false)
			: base(name, null, transparent)
		{			
			DefaultTextMargin = TextMargin;
			InfoTextBrush = new SolidBrush (ForeColor);
			InfoText = infoText;
		}

		protected override void OnScaleWidget (IGUIContext ctx, float absoluteScaleFactor, float relativeScaleFactor)
		{
			base.OnScaleWidget (ctx, absoluteScaleFactor, relativeScaleFactor);
			DefaultTextMargin.Scale (relativeScaleFactor);
		}

		public override void OnPaint (IGUIContext ctx, RectangleF bounds)
		{
			base.OnPaint (ctx, bounds);

			if (!String.IsNullOrEmpty (m_InfoText)) {
				bounds.Inflate (-DefaultTextMargin.Width, -DefaultTextMargin.Height);
				SizeF infoSize = ctx.DrawString (m_InfoText, Font, InfoTextBrush, bounds, FontFormat.DefaultSingleLine);
				bounds.Inflate (-infoSize.Width, 0);
			}
		}

		protected override void CleanupManagedResources ()
		{
			InfoTextBrush.Dispose ();
			base.CleanupManagedResources ();
		}
	}
}

