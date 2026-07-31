using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace MeshEditor.WinUI
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
			if (!System.Diagnostics.Debugger.IsAttached)
			{
				// Add the event handler for handling UI thread exceptions to the event.
				Application.ThreadException += Application_ThreadException;

				// Set the unhandled exception mode to force all Windows Forms errors
				// to go through our handler.
				Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

				// Add the event handler for handling non-UI thread exceptions to the event. 
				AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
			}

			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.CurrentCulture = MeshEditor.IO.CultureProvider.EnglishCulture; // sets us language culture
			Application.Run(new MainForm(args));
		}

		private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			if (e.ExceptionObject is Exception error)
			{
				ReportError("Unhandled exception", error);
			}
			else
			{
				ReportError("Unhandled exception", e.ExceptionObject?.ToString() ?? "");
			}
		}

		private static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
		{
			ReportError("Thread exception", e.Exception);
		}

		public static void ReportError(string caption, Exception error)
		{
			string errorText = $"{error.GetType()}: {error.Message}";
			ReportError(caption, errorText);
		}

		public static void ReportError(string caption, string errorText)
		{
			Console.Error.WriteLine(errorText); 
			_ = MessageBox.Show(errorText, caption, MessageBoxButtons.OK, MessageBoxIcon.Error);
		}
	}
}
