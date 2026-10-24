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

namespace MeshEditor.AvaloniaUI
{
	class UpdateChecker
	{
		private static readonly HttpClient HttpClient = new HttpClient();

		// the public blob container of the update service (tests point it to Azurite); scripts/publish-update.cs publishes there
		public static string MeshEditorUpdateUri { get; set; } = @"https://feastorage.blob.core.windows.net/mesheditor-update";

		// packages are published for linux/x64 (Debian package) and windows/x64 (zip), each folder with its releases.txt
		// (PlatformID.Unix would also match macOS, Is64BitOperatingSystem also arm64)
		public static bool IsUpdateServiceAvailableForThisPlatform => clientFolder != null && RuntimeInformation.OSArchitecture == Architecture.X64;

		private static string clientFolder => OperatingSystem.IsLinux() ? "linux" : OperatingSystem.IsWindows() ? "windows" : null;

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
			string architectureFolder = "x64";

			string releasesFileUri = $"{MeshEditorUpdateUri}/{clientFolder}/{architectureFolder}/releases.txt";
			string releasesFileContent;
			try
			{
				releasesFileContent = await HttpClient.GetStringAsync(releasesFileUri);
			}
			catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
			{
				return false; // nothing has been released for this platform yet
			}

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
