using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using MeshEditor.AvaloniaUI.Dialogs;
using NUnit.Framework;

namespace MeshEditor.AvaloniaUI.Tests
{
	/// <summary>
	/// Records the windows the application opens while it is alive, so tests can check that no stray window
	/// (e.g. a progress view nobody closes) is left open. Dispose it at the end of the test.
	/// </summary>
	public sealed class OpenWindows : IDisposable
	{
		private readonly List<Window> opened = new List<Window>();
		private readonly IDisposable subscription;

		public OpenWindows()
		{
			subscription = Window.WindowOpenedEvent.AddClassHandler<Window>((w, e) => opened.Add(w), RoutingStrategies.Direct);
		}

		/// <summary>
		/// Windows opened since this instance was created that are still visible.
		/// </summary>
		public IReadOnlyList<Window> Visible => opened.Where(w => w.IsVisible).Distinct().ToList();

		public IReadOnlyList<T> VisibleOf<T>() where T : Window => Visible.OfType<T>().ToList();

		/// <summary>
		/// Runs the dispatcher (timers included) until a window of type <typeparamref name="T"/> is visible.
		/// </summary>
		public async Task<T> WaitFor<T>(TimeSpan? timeout = null) where T : Window
		{
			var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(10));
			while (DateTime.UtcNow < deadline)
			{
				Dispatcher.UIThread.RunJobs();
				var window = VisibleOf<T>().LastOrDefault();
				if (window != null)
					return window;
				await Task.Delay(20);
			}
			Assert.Fail($"{typeof(T).Name} was not opened");
			return null;
		}

		/// <summary>
		/// Runs the dispatcher for the given time so that pending timers can fire.
		/// </summary>
		public static async Task Pump(TimeSpan duration)
		{
			var deadline = DateTime.UtcNow + duration;
			while (DateTime.UtcNow < deadline)
			{
				Dispatcher.UIThread.RunJobs();
				await Task.Delay(20);
			}
			Dispatcher.UIThread.RunJobs();
		}

		/// <summary>
		/// Closes all visible message boxes (like pressing Escape) and returns their titles.
		/// </summary>
		public IReadOnlyList<string> DismissMessageBoxes()
		{
			var boxes = VisibleOf<MessageBox>();
			foreach (var box in boxes)
				box.Close();
			Dispatcher.UIThread.RunJobs();
			return boxes.Select(b => b.Title).ToList();
		}

		public void Dispose()
		{
			subscription.Dispose();
			// don't let a failed test leave dialogs behind for the next one
			DismissMessageBoxes();
			foreach (var progressView in VisibleOf<ProgressViewDialog>())
				progressView.Quit();
			Dispatcher.UIThread.RunJobs();
		}
	}
}
