using System;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia;
using Avalonia.Threading;

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

			if (!System.Diagnostics.Debugger.IsAttached)
			{
				// report UI thread exceptions (e.g. from async void handlers) and keep running, like
				// Application.ThreadException + UnhandledExceptionMode.CatchException in the WinForms UI
				Dispatcher.UIThread.UnhandledException += (s, e) =>
				{
					e.Handled = true;
					Console.Error.WriteLine("Thread exception: " + e.Exception);
					_ = Dialogs.MessageBox.Show($"{e.Exception.GetType()}: {e.Exception.Message}", "Thread exception", Dialogs.MessageBoxButtons.OK, Dialogs.MessageBoxIcon.Error);
				};
			}

			if (OperatingSystem.IsLinux() && !AreX11SessionManagementLibrariesInstalled())
			{
				Console.Error.WriteLine($"MeshEditor needs the X11 session management libraries {string.Join(", ", x11SessionManagementLibraries.Select(library => library.File))}. "
					+ $"Install them with: sudo apt install {string.Join(" ", x11SessionManagementLibraries.Select(library => library.Package))}");
				return 1;
			}

			return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
		}

		// Avalonia's X11 backend always connects to the session manager and crashes at startup without libICE and libSM,
		// which are not installed on WSL or a minimal Ubuntu
		private static readonly (string File, string Package)[] x11SessionManagementLibraries =
		[
			("libICE.so.6", "libice6"),
			("libSM.so.6", "libsm6"),
		];

		private static bool AreX11SessionManagementLibrariesInstalled()
		{
			foreach ((string file, _) in x11SessionManagementLibraries)
			{
				if (!NativeLibrary.TryLoad(file, out IntPtr handle))
					return false;
				NativeLibrary.Free(handle);
			}
			return true;
		}

		public static AppBuilder BuildAvaloniaApp()
			=> AppBuilder.Configure<App>()
				.UsePlatformDetect()
				.WithInterFont()
				.LogToTrace();
	}
}
