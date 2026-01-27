using System;
using System.Linq;
using System.Drawing;
using OpenTK;
using OpenTK.Input;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using KS.Foundation;

namespace SummerGUI
{
	public class ChildFormOverlayWidgetStyle : WidgetStyle
	{
		public override void InitStyle ()
		{
			SetBackColor (Color.FromArgb(0, Color.Black));
			SetForeColor (Color.Empty);
			SetBorderColor (Color.Empty);
		}
	}

	public abstract class ChildFormOverlay : Container, IChildFormHost
	{	
		public event EventHandler<EventArgs> Closing;
		public OverlayModes OverlayMode { get; protected set; }

		public ChildFormOverlay (string name)
			: base(name, Docking.Fill, new ChildFormOverlayWidgetStyle())
		{						
			ZIndex = 100000;
			this.Dock = Docking.Fill;
			OverlayMode = OverlayModes.Modal;
			CanFocus = true;
		}

		public void OnOK ()
		{
			Result = DialogResults.OK;
			OnClose();
		}

		public void OnCancel ()
		{
			Result = DialogResults.Cancel;
			OnClose();
		}

		public virtual void OnClose ()
		{
			if (Closing != null)
				Closing (this, EventArgs.Empty);

			// Do nothing here
			if (ParentWindow != null) {
				ParentWindow.Controls.RemoveChild (this);
				Invalidate (10);
				this.Dispose ();
			}
		}						
			
		public bool AllowMinimize { get; set; }
		public bool AllowMaximize { get; set; }
		public bool IsModal { get; set; }

		public DialogResults Result { get; set; }
				

		public virtual void ShowDialog (SummerGUIWindow parent)
		{									
			if (parent != null) {				
				parent.Controls.AddChild (this);
			}

			this.Focus ();
		}
			
		public override bool OnKeyDown (KeyboardKeyEventArgs e)
		{			
			switch (e.Key) {
			case Keys.Escape:				
				OnClose();
				return true;			
			default:
				return base.OnKeyDown (e);
			}				
		}

		protected override void CleanupManagedResources ()
		{			
			base.CleanupManagedResources ();
		}
	}
}

