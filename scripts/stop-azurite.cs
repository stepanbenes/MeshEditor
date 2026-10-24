#!/usr/bin/env dotnet

// Stops the Azurite that scripts/start-azurite.cs started. See MeshEditor.Web/README.md.
//
//   dotnet run scripts/stop-azurite.cs

using System.Diagnostics;
using System.Runtime.CompilerServices;

string pidPath = Path.GetFullPath(Path.Combine(ScriptDirectory(), "..", ".azurite", "azurite.pid"));
if (!File.Exists(pidPath))
{
	Console.Error.WriteLine("Azurite wasn't started by scripts/start-azurite.cs (no .azurite/azurite.pid).");
	return 1;
}

string[] lines = File.ReadAllLines(pidPath);
int pid = int.Parse(lines[0]);
long startTicks = long.Parse(lines[1]);
File.Delete(pidPath);

Process process;
try
{
	process = Process.GetProcessById(pid);
}
catch (ArgumentException)
{
	Console.WriteLine("Azurite isn't running anymore.");
	return 0;
}

using (process)
{
	// the saved start time tells a reused PID from the started process
	if (Math.Abs(process.StartTime.ToUniversalTime().Ticks - startTicks) > TimeSpan.TicksPerSecond)
	{
		Console.WriteLine("Azurite isn't running anymore.");
		return 0;
	}
	// on Windows, Azurite runs in a child process of the shell
	process.Kill(entireProcessTree: true);
	process.WaitForExit();
}
Console.WriteLine($"Azurite (process {pid}) is stopped.");
return 0;

static string ScriptDirectory([CallerFilePath] string path = "") => Path.GetDirectoryName(path)!;
