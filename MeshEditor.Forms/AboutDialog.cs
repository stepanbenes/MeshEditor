using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace MeshEditor.WinUI
{
	/// <summary>
	/// dialog pro informovani uzivatele o zakladnich parametrech programu. nazev, autor, rok vydani...
	/// </summary>
	public partial class AboutDialog : Form
	{
		public AboutDialog()
		{
			InitializeComponent();

			AssemblyWithInfo = getAssemblyWithInfo();

			this.Text = String.Format("About {0}", AssemblyTitle);
			this.labelProductName.Text = AssemblyProduct;
			this.labelVersion.Text = String.Format("Version {0}", FileVersion);
			this.labelCopyright.Text = AssemblyCopyright;
			this.labelCompanyName.Text = AssemblyCompany;
			this.textBoxDescription.Text = AssemblyDescription;
		}

		private Assembly getAssemblyWithInfo()
		{
			return Assembly.GetEntryAssembly();
		}

		#region Assembly Attribute Accessors

		public Assembly AssemblyWithInfo { get; }

		public string AssemblyTitle
		{
			get
			{
				object[] attributes = AssemblyWithInfo.GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
				if (attributes.Length > 0)
				{
					AssemblyTitleAttribute titleAttribute = (AssemblyTitleAttribute)attributes[0];
					if (titleAttribute.Title != "")
					{
						return titleAttribute.Title;
					}
				}
				return System.IO.Path.GetFileNameWithoutExtension(AssemblyWithInfo.Location);
			}
		}

		public string AssemblyVersion
		{
			get
			{
				return AssemblyWithInfo.GetName().Version.ToString();
			}
		}

		public string FileVersion
		{
			get
			{
				FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(AssemblyWithInfo.Location);
				return fileVersionInfo.FileVersion;
			}
		}

		public string AssemblyDescription
		{
			get
			{
				object[] attributes = AssemblyWithInfo.GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false);
				if (attributes.Length == 0)
				{
					return "";
				}
				return ((AssemblyDescriptionAttribute)attributes[0]).Description;
			}
		}

		public string AssemblyProduct
		{
			get
			{
				object[] attributes = AssemblyWithInfo.GetCustomAttributes(typeof(AssemblyProductAttribute), false);
				if (attributes.Length == 0)
				{
					return "";
				}
				return ((AssemblyProductAttribute)attributes[0]).Product;
			}
		}

		public string AssemblyCopyright
		{
			get
			{
				object[] attributes = AssemblyWithInfo.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
				if (attributes.Length == 0)
				{
					return "";
				}
				return ((AssemblyCopyrightAttribute)attributes[0]).Copyright;
			}
		}

		public string AssemblyCompany
		{
			get
			{
				object[] attributes = AssemblyWithInfo.GetCustomAttributes(typeof(AssemblyCompanyAttribute), false);
				if (attributes.Length == 0)
				{
					return "";
				}
				return ((AssemblyCompanyAttribute)attributes[0]).Company;
			}
		}

		#endregion
	}
}
