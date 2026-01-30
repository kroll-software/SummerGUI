using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KS.Foundation;

namespace SummerGUI
{	
	public class TaskTimer : IDisposable
	{
		public bool Enabled { get; set; }
		public int Delay { get; set; }
		public int Due { get; set; }
		int currDelay;

		Action ACTION;
		volatile CancellationTokenSource m_TokenSource;

		public TaskTimer (int delay, Action action, int due = 0)
		{			
			ACTION = action;
			Enabled = true;
			Delay = Math.Max(50, delay);
			if (due > 0)
				Due = Math.Max(250, due);
		}

		public void Start()
		{	
			currDelay = Due > 0 ? Due : Delay;
			// das ist der Trick an der Sache
			//if (m_TokenSource != null)
			//	return;
			Enabled = true;
			Loop ();
		}
		public void Stop()
		{	
			Enabled = false;
			try {
				if (m_TokenSource != null) {
					m_TokenSource.Cancel ();
					//m_TokenSource = null;	// nullreferece error
				}	
			} catch {			
			}
			finally
			{
				m_TokenSource = null;
			}
		}

		public void Reset()
		{
			// Wir setzen die Zeit zurück auf den Startwert (Due oder Delay)
			currDelay = Due > 0 ? Due : Delay;

			// Der Trick: Wir lösen einen Abbruch des aktuellen Delays aus.
			// Die Schleife in Loop() fängt das ab und startet sofort neu, 
			// solange Enabled auf true steht.
			if (m_TokenSource != null)
			{
				try 
				{
					m_TokenSource.Cancel();
				}
				catch (ObjectDisposedException) 
				{
					// Falls das Token gerade in der Finalisierung war
				}
			}
			
			if (Enabled)
			{
				// Falls der Timer gestoppt war, aber Reset gerufen wird
				Start();
			}
		}
			
		async void Loop()
		{	
			try {
				if (m_TokenSource != null) {
					m_TokenSource.Cancel ();
				}
				m_TokenSource = new CancellationTokenSource ();
				while (Enabled) {
					await Task.Delay(currDelay, m_TokenSource.Token)	//.ContinueWith(tsk => {})						
						.ContinueWith((t) => {
							if (t.Status == TaskStatus.RanToCompletion && Enabled && ACTION != null) {
								ACTION();
							}
						}, CancellationToken.None, 
							TaskContinuationOptions.None, 
							TaskScheduler.Default);					
					currDelay = Delay;
				}
			} catch {
			} finally {
				m_TokenSource = null;
			}
			//Console.WriteLine ("stopped");	// check, how often it is stopped
		}

		public void Dispose()
		{
			Stop ();
			ACTION = null;
			GC.SuppressFinalize (this);
		}
	}
}

