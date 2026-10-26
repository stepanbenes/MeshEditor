#!/usr/bin/env dotnet
#:package Azure.Storage.Blobs@12.30.1
#:package Azure.Identity@1.21.0

// Publishes packages to the update service (the public blob container mesheditor-update of the storage account feastorage),
// where the app's update check (MeshEditor.Avalonia/UpdateChecker.cs) finds them: uploads each package into <os>/x64/ and puts
// "<version> <file name>" as the first line of releases.txt there. The packages are the output of
// linux-deployment/mesheditor/build (mesheditor_<version>_amd64.deb) and windows-deployment/build.ps1
// (MeshEditor-<version>-win-x64.zip). --user-guide also uploads the user guide (Help -> User guide).
//
// Signs in with the Azure CLI (az login); the account needs the Storage Blob Data Contributor role
// on the storage account. Or pass a connection string with a key or SAS, or UseDevelopmentStorage=true for Azurite.
//
//   dotnet run scripts/publish-update.cs <package> [<package> ...] [--user-guide <pdf>] [--connection-string <value>] [--dry-run]

using System.Text;
using System.Text.RegularExpressions;
using Azure;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

const string AccountUrl = "https://feastorage.blob.core.windows.net";
const string ContainerName = "mesheditor-update";
const char ByteOrderMark = (char)0xFEFF;

var packages = new List<string>();
string userGuide = null;
string connectionString = null;
bool dryRun = false;
for (int i = 0; i < args.Length; i++)
{
	if (args[i] == "--connection-string" && i + 1 < args.Length)
		connectionString = args[++i];
	else if (args[i] == "--user-guide" && i + 1 < args.Length)
		userGuide = args[++i];
	else if (args[i] == "--dry-run")
		dryRun = true;
	else
		packages.Add(args[i]);
}
if (packages.Count == 0 && userGuide == null)
{
	Console.Error.WriteLine("Usage: dotnet run scripts/publish-update.cs <package> [<package> ...] [--user-guide <pdf>] [--connection-string <value>] [--dry-run]");
	return 1;
}

// the folder, version and content type of each package come from its file name
var releases = new List<(string Path, string Folder, Version Version, string ContentType)>();
foreach (string package in packages)
{
	if (!File.Exists(package))
	{
		Console.Error.WriteLine($"{package} does not exist.");
		return 1;
	}
	string name = Path.GetFileName(package);
	Match deb = Regex.Match(name, @"^mesheditor_(\d+\.\d+\.\d+)_amd64\.deb$");
	Match zip = Regex.Match(name, @"^MeshEditor-(\d+\.\d+\.\d+)-win-x64\.zip$");
	if (deb.Success)
		releases.Add((package, "linux/x64", Version.Parse(deb.Groups[1].Value), "application/vnd.debian.binary-package"));
	else if (zip.Success)
		releases.Add((package, "windows/x64", Version.Parse(zip.Groups[1].Value), "application/zip"));
	else
	{
		Console.Error.WriteLine($"{name} is not a package: expected mesheditor_<version>_amd64.deb or MeshEditor-<version>-win-x64.zip.");
		return 1;
	}
}
if (userGuide != null && !File.Exists(userGuide))
{
	Console.Error.WriteLine($"{userGuide} does not exist.");
	return 1;
}

BlobContainerClient container = connectionString != null
	? new BlobContainerClient(connectionString, ContainerName)
	: new BlobContainerClient(new Uri($"{AccountUrl}/{ContainerName}"), new AzureCliCredential()); // not DefaultAzureCredential: it fails on the managed identity before it gets to the CLI

try
{
	// check every releases.txt before uploading anything: a version must be newer than the latest release
	var newReleasesFiles = new Dictionary<string, string>();
	foreach (var group in releases.GroupBy(release => release.Folder))
	{
		if (group.Count() > 1)
		{
			Console.Error.WriteLine($"More than one package for {group.Key}.");
			return 1;
		}
		var release = group.Single();
		// the releases.txt files published before this script start with a byte order mark
		string current = (await readText(container.GetBlobClient($"{release.Folder}/releases.txt"))).TrimStart(ByteOrderMark);
		string latestLine = current.Split('\r', '\n').FirstOrDefault(line => line.Trim().Length > 0);
		if (latestLine != null && Version.TryParse(latestLine.Split(' ', '\t')[0], out Version latest) && release.Version <= latest)
		{
			Console.Error.WriteLine($"{Path.GetFileName(release.Path)}: version {release.Version} is not newer than the latest release in {release.Folder} ({latest}).");
			return 1;
		}
		newReleasesFiles[release.Folder] = $"{release.Version} {Path.GetFileName(release.Path)}\n" + current;
	}

	foreach (var release in releases)
	{
		await upload(container.GetBlobClient($"{release.Folder}/{Path.GetFileName(release.Path)}"), release.Path, release.ContentType, cacheControl: null);
	}
	// releases.txt last, so the update check never points to a package that is not there yet
	foreach ((string folder, string content) in newReleasesFiles)
	{
		string releasesFile = Path.GetTempFileName();
		await File.WriteAllTextAsync(releasesFile, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
		await upload(container.GetBlobClient($"{folder}/releases.txt"), releasesFile, "text/plain; charset=utf-8", cacheControl: "no-cache");
		File.Delete(releasesFile);
		Console.WriteLine($"{folder}/releases.txt:");
		Console.Write(content);
	}
	if (userGuide != null)
	{
		await upload(container.GetBlobClient("user-guide.pdf"), userGuide, "application/pdf", cacheControl: "no-cache");
	}
}
catch (RequestFailedException ex) when (ex.Status == 0)
{
	Console.Error.WriteLine($"The storage at {container.Uri} does not answer: {ex.Message}");
	return 1;
}
catch (RequestFailedException ex) when (ex.Status == 403)
{
	Console.Error.WriteLine($"Access to {container.Uri} denied. The signed-in account needs the Storage Blob Data Contributor role on the storage account.");
	return 1;
}
catch (AuthenticationFailedException ex)
{
	Console.Error.WriteLine($"Can't sign in to Azure (run az login first): {ex.Message}");
	return 1;
}
if (dryRun)
	Console.WriteLine("Dry run: nothing was uploaded.");
return 0;

async Task<string> readText(BlobClient blob)
{
	try
	{
		return (await blob.DownloadContentAsync()).Value.Content.ToString();
	}
	catch (RequestFailedException ex) when (ex.Status == 404)
	{
		return "";
	}
}

async Task upload(BlobClient blob, string path, string contentType, string cacheControl)
{
	Console.WriteLine($"{(dryRun ? "would upload" : "uploading")} {path} -> {blob.Uri}");
	if (dryRun)
		return;
	await blob.UploadAsync(path, new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = contentType, CacheControl = cacheControl } });
}
