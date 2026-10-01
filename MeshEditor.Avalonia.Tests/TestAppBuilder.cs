using System.Globalization;
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
			// the headless test session runs Avalonia on its own (non-main) thread; GLFW works there on Windows/Linux
			OpenTK.Windowing.Desktop.GLFWProvider.CheckForMainThread = false;
			return AppBuilder.Configure<App>()
				.UseSkia()
				.UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
		}
	}
}
