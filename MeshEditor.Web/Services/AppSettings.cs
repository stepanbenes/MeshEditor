namespace MeshEditor.Web.Services;

/// <summary>
/// wwwroot/appsettings.json: the storage account and the app registration (the same values as the AzureBlobStorage section
/// of the desktop app's config.json), or a development storage (Azurite) used without signing in.
/// </summary>
public sealed class AppSettings
{
	public AzureBlobStorageSettings AzureBlobStorage { get; set; } = new();
	public DevelopmentStorageSettings DevelopmentStorage { get; set; } = new();

	public bool IsAzureConfigured =>
		!string.IsNullOrWhiteSpace(AzureBlobStorage.AccountUrl) && !string.IsNullOrWhiteSpace(AzureBlobStorage.TenantId) && !string.IsNullOrWhiteSpace(AzureBlobStorage.ClientId);
}

public sealed class AzureBlobStorageSettings
{
	public string AccountUrl { get; set; } = "";
	public string TenantId { get; set; } = "";
	public string ClientId { get; set; } = "";
	public string SolutionsBlobContainerName { get; set; } = "fea-solutions";
	public string LayersBlobContainerName { get; set; } = "fea-layers";
	public string ResultsBlobContainerName { get; set; } = "fea-results";
}

public sealed class DevelopmentStorageSettings
{
	public bool Enabled { get; set; }
	public string ConnectionString { get; set; } = "UseDevelopmentStorage=true";
	public string UserId { get; set; } = "local-user";
	public string UserName { get; set; } = "local user";
}
