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

		public static bool IsUpdateServiceAvailableForThisPlatform => Environment.OSVersion.Platform == PlatformID.Unix;

		public UpdateChecker()
		{
			CurrentVersion = Version.Parse(FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion);
		}

		public Version CurrentVersion { get; }

		public Version ServerVersion { get; private set; }

		public string PackageFileUri { get; private set; }

		public async Task<bool> CheckForUpdates()
		{
			Debug.Assert(IsUpdateServiceAvailableForThisPlatform);

			ServerVersion = null;
			PackageFileUri = null;

			string clientFolder;
			switch (Environment.OSVersion.Platform)
			{
				case PlatformID.Unix:
					clientFolder = "linux";
					break;
				default:
					throw new NotSupportedException("Updates are not available for this platform.");
			}

			string architectureFolder = Environment.Is64BitOperatingSystem ? "x64" : "x86";

			string releasesFileUri = $"{updatesUri}/{clientFolder}/{architectureFolder}/releases.txt";
			string releasesFileContent;
			using (var webClient = new WebClient())
			{
				releasesFileContent = await webClient.DownloadStringTaskAsync(releasesFileUri);
			}

			string[] lines = releasesFileContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
			if (lines.Length > 0)
			{
				// First line is considered as latest realease info
				string[] lineParts = lines[0].Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
				ServerVersion = Version.Parse(lineParts[0]);
				string packageName = lineParts[1];

				PackageFileUri = $"{updatesUri}/{clientFolder}/{architectureFolder}/{packageName}";

				return CurrentVersion < ServerVersion;
			}
			return false;
		}
	}
}
