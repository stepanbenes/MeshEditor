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
			OpenTK.Toolkit.Init();

			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.CurrentCulture = MeshEditor.IO.CultureProvider.EnglishCulture; // sets us language culture
			Application.Run(new MainForm(args));
		}
	}
}
