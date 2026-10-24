using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using MeshEditor.AvaloniaUI.Dialogs;
using NUnit.Framework;

namespace MeshEditor.AvaloniaUI.Tests
{
	/// <summary>
	/// Windows opened and closed around loading a mesh.
	/// </summary>
	public class LoadingWindowsTests
	{
		[AvaloniaTest]
		public async Task Failed_load_leaves_no_progress_view_open()
		{
			// regression test for #12: the 500 ms progress-view timer fired while the error message box was open,
			// and the progress view it opened was never closed
			using var windows = new OpenWindows();
			string missingFile = Path.Combine(AppContext.BaseDirectory, "TestData", "missing.obj");
			var window = new MainWindow(new[] { missingFile }) { WindowState = WindowState.Normal, Width = 800, Height = 600 };
			window.Show();

			var errorBox = await windows.WaitFor<MessageBox>();
			Assert.That(errorBox.Title, Is.EqualTo("Error while loading mesh"));

			await OpenWindows.Pump(TimeSpan.FromMilliseconds(800)); // past the progress-view timer
			Assert.That(windows.VisibleOf<ProgressViewDialog>(), Is.Empty, "no progress view while the error box is open");

			windows.DismissMessageBoxes();
			await OpenWindows.Pump(TimeSpan.FromMilliseconds(100));

			Assert.That(window.ActiveViewport.IOActionInProgress, Is.False);
			Assert.That(windows.VisibleOf<ProgressViewDialog>(), Is.Empty, "no progress view after the error box is dismissed");
			Assert.That(windows.Visible, Is.EquivalentTo(new Window[] { window }), "only the main window is left open");
		}
	}
}
