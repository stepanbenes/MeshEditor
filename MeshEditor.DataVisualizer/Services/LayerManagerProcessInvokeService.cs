using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.DataVisualizer.Services
{
	static class LayerManagerProcessInvokeService
	{
		public static Task<int> Invoke(string arguments)
		{
			Process process = new Process();

			process.StartInfo = createProcessStartInfo(arguments);

			process.EnableRaisingEvents = true;

			TaskCompletionSource<int> tcs = new TaskCompletionSource<int>();
			process.Exited += (s, e) =>
			{
				tcs.SetResult(process.ExitCode);
			};

			process.Start();

			return tcs.Task;
		}

		private static ProcessStartInfo createProcessStartInfo(string arguments)
		{
			var startInfo = new ProcessStartInfo("fem-format-converter", arguments)
			{
				UseShellExecute = true
			};
			return startInfo;
		}
	}
}
