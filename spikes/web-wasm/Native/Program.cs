using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using MeshEditor.LayerManager;
using MeshEditor.LayerManager.Compression;
using MeshEditor.LayerManager.Import;

if (args[0] == "seed")
{
	var svc = new BlobServiceClient("UseDevelopmentStorage=true");
	var props = (await svc.GetPropertiesAsync()).Value; props.Cors = new List<BlobCorsRule> { new BlobCorsRule { AllowedOrigins = "http://localhost:8765", AllowedMethods = "GET,PUT,DELETE,HEAD,OPTIONS", AllowedHeaders = "*", ExposedHeaders = "*", MaxAgeInSeconds = 3600 } }; await svc.SetPropertiesAsync(props);
	var c = svc.GetBlobContainerClient("fea-layers"); await c.CreateIfNotExistsAsync();
	var imp = new MemoryStorage();
	imp.Records["grid.msh"] = File.ReadAllBytes(args[1] + ".msh");
	imp.Records["grid.res"] = File.ReadAllBytes(args[1] + ".res");
	var layers = new MemoryStorage();
	var gen = new LayerGenerator(layers, layers, compressionService: CompressionServiceFactory.Create(Array.Empty<string>(), null));
	var s = gen.GenerateMasterLayer("master", new[] { AnalysisResultImportServiceFactory.Create(imp, new AnalysisResult { MeshRecordNames = new[] { "grid.msh" }, DataRecordNames = new[] { "grid.res" } }, "Nearest") }, Enumerable.Empty<decimal>());
	foreach (var (k, v) in layers.Records) await c.GetBlobClient("user1/folder/" + k).UploadAsync(BinaryData.FromBytes(v), overwrite: true);
	var sas = c.GenerateSasUri(BlobContainerSasPermissions.All, DateTimeOffset.UtcNow.AddHours(4));
	Console.WriteLine(s.Id); Console.WriteLine(sas);
}
else
{
	var c = new BlobContainerClient("UseDevelopmentStorage=true", "fea-layers");
	await foreach (var b in c.GetBlobsAsync(BlobTraits.None, BlobStates.None, "user1/folder/" + args[1], default)) Console.WriteLine($"{b.Name} {b.Properties.ContentLength}");
}
