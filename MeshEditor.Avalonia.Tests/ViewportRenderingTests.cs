using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using MeshEditor.CoreInterface;
using NUnit.Framework;

namespace MeshEditor.AvaloniaUI.Tests
{
	/// <summary>
	/// Smoke tests of the OpenGL viewport (real OpenGL rendering into the offscreen framebuffer).
	/// </summary>
	public class ViewportRenderingTests
	{
		private static string samplePath => Path.Combine(AppContext.BaseDirectory, "TestData", "sample.obj");

		private static async Task<MainWindow> openWindowWithSample()
		{
			var window = new MainWindow(new[] { samplePath })
			{
				WindowState = WindowState.Normal,
				Width = 800,
				Height = 600,
			};
			window.Show();
			var timeout = DateTime.UtcNow.AddSeconds(30);
			while (!window.ActiveViewport.SceneFacade.ContainsMesh || window.ActiveViewport.IOActionInProgress)
			{
				if (DateTime.UtcNow > timeout)
					Assert.Fail("mesh was not loaded in time");
				Dispatcher.UIThread.RunJobs();
				await Task.Delay(20);
			}
			Dispatcher.UIThread.RunJobs();
			return window;
		}

		private static (byte b, byte g, byte r) pixel(byte[] bgra, int width, int x, int y)
		{
			int i = (y * width + x) * 4;
			return (bgra[i], bgra[i + 1], bgra[i + 2]);
		}

		[AvaloniaTest]
		public async Task Loads_sample_mesh_and_updates_caption_and_status()
		{
			var window = await openWindowWithSample();

			Assert.That(window.Title, Is.EqualTo("MeshEditor - sample"));
			Assert.That(window.statusLabel.Text, Is.Not.Empty);
			Assert.That(window.statusLabel.Text, Does.Not.StartWith("Loading"));
		}

		[AvaloniaTest]
		public async Task Renders_mesh_in_the_middle_of_the_viewport()
		{
			var window = await openWindowWithSample();
			var viewport = window.ActiveViewport;

			byte[] pixels = viewport.TakeScreenshot(System.Drawing.Rectangle.Empty, out int width, out int height);

			Assert.That(width, Is.EqualTo(viewport.PixelSize.Width));
			Assert.That(height, Is.EqualTo(viewport.PixelSize.Height));
			var background = pixel(pixels, width, 2, 2);
			var center = pixel(pixels, width, width / 2, height / 2);
			Assert.That(center, Is.Not.EqualTo(background), "the mesh (zoomed to fit) covers the centre of the viewport");
			Assert.That(center.r > 200 && center.g > 200 && center.b > 200, Is.True, $"white face in the centre, got {center}");
		}

		[AvaloniaTest]
		public async Task Picking_depth_reads_work_with_multisampled_frame()
		{
			// the visible frame is drawn multisampled; the depth passes and glReadPixels of the core must use the single-sampled framebuffer
			var window = await openWindowWithSample();
			var viewport = window.ActiveViewport;
			var scene = viewport.SceneFacade;
			viewport.Refresh();

			var center = new System.Drawing.Point(viewport.PixelSize.Width / 2, viewport.PixelSize.Height / 2);
			scene.MouseDownHandler(center);
			string statusOnMesh = (string)scene.GetValue(AvailableValue.Status);
			scene.MouseUpHandler(center, MouseButton.Left);

			var corner = new System.Drawing.Point(3, 3);
			scene.MouseDownHandler(corner);
			string statusOnBackground = (string)scene.GetValue(AvailableValue.Status);
			scene.MouseUpHandler(corner, MouseButton.Left);

			Assert.That(statusOnMesh, Does.StartWith("Point under cursor"));
			Assert.That(statusOnBackground ?? string.Empty, Does.Not.StartWith("Point under cursor"));
		}

		[AvaloniaTest]
		public async Task Signal_node_draws_signal_lines()
		{
			var window = await openWindowWithSample();
			var viewport = window.ActiveViewport;
			byte[] before = viewport.TakeScreenshot(System.Drawing.Rectangle.Empty, out int width, out _);

			viewport.SignalNodeByID(new[] { 1 });
			Dispatcher.UIThread.RunJobs();

			Assert.That((bool)viewport.SceneFacade.GetValue(AvailableValue.NodeSignalIsSet), Is.True);
			byte[] after = viewport.TakeScreenshot(System.Drawing.Rectangle.Empty, out _, out _);
			Assert.That(after, Is.Not.EqualTo(before), "signal lines are rendered");
		}

		[AvaloniaTest]
		public async Task Split_window_creates_second_viewport_sharing_the_mesh()
		{
			var window = await openWindowWithSample();

			window.splitActiveWindowToolStripMenuItem.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(MenuItem.ClickEvent));
			Dispatcher.UIThread.RunJobs();

			Assert.That(window.Viewports.Count, Is.EqualTo(2));
			Assert.That(window.Viewports[1].SceneFacade.ContainsMesh, Is.True);
			byte[] pixels = window.Viewports[1].TakeScreenshot(System.Drawing.Rectangle.Empty, out int width, out int height);
			var center = pixel(pixels, width, width / 2, height / 2);
			Assert.That(center.r > 200 && center.g > 200 && center.b > 200, Is.True, $"second viewport renders the mesh, got {center}");
		}
	}
}
