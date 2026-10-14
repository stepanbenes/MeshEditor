using System;
using System.Globalization;
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
				Console.Error.WriteLine("MeshEditor needs the X11 session management libraries libICE and libSM. Install them with: sudo apt install libice6 libsm6");
				return 1;
			}

			return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
		}

		// Avalonia's X11 backend always connects to the session manager and crashes at startup without libICE and libSM,
		// which are not installed on WSL or a minimal Ubuntu
		private static bool AreX11SessionManagementLibrariesInstalled()
		{
			foreach (string library in new[] { "libICE.so.6", "libSM.so.6" })
			{
				if (!NativeLibrary.TryLoad(library, out IntPtr handle))
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
