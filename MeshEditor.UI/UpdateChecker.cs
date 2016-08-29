using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.WinUI
{
	class UpdateChecker
	{
		private static readonly string updatesUri = @"https://feastorage.blob.core.windows.net/mesheditor-update";

		private readonly string clientFolder, architectureFolder;

		public UpdateChecker()
		{
			switch (Environment.OSVersion.Platform)
			{
				case PlatformID.Unix:
					clientFolder = "linux";
					break;
				default:
					throw new NotSupportedException("Updates are not available for this platform.");
			}

			architectureFolder = Environment.Is64BitOperatingSystem ? "x64" : "x86";

			CurrentVersion = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion;
		}

		public string CurrentVersion { get; }

		public string ServerVersion { get; private set; }

		public string PackageFileUri { get; private set; }

		public async Task<bool> CheckForUpdates()
		{
			string releasesFileUri = $"{updatesUri}/{clientFolder}/{architectureFolder}/RELEASES";
			string releasesFileContent;
			using (var webClient = new WebClient())
			{
				releasesFileContent = await webClient.DownloadStringTaskAsync(releasesFileUri);
			}

			string[] lines = releasesFileContent.Split(new [] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
			if (lines.Length > 0)
			{
				string[] lineParts = lines[0].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
				ServerVersion = lineParts[0];
				string packageName = lineParts[1];

				PackageFileUri = $"{updatesUri}/{clientFolder}/{architectureFolder}/{packageName}";

				return string.CompareOrdinal(CurrentVersion, ServerVersion) < 0;
			}
			return false;
		}
	}
}
