using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace MeshEditor.AvaloniaUI.Dialogs
{
	/// <summary>
	/// About dialog (port of MeshEditor.WinUI.AboutDialog).
	/// </summary>
	public partial class AboutDialog : Window
	{
		public AboutDialog()
		{
			InitializeComponent();
			AssemblyWithInfo = getAssemblyWithInfo();
			this.Title = String.Format("About {0}", AssemblyTitle);
			this.labelProductName.Text = AssemblyProduct;
			this.labelVersion.Text = String.Format("Version {0}", FileVersion);
			this.labelCopyright.Text = AssemblyCopyright;
			this.labelCompanyName.Text = AssemblyCompany;
			this.textBoxDescription.Text = AssemblyDescription;
		}

		private static Assembly getAssemblyWithInfo()
		{
			// the entry assembly; under a test runner the application assembly itself
			var entryAssembly = Assembly.GetEntryAssembly();
			return entryAssembly?.GetCustomAttribute<AssemblyProductAttribute>()?.Product == "MeshEditor" ? entryAssembly : typeof(AboutDialog).Assembly;
		}

		public new async Task ShowDialog(Window owner)
		{
			owner ??= DialogOwner.Current;
			if (owner != null)
				await base.ShowDialog(owner);
			else
			{
				var closed = new TaskCompletionSource();
				Closed += (s, e) => closed.TrySetResult();
				WindowStartupLocation = WindowStartupLocation.CenterScreen;
				Show();
				await closed.Task;
			}
		}

		private void okButton_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}

		#region Assembly Attribute Accessors

		public Assembly AssemblyWithInfo { get; }

		public string AssemblyTitle
		{
			get
			{
				var titleAttribute = AssemblyWithInfo.GetCustomAttribute<AssemblyTitleAttribute>();
				if (titleAttribute != null && titleAttribute.Title != "")
					return titleAttribute.Title;
				return System.IO.Path.GetFileNameWithoutExtension(AssemblyWithInfo.Location);
			}
		}

		public string AssemblyVersion => AssemblyWithInfo.GetName().Version.ToString();

		public string FileVersion => FileVersionInfo.GetVersionInfo(AssemblyWithInfo.Location).FileVersion;

		public string AssemblyDescription => AssemblyWithInfo.GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description ?? "";

		public string AssemblyProduct => AssemblyWithInfo.GetCustomAttribute<AssemblyProductAttribute>()?.Product ?? "";

		public string AssemblyCopyright => AssemblyWithInfo.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright ?? "";

		public string AssemblyCompany => AssemblyWithInfo.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company ?? "";

		#endregion
	}
}
