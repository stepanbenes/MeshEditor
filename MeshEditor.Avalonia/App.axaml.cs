using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using MeshEditor.AvaloniaUI.Dialogs;
using MeshEditor.AvaloniaUI.Rendering;

namespace MeshEditor.AvaloniaUI
{
	public partial class App : Application
	{
		public override void Initialize()
		{
			AvaloniaXamlLoader.Load(this);
		}

		public override void OnFrameworkInitializationCompleted()
		{
			if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
			{
				try
				{
					desktop.MainWindow = new MainWindow(desktop.Args ?? System.Array.Empty<string>());
				}
				catch (GraphicsInitializationException ex)
				{
					// without OpenGL there is nothing to draw the mesh with: explain why instead of crashing
					Console.Error.WriteLine(ex.Message);
					desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown; // exit code 1 when the message box closes
					_ = showErrorAndExit(desktop, ex.Message);
				}
			}

			base.OnFrameworkInitializationCompleted();
		}

		private static async Task showErrorAndExit(IClassicDesktopStyleApplicationLifetime desktop, string message)
		{
			await MessageBox.Show(message, "MeshEditor cannot start", MessageBoxButtons.OK, MessageBoxIcon.Error);
			desktop.Shutdown(1);
		}
	}
}
