using System;
using System.Linq;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using OpenTK.Input;
using KS.Foundation;

namespace SummerGUI
{
	public class UpdateTaskScheduler : TaskScheduler
	{
		ConcurrentQueue<Task> tasks = new ConcurrentQueue<Task>();

		protected override void QueueTask(Task task)
		{
			tasks.Enqueue(task);
		}

		protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
		{
			return false;
		}

		protected override bool TryDequeue(Task task)
		{
			return false;
		}

		protected override IEnumerable<Task> GetScheduledTasks()
		{
			return new List<Task>(tasks).AsReadOnly();
		}

		public override int MaximumConcurrencyLevel
		{
			get
			{
				return 1;
			}
		}

		bool ExecuteNextTask()
		{
			Task task;
			if (tasks.TryDequeue(out task))
			{
				//bool result = base.TryExecuteTask(task);
				base.TryExecuteTask(task);
				return true;
			}
			return false;
		}

		public static TaskScheduler CreateNew(out Func<bool> executionFunction)
		{
			var scheduler = new UpdateTaskScheduler();
			executionFunction = scheduler.ExecuteNextTask;
			return scheduler;
		}
	}
}

