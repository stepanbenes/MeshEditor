#!/usr/bin/env dotnet

// Builds the PDF of the user guide (docs/user-guide/user-guide.md) that Help -> User guide opens: GitHub renders the
// Markdown to HTML (the Markdown API, so the PDF looks like the guide on github.com), the script wraps it in a page with
// print styles (A4, every chapter on a new page) and a headless Chrome or Edge prints that page to PDF.
// Publish the PDF with scripts/publish-update.cs --user-guide.
//
// The API works without signing in, with a low rate limit; set GITHUB_TOKEN to use a token. The browser is looked up
// in the usual places, --browser gives its path.
//
//   dotnet run scripts/build-user-guide-pdf.cs [<output pdf>] [--browser <path>]

using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using System.Runtime.CompilerServices;

const string Style = """
	body { font-family: "Segoe UI", "Helvetica Neue", Arial, sans-serif; font-size: 10.5pt; line-height: 1.45; color: #1f2328; margin: 0; }
	h1 { font-size: 22pt; border-bottom: 1px solid #d0d7de; padding-bottom: 4pt; }
	h2 { font-size: 16pt; border-bottom: 1px solid #d0d7de; padding-bottom: 3pt; break-before: page; }
	h3 { font-size: 12.5pt; }
	h4 { font-size: 11pt; }
	h2, h3, h4 { break-after: avoid; }
	img { max-width: 100%; break-inside: avoid; }
	p:has(img) { text-align: center; }
	table { border-collapse: collapse; margin: 6pt 0; }
	td, th { border: 1px solid #d0d7de; padding: 3pt 6pt; vertical-align: top; }
	th { background: #f6f8fa; }
	code { font-family: Consolas, Menlo, monospace; font-size: 9.5pt; background: #f6f8fa; padding: 0 2pt; }
	pre { background: #f6f8fa; padding: 6pt; font-size: 9pt; white-space: pre-wrap; break-inside: avoid; }
	pre code { padding: 0; }
	a { color: #0969da; text-decoration: none; }
	.anchor, hr { display: none; }
	@page { size: A4; margin: 16mm 15mm; }
	""";

string guideDirectory = Path.GetFullPath(Path.Combine(ScriptDirectory(), "..", "docs", "user-guide"));
string? output = null;
string? browser = null;
for (int i = 0; i < args.Length; i++)
{
	if (args[i] == "--browser" && i + 1 < args.Length)
		browser = args[++i];
	else if (output == null && !args[i].StartsWith("--"))
		output = args[i];
	else
	{
		Console.Error.WriteLine("Usage: dotnet run scripts/build-user-guide-pdf.cs [<output pdf>] [--browser <path>]");
		return 1;
	}
}
output = Path.GetFullPath(output ?? Path.Combine(guideDirectory, "user-guide.pdf"));
browser ??= FindBrowser();
if (browser == null || !File.Exists(browser))
{
	Console.Error.WriteLine("Chrome, Chromium or Edge was not found; give its path with --browser.");
	return 1;
}

// the Markdown API renders the guide like github.com does
string markdown = await File.ReadAllTextAsync(Path.Combine(guideDirectory, "user-guide.md"));
using var http = new HttpClient();
http.DefaultRequestHeaders.UserAgent.ParseAdd("MeshEditor-build-user-guide-pdf");
http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
if (Environment.GetEnvironmentVariable("GITHUB_TOKEN") is { Length: > 0 } token)
	http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
var request = new JsonObject { ["text"] = markdown, ["mode"] = "markdown" };
using var response = await http.PostAsync("https://api.github.com/markdown", new StringContent(request.ToJsonString(), Encoding.UTF8, "application/json"));
if (!response.IsSuccessStatusCode)
{
	Console.Error.WriteLine($"GitHub could not render the guide: {(int)response.StatusCode} {await response.Content.ReadAsStringAsync()}");
	return 1;
}
// GitHub prefixes the heading ids with user-content- and its scripts fix the links up; the PDF has no scripts
string body = (await response.Content.ReadAsStringAsync()).Replace("id=\"user-content-", "id=\"");

// the base makes the relative figure paths resolve in the guide folder
string baseUri = new Uri(guideDirectory + Path.DirectorySeparatorChar).AbsoluteUri;
string html = $"""
	<!doctype html>
	<html><head><meta charset="utf-8"><base href="{baseUri}"><title>MeshEditor User Guide</title>
	<style>{Style}</style></head>
	<body>{body}</body></html>
	""";
string temporaryDirectory = Directory.CreateTempSubdirectory("mesheditor-user-guide-").FullName;
try
{
	string htmlPath = Path.Combine(temporaryDirectory, "user-guide.html");
	await File.WriteAllTextAsync(htmlPath, html);
	File.Delete(output);
	var startInfo = new ProcessStartInfo(browser) { UseShellExecute = false, RedirectStandardError = true, RedirectStandardOutput = true };
	foreach (string argument in new[] { "--headless", "--disable-gpu", "--no-pdf-header-footer", "--allow-file-access-from-files",
		$"--user-data-dir={Path.Combine(temporaryDirectory, "profile")}", $"--print-to-pdf={output}", new Uri(htmlPath).AbsoluteUri })
		startInfo.ArgumentList.Add(argument);
	using var process = Process.Start(startInfo)!;
	Task<string> errors = process.StandardError.ReadToEndAsync();
	await process.StandardOutput.ReadToEndAsync();
	await process.WaitForExitAsync();
	if (!File.Exists(output))
	{
		Console.Error.WriteLine($"{Path.GetFileName(browser)} did not print the PDF (exit code {process.ExitCode}):");
		Console.Error.WriteLine(await errors);
		return 1;
	}
}
finally
{
	try { Directory.Delete(temporaryDirectory, true); } catch (IOException) { } // the browser may still hold its profile for a moment
}
Console.WriteLine($"{output} ({new FileInfo(output).Length / 1024} kB)");
return 0;

static string? FindBrowser()
{
	string[] candidates;
	if (OperatingSystem.IsWindows())
	{
		var roots = new[] { Environment.SpecialFolder.ProgramFiles, Environment.SpecialFolder.ProgramFilesX86, Environment.SpecialFolder.LocalApplicationData }
			.Select(Environment.GetFolderPath).Where(root => root.Length > 0);
		candidates = roots.SelectMany(root => new[] { Path.Combine(root, @"Google\Chrome\Application\chrome.exe"), Path.Combine(root, @"Microsoft\Edge\Application\msedge.exe") })
			.ToArray();
	}
	else if (OperatingSystem.IsMacOS())
		candidates = ["/Applications/Google Chrome.app/Contents/MacOS/Google Chrome", "/Applications/Chromium.app/Contents/MacOS/Chromium", "/Applications/Microsoft Edge.app/Contents/MacOS/Microsoft Edge"];
	else
		candidates = (Environment.GetEnvironmentVariable("PATH") ?? "").Split(':', StringSplitOptions.RemoveEmptyEntries)
			.SelectMany(directory => new[] { "google-chrome", "google-chrome-stable", "chromium", "chromium-browser", "microsoft-edge" }.Select(name => Path.Combine(directory, name)))
			.ToArray();
	return candidates.FirstOrDefault(File.Exists);
}

static string ScriptDirectory([CallerFilePath] string path = "") => Path.GetDirectoryName(path)!;
