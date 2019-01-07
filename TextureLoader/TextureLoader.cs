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

	// ToDo: Still under construction:
	// an asynchronous Textureloader which loads fast 
	// and without blocking the GUI / OpenGL

	// http://luiscubal.blogspot.ch/2013/04/asynchronous-opengl-texture-loading.html

	public class TextureLoader
	{
		UpdateTaskScheduler UITaskScheduler;
		readonly TaskFactory UITaskFactory;
		CancellationToken m_Token;

		public TextureLoader ()
		{			
			m_Token = new CancellationToken ();
			Func<bool> executeTask;
			UITaskScheduler = UpdateTaskScheduler.CreateNew(out executeTask) as UpdateTaskScheduler;
			UITaskFactory = new TaskFactory(m_Token,
				TaskCreationOptions.HideScheduler,
				TaskContinuationOptions.HideScheduler, UITaskScheduler);
		}			

		/** Usage in UpdateFrame: load 50 bitmaps each frame
		for (int i = 0; i < 50 && executeTask(); ++i)
		{
		}
		**/

		public static Task WaitMilliSeconds(int milli) {
			var tcs = new TaskCompletionSource<object>();
			new Thread(() => {
				Thread.Sleep(milli);
				tcs.SetResult(null);
			}).Start();

			return tcs.Task;
		}

		public static void ExecuteAfterMilliseconds(int milli, Action<Task> action)
		{
			var tcs = new TaskCompletionSource<object>();
			new Thread(() => {
				Thread.Sleep(milli);
				tcs.SetResult(null);
			}).Start();

			tcs.Task.ContinueWith(action);
		}

		public static Task WaitOneSecond() {
			var tcs = new TaskCompletionSource<object>();

			new Thread(() => {
				Thread.Sleep(1000);
				tcs.SetResult(null);
			}).Start();

			return tcs.Task;
		}

		public void LoadTextureFromBitmap (Bitmap bitmap)
		{

		}

		/***
		WaitOneSecond().ContinueWith(task => { Console.WriteLine("We're done!"); });
		***/

		/// <summary>
		/// Provide a list of filenames to load
		/// </summary>
		/// <returns>The textures.</returns>
		/// <param name="texturesToLoad">Textures to load.</param>
		public Task<object> LoadTextures(IEnumerable<string> texturesToLoad)
		{
			var uiFactory = UITaskFactory;

			int counter = 0;

			var tcs = new TaskCompletionSource<object> ();

			var baseTasks = new List<Task> ();
			foreach (var textureToLoad in texturesToLoad) {
				var baseTask = new Task<Bitmap> (() => {
					var bitmap = new Bitmap (textureToLoad);
					return bitmap;
				});

				baseTask.ContinueWith (task => {
					uiFactory.StartNew (() => {
						var bitmap = task.Result;

						LoadTextureFromBitmap (bitmap);

						bitmap.Dispose ();

						if (Interlocked.Decrement (ref counter) == 0) {
							//We have no further textures to load
							tcs.SetResult (null); //Tell TCS we're done!
						}
					});
				});

				baseTasks.Add (baseTask);
			}

			counter = baseTasks.Count;

			foreach (var baseTask in baseTasks) {
				baseTask.Start ();
			}

			return tcs.Task;
		}
	}
}

