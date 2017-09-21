using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using MeshEditor.SolutionManager.CommandLine;
using MeshEditor.SolutionManager.Logging;

namespace MeshEditor.FormatConverter.FunctionApp
{
	public static class FemFormatConversionFunction
	{
		[FunctionName("FemFormatConversionFunction")]
		public static void Run([QueueTrigger("format-conversion-queue", Connection = "")] string message, TraceWriter log)
		{
			log.Info($"C# Queue trigger function processed: {message}");

			var program = new CommandLineParser(isRunningLocally: false, storageType: StorageType.Remote, logger: new TraceLogger(log));
			program.Run(message);
		}
	}
}
