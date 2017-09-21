using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.SolutionManager.CommandLine
{
	public struct ConsoleBrush : IDisposable
	{
		readonly ConsoleColor colorToRestore;

		public ConsoleBrush(ConsoleColor color)
		{
			colorToRestore = Console.ForegroundColor;
			Console.ForegroundColor = color;
		}

		public void Dispose()
		{
			Console.ForegroundColor = colorToRestore;
		}
	}
}
