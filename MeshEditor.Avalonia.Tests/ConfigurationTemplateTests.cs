using System;
using System.IO;
using MeshEditor.Common;
using MeshEditor.SolutionManager;
using NUnit.Framework;

namespace MeshEditor.AvaloniaUI.Tests
{
	/// <summary>
	/// The installation config.json (MeshEditor.Avalonia/config.json, next to the executable) is the template of the user's
	/// configuration, applied when a newer version starts. Its remote storage values are empty for now.
	/// </summary>
	public class ConfigurationTemplateTests
	{
		private string originalConfigurationDirectory;
		private string directory;

		[SetUp]
		public void SetUp()
		{
			originalConfigurationDirectory = ConfigurationManager.ConfigurationDirectory;
			directory = Path.Combine(Path.GetTempPath(), "MeshEditorTests", Guid.NewGuid().ToString("N"));
			ConfigurationManager.ConfigurationDirectory = directory;
		}

		[TearDown]
		public void TearDown()
		{
			ConfigurationManager.ConfigurationDirectory = originalConfigurationDirectory;
			ConfigurationManager.LoadConfiguration();
			if (Directory.Exists(directory))
				Directory.Delete(directory, recursive: true);
		}

		[Test]
		public void Template_is_installed_next_to_the_executable()
		{
			Assert.That(File.Exists(Path.Combine(AppContext.BaseDirectory, "config.json")), Is.True);
		}

		[Test]
		public void First_start_gets_the_empty_remote_storage_values_of_the_template()
		{
			ConfigurationManager.LoadConfiguration();

			var azure = ConfigurationManager.GetConfigurationObject<AzureBlobStorageConfiguration>("AzureBlobStorage");
			Assert.That(azure, Is.Not.Null, "the section shows the user what to fill in");
			Assert.That((azure.AccountUrl, azure.TenantId, azure.ClientId), Is.EqualTo(("", "", "")));
			Assert.That(azure.IsComplete, Is.False, "no remote solutions");
		}

		[Test]
		public void New_version_keeps_the_remote_storage_values_of_the_user()
		{
			Directory.CreateDirectory(directory);
			File.WriteAllText(Path.Combine(directory, "config.json"),
				"{\"AssemblyVersion\":\"1.0.0.0\",\"AzureBlobStorage\":{\"AccountUrl\":\"https://account.blob.core.windows.net\",\"TenantId\":\"tenant\",\"ClientId\":\"client\",\"SolutionsBlobContainerName\":\"my-solutions\"}}");

			ConfigurationManager.LoadConfiguration();

			var azure = ConfigurationManager.GetConfigurationObject<AzureBlobStorageConfiguration>("AzureBlobStorage");
			Assert.That((azure.AccountUrl, azure.TenantId, azure.ClientId), Is.EqualTo(("https://account.blob.core.windows.net", "tenant", "client")), "empty template values don't erase them");
			Assert.That(azure.SolutionsBlobContainerName, Is.EqualTo("my-solutions"), "properties missing in the template are kept");
			Assert.That(azure.IsComplete, Is.True);
		}
	}
}
