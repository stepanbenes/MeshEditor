using System;
using System.Globalization;
using System.Threading;
using Avalonia;

namespace MeshEditor.AvaloniaUI
{
	internal static class Program
	{
		[STAThread]
		public static int Main(string[] args)
		{
			// sets US language culture (Application.CurrentCulture in the WinForms UI) - file formats depend on it
			CultureInfo culture = MeshEditor.IO.CultureProvider.EnglishCulture;
			CultureInfo.DefaultThreadCurrentCulture = culture;
			CultureInfo.DefaultThreadCurrentUICulture = culture;
			Thread.CurrentThread.CurrentCulture = culture;
			Thread.CurrentThread.CurrentUICulture = culture;

			AppDomain.CurrentDomain.UnhandledException += (s, e) =>
				Console.Error.WriteLine("Unhandled exception: " + e.ExceptionObject);

			return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
		}

		public static AppBuilder BuildAvaloniaApp()
			=> AppBuilder.Configure<App>()
				.UsePlatformDetect()
				.WithInterFont()
				.LogToTrace();
	}
}
