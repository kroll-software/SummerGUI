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
	public class GUIEnabler : IDisposable
	{
		protected StatusBar Status { get; set; }

		public GUIEnabler(StatusBar status)
		{
			Status = status;
		}

		~GUIEnabler() 
		{
			Dispose ();
		}

		private bool IsDisposed;

		public void Dispose()
		{						
			if (!IsDisposed) {
				IsDisposed = true;
				Status.EnableGUI (true);
				Status = null;
				GC.SuppressFinalize (this);
			}				
		}
	}

		
	public class StatusBar : Container, IStatusPresenter, IRootControllerObserver
	{
		public StatusTextPanel DefaultPanel { get; set; }
		public StatusProgressPanel ProgressPanel { get; set; }

		public IObserver<EventMessage> RootControllerObserver { get; private set; }

		public StatusBar (string name) : this(name, new StatusBarStyle()) {}
		public StatusBar (string name, IWidgetStyle style)
			: base(name, Docking.Bottom, style)
		{
			this.ZIndex = 5000;
			this.Padding = new Padding (6, 3, 5, 3);
			this.Margin = Padding.Empty;

			DefaultPanel = new StatusTextPanel ("default", Docking.Fill, "");
			this.AddChild (DefaultPanel);

			ProgressPanel = new StatusProgressPanel ("progress");
			ProgressPanel.Visible = false;
			this.AddChild (ProgressPanel);

			RootControllerObserver = new Observer<EventMessage> (OnNext, OnError, OnCompleted);

			ReadyStatusString = "Ready.";
			ShowStatus ();
		}			

		public void OnNext(EventMessage message)
		{
			switch (message.Subject) {
			case "ShowStatus":
				if (message.Args != null) {
					string msg = message.Args.FirstOrDefault ().SafeString ();
					bool waitCursor = false;
					if (msg != null && message.Args.Length > 1)
						waitCursor = message.Args [1].SafeBool ();
					ShowStatus (msg, waitCursor, true);
				}
				break;
			case "ClearStatus":
				ShowStatus ();
				break;
			}
		}

		public void OnError(Exception ex)
		{
			ShowStatus (ex.Message, false, false);
		}

		public void OnCompleted()
		{
		}
			
		public override SizeF PreferredSize (IGUIContext ctx, SizeF proposedSize)
		{
			if (CachedPreferredSize == SizeF.Empty) {
				if (Children.Count == 0)
					return new SizeF (0, 21);

				/***
				float h = 0;
				float w = 0;
				for (int i = 0; i < Children.Count; i++) {
					Widget child = Children [i];
					if (child != null && child.Visible && !child.IsOverlay) {
						SizeF sz = child.PreferredSize (ctx);
						h = Math.Max (h, sz.Height + child.Margin.Height);
						w = Math.Max (w, sz.Width + child.Margin.Width);
					}
				}
				***/

				IGUIFont font = WidgetExtensions.GetFont (CommonFontTags.Default);
				if (font != null)
					CachedPreferredSize = new SizeF (proposedSize.Width, font.CaptionHeight + Padding.Height);
				else
					CachedPreferredSize = new SizeF (0, 21);

				//return new SizeF (w + Padding.Width, h + Padding.Height);
			}				

			return CachedPreferredSize;
		}			
			

		protected StatusMessageStack StatusStack = new StatusMessageStack();

		public void ClearStatus()
		{
			lock (LockShowStatus)
			{
				StatusStack.Clear();                
			}

			ShowStatus("", false);
		}

		public string ReadyStatusString { get; set; }

		public void ShowStatus()
		{
			ShowStatus("", false, true);
		}

		protected int m_StatusCount = 1;

		protected object LockShowStatus = new object();
		public virtual void ShowStatus(string status, bool waitCursor, bool useStack = true)
		{
			//Console.WriteLine ("ShowStatus {0}", status);

			if (DefaultPanel == null)
				return;
			try
			{
				if (useStack)
				{
					if (!String.IsNullOrEmpty(status))
						m_StatusCount++;
					else
						m_StatusCount--;

					lock (LockShowStatus)
					{
						if (String.IsNullOrEmpty(status))
						{							
							if (StatusStack.Count > 0)
								StatusStack.Pop();

							if (StatusStack.Count > 0)
							{
								waitCursor = StatusStack.Peek().WaitCursor;
								status = StatusStack.Pop().Status;
							}
							else
								waitCursor = false;
						}
						else
						{
							StatusStack.Push(new StatusMessageItem(status, waitCursor));
						}
					}
				}

				if (String.IsNullOrEmpty(status))
					status = ReadyStatusString;

				DefaultPanel.Text = status;

				if (waitCursor)
				{					
					ParentWindow.Do(p => p.ShowWaitCursor());
					DefaultPanel.Icon = (char)FontAwesomeIcons.fa_hourglass_2;
				}
				else
				{
					ParentWindow.Do(p => p.RestoreCursor());
					DefaultPanel.Icon = (char)FontAwesomeIcons.None;
				}

				Update(true);
			}
			catch (Exception ex)
			{                
				ex.LogError ();
			}            
		}


		protected int LastProgressValue = -1;
		protected object LockShowProgress = new object();
		public void ShowProgress(int PercentDone)
		{
			if (ProgressPanel == null || PercentDone == LastProgressValue)
				return;

			try
			{
				lock (LockShowProgress)
				{
					LastProgressValue = PercentDone;

					if (PercentDone < 0)
						PercentDone = 0;

					if (PercentDone >= 100)
					{						
						ProgressPanel.Visible = false;
						EnableGUI(true);
					}
					else
					{
						EnableGUI(false);
						ProgressPanel.Value = PercentDone;
						ProgressPanel.Visible = true;
					}
				}

				this.Invalidate();
			}
			catch (Exception ex)
			{                
				ex.LogError ();
			}
		}

		public IDisposable DisableGUI ()
		{
			EnableGUI (false);
			return new GUIEnabler (this);
		}

		protected bool LastEnableGUI = true;        
		internal void EnableGUI(bool enable)
		{            
			if (LastEnableGUI == enable)
				return;

			try {
				if (ParentWindow != null && ParentWindow.Controls != null)
					ParentWindow.Controls.Enabled = enable;
				LastEnableGUI = enable;

				this.Enabled = true;
			} catch (Exception) {
			}            

			try {                
				this.Focus();
			} catch (Exception) {
			}
		}

		protected override void CleanupManagedResources ()
		{
			if (RootControllerObserver != null)
				(RootControllerObserver as IDisposable).Dispose ();
			base.CleanupManagedResources ();
		}
	}
}

