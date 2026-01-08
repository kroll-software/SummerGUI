using System;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using KS.Foundation;

namespace SummerGUI
{
	public abstract class MeanValueCircularBuffer<T>
	{		
		protected readonly object SyncObject;
		protected readonly LinkedList<T> Values;

		public int N { get; private set; }

		protected MeanValueCircularBuffer (int n = 30)
		{
			N = n;
			Values = new LinkedList<T> ();
			SyncObject = new object();
		}			

		public void Put(T elem) {			
			lock (SyncObject) {				
				Values.AddFirst(elem);
				while (Values.Count > N)
					Values.RemoveLast ();
			}
		}

		public T MeanValue
		{
			get{
				lock (SyncObject) {
					if (Values.Count == 0)
						return default(T);
					return CalculateMean ();
				}
			}
		}

		protected abstract T CalculateMean ();
	}

	public class FramePerformanceMeter : MeanValueCircularBuffer<long>
	{
		readonly Stopwatch sw;
		long last_value = 0;

		public FramePerformanceMeter(int n) : base(n) 
		{
			sw = Stopwatch.StartNew ();
		}

		protected new void Put (long value) {
			base.Put(value);
		}

		protected override long CalculateMean ()
		{			
			int count = Values.Count;
			if (count == 0)
				return 0;
			return Values.Sum () / count;
		}			

		public long Pulse()
		{		
			if (!Monitor.TryEnter (SyncObject, 1))
				return last_value;

			if (!sw.IsRunning)
				return last_value;
			
			try {
				sw.Stop ();
				Put (sw.ElapsedMilliseconds);
				last_value = CalculateMean ();
				return last_value;
			} catch (Exception ex) {
				ex.LogError ();
				return 0;
			} finally{
				sw.Reset ();
				sw.Start ();
				Monitor.Exit (SyncObject);
			}
		}
	}
}

