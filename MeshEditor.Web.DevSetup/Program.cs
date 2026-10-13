using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

// Usage: dotnet run --project MeshEditor.Web.DevSetup [origin ...] [--connection-string <value>]
// Creates the containers of the remote solutions and allows the web app's origins (CORS) in the development storage.
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

var service = new BlobServiceClient(connectionString);
var properties = (await service.GetPropertiesAsync()).Value;
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
