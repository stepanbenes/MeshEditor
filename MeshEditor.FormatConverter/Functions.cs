using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;

namespace MeshEditor.FormatConverter
{
	public class Functions
	{
		// This function will get triggered/executed when a new message is written 
		// on an Azure Queue called queue.
		public static void ProcessQueueMessage([QueueTrigger("results-converter-queue")] string message, TextWriter log)
		{
			Console.WriteLine(message);

			var program = new Program();
			program.Run(message.Split(' ', '\t'));
		}
	}
}
