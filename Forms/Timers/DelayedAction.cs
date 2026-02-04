using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KS.Foundation;

namespace SummerGUI
{
	public class DelayedAction : IDisposable
	{		
		public int Delay { get; set; }
		Action ACTION;
		volatile CancellationTokenSource m_TokenSource;
		public bool Enabled  { get; set; }
		public bool IsStarted  { get; private set; }

		public DelayedAction (int delay, Action action)
		{
			Delay = delay;
			ACTION = action;
			Enabled = true;
		}

		public async void Start()
		{			
			try {							
				Enabled = true;
				IsStarted = true;
				if (m_TokenSource != null)
					return;
				//Stop();
				//
				m_TokenSource = new CancellationTokenSource ();
				await Task.Delay(Delay, m_TokenSource.Token)	//.ContinueWith(tsk => {})
					.ContinueWith((t) => { 
						if (t.Status == TaskStatus.RanToCompletion && Enabled && ACTION != null) {
							ACTION(); 
						}
					}, CancellationToken.None, 
						TaskContinuationOptions.None,
						TaskScheduler.Default);
			} catch {				
			} finally {
				m_TokenSource = null;
				IsStarted = false;
			}
		}

		public void Stop()
		{
			try {
				Enabled = false;
				IsStarted = false;
				if (m_TokenSource != null) {
					m_TokenSource.Cancel ();
					//Concurrency.WaitSpinning(3);
				}
			} catch {				
			}
		}
				
		public void Dispose()
		{
			Stop ();
			ACTION = null;
			GC.SuppressFinalize (this);
		}
	}
}

