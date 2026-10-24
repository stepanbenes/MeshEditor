using System.Text.Json;
using Microsoft.JSInterop;

namespace MeshEditor.Web.Services;

/// <summary>
/// The signed-in user's remote solutions. Everything runs in the background worker (wwwroot/worker), which holds the layer code:
/// this service signs in, configures the worker, sends it calls and tracks long operations (jobs) with their progress.
/// </summary>
public sealed class RemoteStorage(IJSRuntime js, AppSettings settings) : IAsyncDisposable
{
	private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
	private IJSObjectReference? module;
	private DotNetObjectReference<RemoteStorage>? self;
	private Task<UserInfo?>? initialization;
	private readonly List<Job> jobs = new();

	public UserInfo? User { get; private set; }
	public bool IsDevelopment => settings.DevelopmentStorage.Enabled;
	public bool IsConfigured => IsDevelopment || settings.IsAzureConfigured;
	public string StorageName => IsDevelopment ? "the development storage (Azurite)" : new Uri(settings.AzureBlobStorage.AccountUrl).Host;
	public IReadOnlyList<Job> Jobs => jobs;

	/// <summary>
	/// Raised when a job starts, reports progress or ends.
	/// </summary>
	public event Action? JobsChanged;

	/// <summary>
	/// Signed-in user (or the development user), null when not signed in. Configures the worker once.
	/// </summary>
	public Task<UserInfo?> InitializeAsync()
	{
		if (initialization is { IsFaulted: true })
			initialization = null; // e.g. the storage was not reachable: try again
		return initialization ??= initializeAsync();
	}

	private async Task<UserInfo?> initializeAsync()
	{
		if (!IsConfigured)
			return null;
		var m = await getModuleAsync();
		UserInfo? user;
		if (IsDevelopment)
		{
			user = new UserInfo(settings.DevelopmentStorage.UserId, settings.DevelopmentStorage.UserName);
		}
		else
		{
			var azure = settings.AzureBlobStorage;
			string json = await m.InvokeAsync<string>("initAuth", azure.ClientId, azure.TenantId);
			user = JsonSerializer.Deserialize<UserInfo?>(json, JsonOptions);
			if (user == null)
				return null;
			await m.InvokeVoidAsync("setUseAuth", true);
		}
		var containers = settings.AzureBlobStorage;
		await CallAsync<UserInfo>("configure", new
		{
			accountUrl = IsDevelopment ? null : containers.AccountUrl,
			connectionString = IsDevelopment ? settings.DevelopmentStorage.ConnectionString : null,
			solutionsContainer = containers.SolutionsBlobContainerName,
			layersContainer = containers.LayersBlobContainerName,
			resultsContainer = containers.ResultsBlobContainerName,
			userId = user.Id,
			userName = user.Name
		});
		// signed in only when the worker is ready for the user
		User = user;
		return User;
	}

	public async Task SignInAsync() => await (await getModuleAsync()).InvokeVoidAsync("signIn");

	public async Task SignOutAsync() => await (await getModuleAsync()).InvokeVoidAsync("signOut");

	/// <summary>
	/// Calls a worker method. Throws <see cref="RemoteStorageException"/> with a message for the user when it fails.
	/// </summary>
	public async Task<T> CallAsync<T>(string method, object? args = null, string? jobId = null, string[]? fileInputIds = null)
	{
		var m = await getModuleAsync();
		string json = await m.InvokeAsync<string>("callJson", method, JsonSerializer.Serialize(args ?? new { }, JsonOptions), jobId ?? "", fileInputIds ?? Array.Empty<string>());
		using var document = JsonDocument.Parse(json);
		if (document.RootElement.ValueKind == JsonValueKind.Object && document.RootElement.TryGetProperty("error", out var error))
		{
			bool cancelled = document.RootElement.TryGetProperty("cancelled", out var c) && c.ValueKind == JsonValueKind.True;
			throw new RemoteStorageException(error.GetString() ?? "The operation failed.", cancelled);
		}
		return document.RootElement.Deserialize<T>(JsonOptions)!;
	}

	/// <summary>
	/// Runs a long operation as a job: it is listed with its progress until it ends. Returns null when it failed or was cancelled
	/// (the job keeps the message).
	/// </summary>
	public async Task<T?> RunJobAsync<T>(string title, string method, object? args = null, string[]? fileInputIds = null) where T : class
	{
		var job = new Job(Guid.NewGuid().ToString("N"), title);
		jobs.Insert(0, job);
		JobsChanged?.Invoke();
		try
		{
			var result = await CallAsync<T>(method, args, job.Id, fileInputIds);
			job.Finish("Done.", failed: false);
			return result;
		}
		catch (RemoteStorageException ex)
		{
			job.Finish(ex.Cancelled ? "Cancelled." : ex.Message, failed: !ex.Cancelled);
			return null;
		}
		finally
		{
			JobsChanged?.Invoke();
		}
	}

	public async Task CancelAsync(Job job)
	{
		job.Message = "Cancelling…";
		JobsChanged?.Invoke();
		await (await getModuleAsync()).InvokeVoidAsync("cancel", job.Id);
	}

	public void DismissJob(Job job)
	{
		jobs.Remove(job);
		JobsChanged?.Invoke();
	}

	public async Task<PickedFile[]> GetPickedFilesAsync(string inputId) =>
		await (await getModuleAsync()).InvokeAsync<PickedFile[]>("pickedFiles", inputId);

	public async Task ClearPickedFilesAsync(string inputId) =>
		await (await getModuleAsync()).InvokeVoidAsync("clearFiles", inputId);

	[JSInvokable]
	public void OnProgress(string jobId, string message, int percent)
	{
		var job = jobs.FirstOrDefault(j => j.Id == jobId);
		if (job == null || !job.Running)
			return;
		job.Message = message;
		job.Percent = percent;
		JobsChanged?.Invoke();
	}

	private async Task<IJSObjectReference> getModuleAsync()
	{
		if (module == null)
		{
			module = await js.InvokeAsync<IJSObjectReference>("import", "./js/app.js");
			self = DotNetObjectReference.Create(this);
			await module.InvokeVoidAsync("setProgressTarget", self);
		}
		return module;
	}

	public async ValueTask DisposeAsync()
	{
		if (module != null)
			await module.DisposeAsync();
		self?.Dispose();
	}
}

/// <summary>
/// Long operation in the worker: import, upload, layer generation, deletion.
/// </summary>
public sealed class Job(string id, string title)
{
	public string Id { get; } = id;
	public string Title { get; } = title;
	public string Message { get; set; } = "Starting…";
	public int Percent { get; set; } = -1;
	public bool Running { get; private set; } = true;
	public bool Failed { get; private set; }

	public void Finish(string message, bool failed)
	{
		Running = false;
		Failed = failed;
		Message = message;
		Percent = failed ? Percent : 100;
	}
}
