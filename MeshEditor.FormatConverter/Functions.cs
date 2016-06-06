using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using MeshEditor.LayerManager.Common;

namespace MeshEditor.FormatConverter
{
	public class Functions
	{
		// This function will get triggered/executed when a new message is written 
		// on an Azure Queue called queue.
		public static void ConvertResults([QueueTrigger("results-converter-queue")] string message, TextWriter log)
		{
			var program = new Program(isRunningLocally: false, storageType: StorageType.Remote);
			program.Run(message.SplitToTokensWithQuotes(), log);
		}
	}
}
