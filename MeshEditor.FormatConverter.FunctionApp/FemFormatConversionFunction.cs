using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using MeshEditor.SolutionManager.CommandLine;
using MeshEditor.Common;
using MeshEditor.SolutionManager;

namespace MeshEditor.FormatConverter.FunctionApp
{
	public static class FemFormatConversionFunction
	{
		[FunctionName("FemFormatConversionFunction")]
		public static void Run([QueueTrigger("format-conversion-queue", Connection = "")] string message, TraceWriter log)
		{
			configure();

			var program = new CommandLineParser(isRunningLocally: false, storageType: StorageType.Remote, logger: new TraceLogger(log));
			program.Run(message);
		}

		private static void configure()
		{
			var azureBlobStorageConfiguration = new AzureBlobStorageConfiguration
			{
				ConnectionString = Environment.GetEnvironmentVariable("feastorage_connection_string"),
				LayersBlobContainerName = Environment.GetEnvironmentVariable("feastorage_LayersBlobContainerName"),
				ResultsBlobContainerName = Environment.GetEnvironmentVariable("feastorage_ResultsBlobContainerName")
			};

			ConfigurationManager.SetConfigurationObject("AzureBlobStorage", azureBlobStorageConfiguration);

			var restApiConfiguration = new RestApiConfiguration
			{
				Uri = Environment.GetEnvironmentVariable("RestApiUri")
			};

			ConfigurationManager.SetConfigurationObject("RestApi", restApiConfiguration);
		}
	}
}
