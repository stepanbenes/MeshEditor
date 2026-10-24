#!/usr/bin/env dotnet
#:package Azure.Storage.Blobs@12.30.1

// Prepares Azurite (the Azure Storage emulator) for the web app: creates the containers of the remote solutions and allows the
// web app's origins (CORS) in the development storage. See MeshEditor.Web/README.md.
//
//   dotnet run scripts/setup-azurite.cs [origin ...] [--connection-string <value>]

using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

var origins = new List<string>();
string connectionString = "UseDevelopmentStorage=true";
for (int i = 0; i < args.Length; i++)
{
	if (args[i] == "--connection-string" && i + 1 < args.Length)
		connectionString = args[++i];
	else
		origins.Add(args[i].TrimEnd('/'));
}
if (origins.Count == 0)
	origins.Add("http://localhost:5280");

// the emulator runs on this computer: when it doesn't answer, the retries would only delay the message by half a minute
var options = new BlobClientOptions();
options.Retry.MaxRetries = 0;
var service = new BlobServiceClient(connectionString, options);
BlobServiceProperties properties;
try
{
	properties = (await service.GetPropertiesAsync()).Value;
}
catch (RequestFailedException ex) when (ex.Status == 0)
{
	Console.Error.WriteLine($"Azurite is not running at {service.Uri}. Start it first, see MeshEditor.Web/README.md.");
	return 1;
}
catch (RequestFailedException ex) when (ex.ErrorCode == "InvalidHeaderValue" && ex.Message.Contains("API version", StringComparison.Ordinal))
{
	Console.Error.WriteLine("Azurite doesn't know the storage API version of this app. Start it with --skipApiVersionCheck, see MeshEditor.Web/README.md.");
	return 1;
}
properties.Cors = new List<BlobCorsRule>
{
	new BlobCorsRule
	{
		AllowedOrigins = string.Join(",", origins),
		AllowedMethods = "GET,HEAD,PUT,POST,DELETE,OPTIONS,MERGE",
		AllowedHeaders = "*",
		ExposedHeaders = "*",
		MaxAgeInSeconds = 3600
	}
};
await service.SetPropertiesAsync(properties);
Console.WriteLine($"CORS allows {string.Join(", ", origins)}");

foreach (string container in new[] { "fea-solutions", "fea-layers", "fea-results" })
{
	await service.GetBlobContainerClient(container).CreateIfNotExistsAsync();
	Console.WriteLine($"Container {container} is ready");
}
return 0;
