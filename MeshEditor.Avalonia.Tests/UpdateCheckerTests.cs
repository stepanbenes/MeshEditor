using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using MeshEditor.WinUI;
using NUnit.Framework;

namespace MeshEditor.AvaloniaUI.Tests
{
	/// <summary>
	/// The update check against a public blob container in Azurite, laid out like the update service (scripts/publish-update.cs):
	/// &lt;os&gt;/x64/releases.txt with the latest release on the first line. Ignored when Azurite is not running.
	/// </summary>
	public class UpdateCheckerTests
	{
		private const string AzuriteConnectionString = "UseDevelopmentStorage=true";

		private BlobContainerClient container;
		private string originalUpdateUri;

		private static string platformFolder => OperatingSystem.IsWindows() ? "windows/x64" : "linux/x64";

		[SetUp]
		public async Task SetUp()
		{
			Assume.That(UpdateChecker.IsUpdateServiceAvailableForThisPlatform, "no update service for this platform");
			if (!isAzuriteRunning())
				Assert.Ignore("No test storage: start Azurite.");
			container = new BlobServiceClient(AzuriteConnectionString).GetBlobContainerClient("mesheditor-update-test-" + Guid.NewGuid().ToString("N"));
			await container.CreateAsync(PublicAccessType.Blob);
			originalUpdateUri = UpdateChecker.MeshEditorUpdateUri;
			UpdateChecker.MeshEditorUpdateUri = container.Uri.ToString();
		}

		[TearDown]
		public async Task TearDown()
		{
			if (originalUpdateUri != null)
				UpdateChecker.MeshEditorUpdateUri = originalUpdateUri;
			if (container != null)
				await container.DeleteIfExistsAsync();
		}

		private async Task publishReleasesAsync(string releases)
		{
			await container.GetBlobClient(platformFolder + "/releases.txt").UploadAsync(BinaryData.FromString(releases));
		}

		[Test]
		public async Task Newer_release_is_an_update_with_the_package_link()
		{
			// the releases.txt files published before scripts/publish-update.cs start with a byte order mark
			await publishReleasesAsync("﻿99.0.0 package-99.0.0\r\n2.5.2 package-2.5.2\r\n");

			var updateChecker = new UpdateChecker();
			Assert.That(await updateChecker.CheckForUpdates(), Is.True);
			Assert.That(updateChecker.ServerVersion, Is.EqualTo(new Version(99, 0, 0)));
			Assert.That(updateChecker.PackageFileUri, Is.EqualTo($"{container.Uri}/{platformFolder}/package-99.0.0"));
		}

		[Test]
		public async Task Current_version_is_not_an_update()
		{
			var updateChecker = new UpdateChecker();
			await publishReleasesAsync($"{updateChecker.CurrentVersion} package\n");

			Assert.That(await updateChecker.CheckForUpdates(), Is.False);
			Assert.That(updateChecker.ServerVersion, Is.EqualTo(updateChecker.CurrentVersion));
		}

		[Test]
		public async Task No_releases_for_the_platform_is_not_an_update()
		{
			var updateChecker = new UpdateChecker();

			Assert.That(await updateChecker.CheckForUpdates(), Is.False);
			Assert.That(updateChecker.ServerVersion, Is.Null);
		}

		private static bool isAzuriteRunning()
		{
			try
			{
				using var client = new TcpClient();
				return client.ConnectAsync("127.0.0.1", 10000).Wait(TimeSpan.FromSeconds(1)) && client.Connected;
			}
			catch (Exception)
			{
				return false;
			}
		}
	}
}
