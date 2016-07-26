using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.DataVisualizer.Services
{
	static class LayerManagerProcessInvokeService
	{
		public static Task<int> Invoke(string arguments)
		{
			Process process = new Process();
			process.EnableRaisingEvents = true;
			// Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().CodeBase), layerManagerExecutableName
			process.StartInfo = new ProcessStartInfo("layer", arguments) { UseShellExecute = true };
			TaskCompletionSource<int> tcs = new TaskCompletionSource<int>();
			process.Exited += (s, e) => tcs.TrySetResult(process.ExitCode);
			process.Start();
			return tcs.Task;
		}
	}
}
