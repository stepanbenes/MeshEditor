using System;
using System.Globalization;
using System.IO;
using Avalonia;
using Avalonia.Headless;

[assembly: AvaloniaTestApplication(typeof(MeshEditor.AvaloniaUI.Tests.TestAppBuilder))]

namespace MeshEditor.AvaloniaUI.Tests
{
	public class TestAppBuilder
	{
		public static AppBuilder BuildAvaloniaApp()
		{
			CultureInfo.DefaultThreadCurrentCulture = MeshEditor.IO.CultureProvider.EnglishCulture;
			// don't read (e.g. the last loaded mesh, which MainWindow opens on start) or write the user's configuration
			MeshEditor.Common.ConfigurationManager.ConfigurationDirectory = Path.Combine(Path.GetTempPath(), "MeshEditorTests", "config-" + Guid.NewGuid().ToString("N"));
			// the headless test session runs Avalonia on its own (non-main) thread; GLFW works there on Windows/Linux
			OpenTK.Windowing.Desktop.GLFWProvider.CheckForMainThread = false;
			return AppBuilder.Configure<App>()
				.UseSkia()
				.UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
		}
	}
}
