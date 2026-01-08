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
	public class ConsoleOverlayOutputWidgetStyle : WidgetStyle
	{
		public override void InitStyle ()
		{
			SetBackColor (Color.FromArgb(128, Color.Black));
			SetForeColor (Theme.Colors.Base3);
			SetBorderColor (Color.Empty);
		}
	}

	public class ConsoleOutputWidgetStyle : WidgetStyle
	{
		public override void InitStyle ()
		{
			SetBackColor (Color.Black);
			SetForeColor (Theme.Colors.Base2);
			SetBorderColor (Color.Empty);
		}
	}

	public class ConsoleOverlayOutputWidget : ConsoleOutputWidget
	{
		public ConsoleOverlayOutputWidget (string name)
			: base (name, Docking.Fill, new ConsoleOverlayOutputWidgetStyle())
		{
			ZIndex = 10000;
		}
	}

	public class ConsoleOutputWidget : ScrollingBox
	{
		protected readonly EventLogger Logger;

		public ConsoleOutputWidget (string name) : this (name, Docking.Fill, new ConsoleOutputWidgetStyle()) {}
		public ConsoleOutputWidget (string name, Docking dock, IWidgetStyle style)
			: base (name, dock, style)
		{
			//Font = ApplicationWindow.CurrentContext.FontManager.MonoFont;
			Padding = new Padding (16);
			IsAnimationRunning = false;

			Logger = new EventLogger (LogLevels.Verbose);
			Logger.LogEvent += (sender, e) => AddParagraph(e.Message.Replace("\t", "  "));
			Logging.RegisterLogger (Logger);
		}

		public new void Start()
		{
			throw new NotSupportedException ();
		}

		protected override void OnVisibleChanged ()
		{
			base.OnVisibleChanged ();
			RecalculateItems ();
		}

		protected override void RecalculateItems ()
		{
			if (Items.Count == 0 || !Visible)
				return;

			base.RecalculateItems ();

			if (TotalItemHeight <= Height) {
				Items [0].rectF.Y = 0;
				for (int i = 1; i < Items.Count; i++) {
					Items [i].rectF.Y = Items [i - 1].rectF.Bottom;				
				}
			} else {
				Items [Items.Count - 1].rectF.Y = Bounds.Height - Padding.Bottom - Items [Items.Count - 1].rectF.Height;
				for (int i = Items.Count - 2; i >= 0; i--) {
					Items [i].rectF.Y = Items [i + 1].rectF.Top - Items [i].rectF.Height;
				}
			}
		}

		public override void OnLayout (IGUIContext ctx, RectangleF bounds)
		{
			IsAnimationRunning = false;
			base.OnLayout (ctx, bounds);
		}

		protected override void CleanupManagedResources ()
		{
			if (Logger != null) {
				Logging.UnregisterLogger (Logger);
				Logger.Dispose ();
			}
			base.CleanupManagedResources();
		}
	}
}

