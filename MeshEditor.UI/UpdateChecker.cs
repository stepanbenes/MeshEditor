using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.WinUI
{
	class UpdateChecker
	{
		private static readonly HttpClient HttpClient = new HttpClient();

		public static string MeshEditorUpdateUri => @"https://feastorage.blob.core.windows.net/mesheditor-update";

		// only linux/x64 packages are published (PlatformID.Unix would also match macOS, Is64BitOperatingSystem also arm64)
		public static bool IsUpdateServiceAvailableForThisPlatform => OperatingSystem.IsLinux() && RuntimeInformation.OSArchitecture == Architecture.X64;

		public UpdateChecker()
		{
			CurrentVersion = Version.Parse(FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion);
		}

		public Version CurrentVersion { get; }

		public Version ServerVersion { get; private set; }

		public string PackageFileUri { get; private set; }

		public async Task<bool> CheckForUpdates()
		{
			ServerVersion = null;
			PackageFileUri = null;

			if (!IsUpdateServiceAvailableForThisPlatform)
				throw new NotSupportedException($"Updates are not available for this platform ({RuntimeInformation.OSDescription}, {RuntimeInformation.OSArchitecture}).");
			string clientFolder = "linux";
			string architectureFolder = "x64";

			string releasesFileUri = $"{MeshEditorUpdateUri}/{clientFolder}/{architectureFolder}/releases.txt";
			string releasesFileContent = await HttpClient.GetStringAsync(releasesFileUri);

			string[] lines = releasesFileContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
			if (lines.Length > 0)
			{
				// First line is considered as latest realease info
				string[] lineParts = lines[0].Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
				ServerVersion = Version.Parse(lineParts[0]);
				string packageName = lineParts[1];

				PackageFileUri = $"{MeshEditorUpdateUri}/{clientFolder}/{architectureFolder}/{packageName}";

				return CurrentVersion < ServerVersion;
			}
			return false;
		}
	}
}
