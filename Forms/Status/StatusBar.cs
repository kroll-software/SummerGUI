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

			DefaultPanel = new StatusTextPanel ("default", Docking.Left, "");
			//DefaultPanel.ZIndex = 100;
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
		
		// ==========================================================
		// Layout: DefaultPanel + Left + Fill + Right
		//
		// Reihenfolge in StatusBar:
		//   [1] DefaultPanel  — immer sichtbar, LINKS, erste Position
		//   [2] Left-Panels   — Docking.Left, ChildCollection-Reihenfolge
		//   [3] Fill/Center   — Docking.Fill o. ohne Docking, wachsen
		//   [4] Right-Panels  — Docking.Right, Rest (von rechts nach links)
		//
		// PanelWidth-Semantik:
		//   0      = Inhalt-Groesse (shrink-to-content)
		//   (0..1] = BRUCHTEIL des flexiblen Restplatzes (waechst)
		//   > 1    = FESTE Pixelbreite
		//
		// Panels ohne IStatusPanel o. mit PanelWidth = 0 => rein content-basiert.
		// MinSize wird immer garantiert (sichtbar bleiben).
		// ==========================================================
		protected override void LayoutChildren (IGUIContext ctx, RectangleF bounds)
		{
			if (Children.Count == 0)
				return;

			const float Gap = 6f;

			// [0] Panels in Gruppen einsortieren
			Widget def    = null;
			var    lefts  = new List<Widget> ();
			var    fills  = new List<Widget> ();
			var    rights = new List<Widget> ();

			for (int i = 0; i < Children.Count; i++) {
				Widget c = Children [i];
				if (c == null || !c.Visible) continue;
				if (c == DefaultPanel)            def    = c;
				else if (c.Dock == Docking.Left)  lefts .Add (c);
				else if (c.Dock == Docking.Right) rights.Add (c);
				else                              fills .Add (c); // Fill/Center/No-Docking
			}

			// [1] Display-Reihenfolge: Def -> Left -> Fill -> Right
			var all = new List<Widget> ();
			if (def != null) all.Add (def);
			all.AddRange (lefts);
			all.AddRange (fills);
			all.AddRange (rights);

			int    n    = all.Count;
			if (n == 0)
				return;

			float avail  = Math.Max (0f, bounds.Width);
			float height = bounds.Height;
			float gaps   = (n > 1) ? Gap * (n - 1) : 0f;

			// [2] Pro Panel: starre Breite (Content), Gewicht, Min/Max
			float[] content = new float [n];   // Content-Breite (ODER fixe Pixel > 1)
			float[] minA    = new float [n];   // absolute Untergrenze (sichtbar bleiben)
			float[] maxA    = new float [n];
			float[] weight  = new float [n];   // 0 = starres, > 0 = Anteil am Restplatz

			for (int i = 0; i < n; i++) {
				Widget     w  = all [i];
				IStatusPanel p = w as IStatusPanel;

				content [i] = (p != null && p.PanelWidth > 1f) ? p.PanelWidth
				                       : Math.Max (0f, w.PreferredSize (ctx, bounds.Size).Width);
				minA [i]    = Math.Max (8f, Math.Max (0f, w.MinSize.Width));
				maxA [i]    = (w.MaxSize.Width < float.MaxValue) ? w.MaxSize.Width : float.MaxValue;

				float wt = 0f;
				if (p != null) {
					if (p.Fill)                                       wt = 1f;
					else if (p.PanelWidth > 0f && p.PanelWidth <= 1f) wt = p.PanelWidth;
				}
				weight [i] = wt;
			}

			// [4] Breite pro Panel (Cap-Modell, v4):
			//     * PanelWidth ∈ (0..1] / Fill = OBERGRENZE ("bis zu X %"):
			//       ein gewichtetes Panel wird NIE breiter als sein Inhalt
			//       (Content) ODER seine Quote, je nachdem welches kleiner ist.
			//       -> "Ready." mit 0.6 bleibt knapp (≈45 px), nicht 60 % breit.
			//     * Starres Panel (PanelWidth == 0): voller Content,
			//       aber bei Platzmangel verkleinerbar (wie alle).
			//     * Niemals eine Überlappung; kein Panel > Gesamtbreite.
			float budget = Math.Max (0f, avail - gaps);
			float[] target = new float [n];
			float   sumM = 0f;

			for (int i = 0; i < n; i++) {
				float demand = Math.Min (maxA [i], Math.Max (minA [i], content [i]));
				float cap    = (weight [i] > 0f)
				             ? Math.Min (maxA [i], Math.Max (minA [i], weight [i] * budget))
				             : demand;
				target [i]   = Math.Min (demand, cap);
				sumM        += minA [i];
			}

			// [5] Platzmangel -> vom LÄNGSTEN Panel abtragen (bis zum Minimum):
			//     Kurze Panels (Ready., Diagnose) bleiben ganzzäh; lange Panels
			//     (Filepath) verlieren zuerst ihre überflüssigen pixels.
			{
				float sumT   = 0f;
				for (int i = 0; i < n; i++)
					sumT += target [i];
				float excess = sumT - budget;
				if (excess > 0.01f) {
					var order = Enumerable.Range (0, n)
						.OrderByDescending (i => target [i] - minA [i]).ToArray ();
					foreach (int i in order) {
						if (excess <= 0.01f)
							break;
						float room = target [i] - minA [i];
						float trim = Math.Min (room, excess);
						target [i] -= trim;
						excess     -= trim;
					}
				}
			}

			float WidthOf (int i) => target [i];

			if (sumM + gaps > budget + 0.01f) {
				// Pathologischer Fall: Fenster kleiner als die Summe aller Minima.
				// Von rechts nach links Mindestbreiten vergibt; Panels ohne
				// Budget erhalten EXAKT Breite 0 (werden nicht gezeichnet) —
				// Overlap ist ausgeschlossen.
				for (int i = 0; i < n; i++)
					target [i] = 0f;
				float rem = budget;
				for (int i = n - 1; i >= 0; i--) {
					if (rem <= 0f)
						break;
					float w = Math.Min (minA [i], rem);
					target [i] = w;
					rem -= w;
				}
			}

			float x  = bounds.Left;
			float xr = bounds.Right;
			int    idx = 0;

			void Place (int i, float left, float width)
			{
				if (width <= 0f) return;
				all [i].SetBounds (new RectangleF (left, bounds.Top, width, height));
				all [i].OnResize (ctx);
			}

			// [7] Layout: [Def] [Left] [Fill] ... [Right]
			// [1] DefaultPanel — LINKS, erste Position
			if (def != null) {
				float bw = WidthOf (idx);
				Place (idx, x, bw); x += bw + Gap; idx++;
			}
			// [2] Left-Panels
			for (int k = 0; k < lefts .Count; k++) {
				float bw = WidthOf (idx);
				Place (idx, x, bw); x += bw + Gap; idx++;
			}
			// [3] Fill/Center-Panels
			for (int k = 0; k < fills .Count; k++) {
				float bw = WidthOf (idx);
				Place (idx, x, bw); x += bw + Gap; idx++;
			}
			// [4] Right-Panels: von rechts nach links, ChildCollection-Reihenfolge
			for (int k = 0; k < rights.Count; k++) {
				float bw = WidthOf (idx);
				xr -= bw;
				Place (idx, xr, bw); xr -= Gap;
				idx++;
			}
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
										status = StatusStack.Peek().Status;
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
		public void ShowProgress(int PromilleDone)
		{
			if (ProgressPanel == null || PromilleDone == LastProgressValue)
				return;

			try
			{
				lock (LockShowProgress)
				{
					LastProgressValue = PromilleDone;

					if (PromilleDone < 0)
						PromilleDone = 0;

					if (PromilleDone >= 1000)
					{						
						ProgressPanel.Visible = false;
						EnableGUI(true);
					}
					else
					{
						EnableGUI(false);						
						ProgressPanel.Value = PromilleDone / 1000f;
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

