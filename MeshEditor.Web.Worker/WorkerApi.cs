using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MeshEditor.SolutionManager;

namespace MeshEditor.Web.Worker
{
	/// <summary>
	/// Entry points called by worker.js. Requests and results are JSON; large arrays (3D buffers) are taken separately
	/// by <see cref="TakeBuffer"/> after a call, so that worker.js can transfer them to the page without copying.
	/// Errors are returned as {"error": "..."}.
	/// </summary>
	[SupportedOSPlatform("browser")]
	static partial class WorkerApi
	{
		private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
		private static readonly WebAppTokenCredential Credential = new WebAppTokenCredential();
		private static readonly Dictionary<string, CancellationTokenSource> Jobs = new Dictionary<string, CancellationTokenSource>();
		private static readonly Dictionary<string, List<StagedFile>> StagedFiles = new Dictionary<string, List<StagedFile>>();
		private static readonly Dictionary<string, byte[]> Buffers = new Dictionary<string, byte[]>();
		private static Operations operations;

		[JSImport("progress", "worker")]
		private static partial void ReportProgress(string jobId, string message, int percent);

		[JSExport]
		public static void SetToken(string accessToken, double expiresOnUnixMs)
		{
			Credential.Set(accessToken, DateTimeOffset.FromUnixTimeMilliseconds((long)expiresOnUnixMs));
		}

		[JSExport]
		public static void StageFile(string jobId, string name, string relativePath, byte[] content)
		{
			if (!StagedFiles.TryGetValue(jobId, out var files))
				StagedFiles[jobId] = files = new List<StagedFile>();
			files.Add(new StagedFile(name, string.IsNullOrEmpty(relativePath) ? name : relativePath, content));
		}

		[JSExport]
		public static void Cancel(string jobId)
		{
			if (Jobs.TryGetValue(jobId, out var cancellation))
				cancellation.Cancel();
		}

		[JSExport]
		public static byte[] TakeBuffer(string name)
		{
			Buffers.Remove(name, out var buffer);
			return buffer ?? Array.Empty<byte>();
		}

		[JSExport]
		public static async Task<string> Call(string method, string jobId, string argumentsJson)
		{
			var cancellation = new CancellationTokenSource();
			if (!string.IsNullOrEmpty(jobId))
				Jobs[jobId] = cancellation;
			try
			{
				var args = JsonDocument.Parse(string.IsNullOrEmpty(argumentsJson) ? "{}" : argumentsJson).RootElement;
				object result = await dispatchAsync(method, jobId, args, cancellation.Token);
				return JsonSerializer.Serialize(result, JsonOptions);
			}
			catch (OperationCanceledException)
			{
				return JsonSerializer.Serialize(new { error = "Cancelled.", cancelled = true }, JsonOptions);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine(ex);
				return JsonSerializer.Serialize(new { error = describe(ex) }, JsonOptions);
			}
			finally
			{
				if (!string.IsNullOrEmpty(jobId))
				{
					Jobs.Remove(jobId);
					StagedFiles.Remove(jobId);
				}
				cancellation.Dispose();
			}
		}

		private static async Task<object> dispatchAsync(string method, string jobId, JsonElement args, CancellationToken cancellationToken)
		{
			if (method == "configure")
			{
				var settings = args.Deserialize<StorageSettings>(JsonOptions);
				var session = new RemoteSession(settings, Credential);
				await session.EnsureContainersAsync(cancellationToken);
				operations = new Operations(session);
				return new { userId = session.User.Id, userName = session.User.Name };
			}

			var ops = operations ?? throw new InvalidOperationException("The storage is not configured.");
			var progress = new JobProgress(jobId);
			switch (method)
			{
				case "listSolutions":
					return await ops.ListSolutionsAsync(cancellationToken);
				case "getSolution":
					return await ops.GetSolutionAsync(args.GetProperty("solutionId").GetInt32(), cancellationToken);
				case "deleteSolution":
					return await ops.DeleteSolutionAsync(args.GetProperty("solutionId").GetInt32(), progress, cancellationToken);
				case "getLayer":
					return await ops.GetLayerAsync(args.GetProperty("solutionId").GetInt32(), args.GetProperty("layerId").GetGuid(), cancellationToken);
				case "createLayer":
					return await ops.CreateLayerAsync(args.Deserialize<CreateLayerRequest>(JsonOptions), progress, cancellationToken);
				case "deleteLayer":
					return await ops.DeleteLayerAsync(args.GetProperty("solutionId").GetInt32(), args.GetProperty("layerId").GetGuid(), progress, cancellationToken);
				case "importResults":
					return await ops.ImportResultsAsync(args.Deserialize<ImportRequest>(JsonOptions), stagedFilesOf(jobId), progress, cancellationToken);
				case "uploadLocalSolution":
					return await ops.UploadLocalSolutionAsync(stagedFilesOf(jobId), progress, cancellationToken);
				case "buildView":
					return withBuffers(await ops.BuildViewAsync(args.GetProperty("solutionId").GetInt32(), args.GetProperty("layerId").GetGuid(), args.GetProperty("meshIndex").GetInt32(), cancellationToken));
				case "getScalars":
					return withBuffers(await ops.GetScalarsAsync(args.GetProperty("solutionId").GetInt32(), args.GetProperty("layerId").GetGuid(),
						args.GetProperty("field").GetString(), args.GetProperty("component").GetString(), args.GetProperty("timeStep").GetDecimal(), cancellationToken));
				case "getVectors":
					return withBuffers(await ops.GetVectorsAsync(args.GetProperty("solutionId").GetInt32(), args.GetProperty("layerId").GetGuid(),
						args.GetProperty("field").GetString(), args.GetProperty("timeStep").GetDecimal(), cancellationToken));
				default:
					throw new ArgumentException($"Unknown method '{method}'.");
			}
		}

		private static IReadOnlyList<StagedFile> stagedFilesOf(string jobId) =>
			jobId != null && StagedFiles.TryGetValue(jobId, out var files) ? files : (IReadOnlyList<StagedFile>)Array.Empty<StagedFile>();

		private static object withBuffers((object meta, Dictionary<string, byte[]> buffers) result)
		{
			Buffers.Clear(); // buffers not taken by the previous call
			foreach (var (name, buffer) in result.buffers)
				Buffers[name] = buffer;
			return new { meta = result.meta, buffers = result.buffers.Keys };
		}

		private static string describe(Exception ex)
		{
			if (ex is Azure.RequestFailedException requestFailed)
			{
				switch (requestFailed.Status)
				{
					case 401:
					case 403:
						return "The storage refused access. Check that your account has the Storage Blob Data Contributor role on the storage account.";
					case 0:
						return "The storage could not be reached. Check the storage address and that CORS on the storage account allows this site.";
				}
				return $"Storage request failed ({requestFailed.Status} {requestFailed.ErrorCode}).";
			}
			// also the failed retries of a storage request ("Retry failed after 6 tries"): the last one tells why
			if (ex is AggregateException aggregate && aggregate.InnerExceptions.Count > 0
				&& (aggregate.InnerExceptions.Count == 1 || aggregate.InnerExceptions.All(e => e is Azure.RequestFailedException)))
				return describe(aggregate.InnerExceptions[^1]);
			return ex.Message;
		}

		private sealed class JobProgress : IProgress<RemoteOperationProgress>
		{
			private readonly string jobId;

			public JobProgress(string jobId) => this.jobId = jobId;

			public void Report(RemoteOperationProgress value)
			{
				if (!string.IsNullOrEmpty(jobId))
					ReportProgress(jobId, value.Message ?? "", value.PercentDone);
			}
		}
	}
}
