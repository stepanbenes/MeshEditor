#!/usr/bin/env dotnet

// Starts Azurite (the Azure Storage emulator, blob service only) in the background for local testing of the web app.
// The first run installs Azurite from npm into .azurite; delete .azurite/node_modules to get a newer version.
// It keeps the data in memory unless --persist is given, then in .azurite/data. Its output goes to .azurite/azurite.log.
// Stop it with scripts/stop-azurite.cs. See MeshEditor.Web/README.md.
//
//   dotnet run scripts/start-azurite.cs [--persist]

using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

const string Host = "127.0.0.1";
const int Port = 10000;

bool persist = args.Contains("--persist");

string stateDirectory = Path.GetFullPath(Path.Combine(ScriptDirectory(), "..", ".azurite"));
string pidPath = Path.Combine(stateDirectory, "azurite.pid");
string logPath = Path.Combine(stateDirectory, "azurite.log");
string mainPath = Path.Combine(stateDirectory, "node_modules", "azurite", "dist", "src", "blob", "main.js");
Directory.CreateDirectory(stateDirectory);

if (await IsListeningAsync())
{
	Console.Error.WriteLine($"Something already listens at {Host}:{Port}. Azurite may be running already; stop it with scripts/stop-azurite.cs.");
	return 1;
}

// Azurite runs straight from node, not through npx: npx starts it a few processes deep, and stop-azurite.cs couldn't always find it
if (!File.Exists(mainPath))
{
	Console.WriteLine("Installing Azurite into .azurite ...");
	using var install = Process.Start(ShellStartInfo($"npm install --prefix \"{stateDirectory}\" --no-save --no-package-lock --no-audit --no-fund azurite@3"))!;
	await install.WaitForExitAsync();
	if (install.ExitCode != 0 || !File.Exists(mainPath))
	{
		Console.Error.WriteLine("Installing Azurite failed. It needs Node.js, see MeshEditor.Web/README.md.");
		return 1;
	}
}

var azuriteArguments = $"--blobHost {Host} --blobPort {Port} --loose --skipApiVersionCheck";
if (persist)
{
	string dataDirectory = Path.Combine(stateDirectory, "data");
	Directory.CreateDirectory(dataDirectory);
	azuriteArguments += $" --location \"{dataDirectory}\"";
}
else
	azuriteArguments += " --inMemoryPersistence";

// the shell redirects the output to the log file, so Azurite doesn't write into this console or depend on it after this script ends;
// on Unix, nohup keeps Azurite running when the terminal closes and exec keeps the PID that is saved for stop-azurite.cs
string command = $"node \"{mainPath}\" {azuriteArguments} > \"{logPath}\" 2>&1";
var startInfo = ShellStartInfo(OperatingSystem.IsWindows() ? command : $"exec nohup {command} < /dev/null");
startInfo.CreateNoWindow = true;
startInfo.WorkingDirectory = stateDirectory;

using var process = Process.Start(startInfo)!;
File.WriteAllLines(pidPath, [process.Id.ToString(), process.StartTime.ToUniversalTime().Ticks.ToString()]);

var deadline = DateTime.UtcNow.AddMinutes(1);
while (!await IsListeningAsync())
{
	if (process.HasExited)
	{
		File.Delete(pidPath);
		Console.Error.WriteLine($"Azurite exited with code {process.ExitCode}. Its output ({logPath}):");
		Console.Error.WriteLine(File.Exists(logPath) ? File.ReadAllText(logPath) : "(none)");
		return 1;
	}
	if (DateTime.UtcNow > deadline)
	{
		process.Kill(entireProcessTree: true);
		File.Delete(pidPath);
		Console.Error.WriteLine($"Azurite didn't start listening at {Host}:{Port} in time and was stopped. See {logPath}.");
		return 1;
	}
	await Task.Delay(500);
}

Console.WriteLine($"Azurite is running at http://{Host}:{Port} ({(persist ? "data in .azurite/data" : "data in memory")}), process {process.Id}. Output: {logPath}");
Console.WriteLine("Next: dotnet run scripts/setup-azurite.cs");
return 0;

static async Task<bool> IsListeningAsync()
{
	using var client = new TcpClient();
	try
	{
		await client.ConnectAsync(Host, Port).WaitAsync(TimeSpan.FromSeconds(1));
		return true;
	}
	catch (Exception ex) when (ex is SocketException or TimeoutException)
	{
		return false;
	}
}

static ProcessStartInfo ShellStartInfo(string command)
{
	var startInfo = new ProcessStartInfo { UseShellExecute = false };
	if (OperatingSystem.IsWindows())
	{
		// /s takes the command between the outer quotes as it is
		startInfo.FileName = "cmd.exe";
		startInfo.Arguments = $"/s /c \"{command}\"";
	}
	else
	{
		startInfo.FileName = "/bin/sh";
		startInfo.ArgumentList.Add("-c");
		startInfo.ArgumentList.Add(command);
	}
	return startInfo;
}

static string ScriptDirectory([CallerFilePath] string path = "") => Path.GetDirectoryName(path)!;
