using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using MeshEditor.Common.Extensions;
using MeshEditor.LayerManager.Import;
using MeshEditor.LayerManager.Serialization;
using MeshEditor.SolutionManager.AzureStorage;

namespace MeshEditor.SolutionManager.IO
{
	/// <summary>
	/// Solutions stored in Azure Blob Storage. Every solution is a JSON blob "{id}.solution.json" in the solutions container
	/// (same format as the local solution file), its layers are in the layers container (blobs "{layerId}/...").
	/// Solution locator is the integer solution id. Concurrent modifications are detected using blob ETags.
	/// </summary>
	class AzureBlobSolutionController : ISolutionController
	{
		#region Fields, constructor

		private static readonly string SolutionBlobSuffix = ".solution.json";
		private const int MaxConcurrencyRetries = 10;

		private readonly BlobContainerClient solutionContainer;
		private readonly BlobContainerClient layerContainer;
		private readonly ISerializationService serializer;

		public AzureBlobSolutionController(BlobContainerClient solutionContainer, BlobContainerClient layerContainer)
		{
			this.solutionContainer = solutionContainer ?? throw new ArgumentNullException(nameof(solutionContainer));
			this.layerContainer = layerContainer ?? throw new ArgumentNullException(nameof(layerContainer));
			serializer = new JsonSerializationService();
		}

		#endregion

		#region ISolutionController

		public Solution CreateNew(object solutionLocator /*ignored*/, IEnumerable<AnalysisResult> analysisResults, string projectName = null)
		{
			var results = analysisResults.ToArray();
			projectName = string.IsNullOrWhiteSpace(projectName) ? Path.GetFileNameWithoutExtension(results.First().MeshRecordNames.First()) : projectName;
			var solution = new Solution
			{
				ProjectName = projectName,
				Results = results,
				Layers = new Solution.Layer[0]
			};
			return runSynchronously(() => createAsync(solution, CancellationToken.None));
		}

		public IEnumerable<ISolutionInfo> GetAll() => runSynchronously(() => GetAllAsync(CancellationToken.None));

		public async Task<IEnumerable<ISolutionInfo>> GetAllAsync(CancellationToken cancellationToken)
		{
			if (!await solutionContainer.ExistsAsync(cancellationToken).ConfigureAwait(false))
				return Enumerable.Empty<ISolutionInfo>();

			var solutionIds = new List<int>();
			await foreach (var blob in solutionContainer.GetBlobsAsync(BlobTraits.None, BlobStates.None, prefix: null, cancellationToken).ConfigureAwait(false))
			{
				if (tryParseSolutionId(blob.Name, out int solutionId))
					solutionIds.Add(solutionId);
			}

			var solutions = await Task.WhenAll(solutionIds.Select(async solutionId =>
			{
				var (solution, _) = await downloadAsync<SolutionInfo>(solutionId, cancellationToken).ConfigureAwait(false);
				return solution;
			})).ConfigureAwait(false);
			return solutions.Where(solution => solution != null).ToArray();
		}

		public Solution Get(object solutionLocator) => runSynchronously(() => GetAsync(solutionLocator, CancellationToken.None));

		public async Task<Solution> GetAsync(object solutionLocator, CancellationToken cancellationToken)
		{
			int solutionId = getSolutionId(solutionLocator);
			var (solution, _) = await downloadAsync<Solution>(solutionId, cancellationToken).ConfigureAwait(false);
			return solution ?? throw new FileNotFoundException($"Remote solution {solutionId} not found.");
		}

		public void Delete(object solutionLocator) => runSynchronously(() => DeleteAsync(solutionLocator, CancellationToken.None));

		public async Task DeleteAsync(object solutionLocator, CancellationToken cancellationToken)
		{
			Solution solution = await GetAsync(solutionLocator, cancellationToken).ConfigureAwait(false);
			foreach (var rootLayer in solution.Layers.EmptyIfNull())
			{
				await deleteAllLayerBlobsOfLayerTreeAsync(rootLayer, cancellationToken).ConfigureAwait(false);
			}
			await getSolutionBlob(solution.Id).DeleteIfExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false); // delete solution blob itself
		}

		public Solution AddLayer(object solutionLocator, Solution.Layer parentLayer, Solution.Layer newLayer)
		{
			int solutionId = getSolutionId(solutionLocator);
			return runSynchronously(() => updateAsync(solutionId, solution => Solution.CreateNewByAddingLayer(solution, newLayer, parentLayer?.Id), CancellationToken.None));
		}

		public Solution DeleteLayer(object solutionLocator, Solution.Layer layerToDelete) => runSynchronously(() => DeleteLayerAsync(solutionLocator, layerToDelete, CancellationToken.None));

		public async Task<Solution> DeleteLayerAsync(object solutionLocator, Solution.Layer layerToDelete /*root layer*/, CancellationToken cancellationToken)
		{
			int solutionId = getSolutionId(solutionLocator);
			await deleteAllLayerBlobsOfLayerTreeAsync(layerToDelete, cancellationToken).ConfigureAwait(false);
			return await updateAsync(solutionId, solution => Solution.CreateNewByDeletingLayer(solution, layerToDelete.Id), cancellationToken).ConfigureAwait(false);
		}

		#endregion

		#region Upload

		/// <summary>
		/// Copies the local solution (solution file and all layer files) to the blob storage as a new remote solution.
		/// The solution blob is created last so that a listed remote solution is always complete.
		/// </summary>
		public async Task<Solution> UploadAsync(Solution localSolution, string localSolutionDirectory, IProgress<string> progress, CancellationToken cancellationToken)
		{
			await layerContainer.CreateIfNotExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

			foreach (var layer in localSolution.Layers.EmptyIfNull().SelectMany(Solution.TraverseLayerTreePostOrder))
			{
				string layerDirectory = Path.Combine(localSolutionDirectory, layer.Id.ToString());
				if (!Directory.Exists(layerDirectory))
					throw new DirectoryNotFoundException($"Files of layer '{layer.Name}' ({layer.Id}) not found in '{localSolutionDirectory}'.");

				foreach (string file in Directory.EnumerateFiles(layerDirectory, "*", SearchOption.AllDirectories))
				{
					string blobName = AzureBlobStorageService.ToBlobName(Path.GetRelativePath(localSolutionDirectory, file));
					progress?.Report($"Uploading {blobName}");
					await layerContainer.GetBlobClient(blobName).UploadAsync(file, overwrite: true, cancellationToken).ConfigureAwait(false);
				}
			}

			var remoteSolution = new Solution
			{
				ProjectName = localSolution.ProjectName,
				Results = localSolution.Results,
				Layers = localSolution.Layers
			};
			return await createAsync(remoteSolution, cancellationToken).ConfigureAwait(false);
		}

		#endregion

		#region Private methods

		private async Task<Solution> createAsync(Solution solution, CancellationToken cancellationToken)
		{
			await solutionContainer.CreateIfNotExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

			for (int attempt = 0; ; attempt++)
			{
				var existingIds = new List<int>();
				await foreach (var blob in solutionContainer.GetBlobsAsync(BlobTraits.None, BlobStates.None, prefix: null, cancellationToken).ConfigureAwait(false))
				{
					if (tryParseSolutionId(blob.Name, out int existingId))
						existingIds.Add(existingId);
				}
				solution.Id = existingIds.Count > 0 ? existingIds.Max() + 1 : 1;

				try
				{
					// fails if another client has just created a solution with the same id
					await uploadAsync(solution, new BlobRequestConditions { IfNoneMatch = ETag.All }, cancellationToken).ConfigureAwait(false);
					solution.Location = getLocation(solution.Id);
					return solution;
				}
				catch (RequestFailedException ex) when (isConcurrencyConflict(ex) && attempt < MaxConcurrencyRetries)
				{ }
			}
		}

		private async Task<Solution> updateAsync(int solutionId, Func<Solution, Solution> update, CancellationToken cancellationToken)
		{
			for (int attempt = 0; ; attempt++)
			{
				var (solution, eTag) = await downloadAsync<Solution>(solutionId, cancellationToken).ConfigureAwait(false);
				if (solution == null)
					throw new FileNotFoundException($"Remote solution {solutionId} not found.");

				Solution updatedSolution = update(solution);
				try
				{
					// fails if the solution was modified since it was downloaded
					await uploadAsync(updatedSolution, new BlobRequestConditions { IfMatch = eTag }, cancellationToken).ConfigureAwait(false);
					updatedSolution.Location = getLocation(solutionId);
					return updatedSolution;
				}
				catch (RequestFailedException ex) when (isConcurrencyConflict(ex) && attempt < MaxConcurrencyRetries)
				{ }
			}
		}

		private async Task uploadAsync(Solution solution, BlobRequestConditions conditions, CancellationToken cancellationToken)
		{
			var stream = new MemoryStream();
			var serialized = new Solution
			{
				Id = solution.Id,
				ProjectName = solution.ProjectName,
				Location = null, // location is not stored, it is the blob address
				Results = solution.Results,
				Layers = solution.Layers
			};
			await serializer.SerializeAsync(serialized, stream, cancellationToken).ConfigureAwait(false);
			var options = new BlobUploadOptions
			{
				Conditions = conditions,
				HttpHeaders = new BlobHttpHeaders { ContentType = "application/json" }
			};
			await getSolutionBlob(solution.Id).UploadAsync(BinaryData.FromBytes(stream.ToArray()), options, cancellationToken).ConfigureAwait(false);
		}

		private async Task<(T solution, ETag eTag)> downloadAsync<T>(int solutionId, CancellationToken cancellationToken) where T : SolutionInfo
		{
			try
			{
				var response = await getSolutionBlob(solutionId).DownloadContentAsync(cancellationToken).ConfigureAwait(false);
				T solution = serializer.Deserialize<T>(response.Value.Content.ToStream());
				solution.Id = solutionId;
				solution.Location = getLocation(solutionId);
				return (solution, response.Value.Details.ETag);
			}
			catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.NotFound)
			{
				return (null, default);
			}
		}

		private async Task deleteAllLayerBlobsOfLayerTreeAsync(Solution.Layer rootLayer, CancellationToken cancellationToken)
		{
			if (!await layerContainer.ExistsAsync(cancellationToken).ConfigureAwait(false))
				return;

			foreach (var layer in Solution.TraverseLayerTreePostOrder(rootLayer))
			{
				string prefix = layer.Id + "/";
				var blobNames = new List<string>();
				await foreach (var blob in layerContainer.GetBlobsAsync(BlobTraits.None, BlobStates.None, prefix, cancellationToken).ConfigureAwait(false))
				{
					blobNames.Add(blob.Name);
				}
				foreach (string blobName in blobNames)
				{
					await layerContainer.GetBlobClient(blobName).DeleteIfExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
				}
			}
		}

		private BlobClient getSolutionBlob(int solutionId) => solutionContainer.GetBlobClient(getSolutionBlobName(solutionId));

		/// <summary>
		/// Address of the solution blob without the shared access signature.
		/// </summary>
		private string getLocation(int solutionId)
		{
			var uriBuilder = new UriBuilder(getSolutionBlob(solutionId).Uri) { Query = string.Empty };
			return uriBuilder.Uri.ToString();
		}

		private static string getSolutionBlobName(int solutionId) => solutionId.ToString(CultureInfo.InvariantCulture) + SolutionBlobSuffix;

		private static bool tryParseSolutionId(string blobName, out int solutionId)
		{
			solutionId = 0;
			return blobName.EndsWith(SolutionBlobSuffix, StringComparison.OrdinalIgnoreCase) &&
				int.TryParse(blobName.Substring(0, blobName.Length - SolutionBlobSuffix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out solutionId);
		}

		private static int getSolutionId(object solutionLocator)
		{
			if (!(solutionLocator is int solutionId))
				throw new ArgumentException("Solution id is not specified", nameof(solutionLocator));
			return solutionId;
		}

		private static bool isConcurrencyConflict(RequestFailedException ex) =>
			ex.Status == (int)HttpStatusCode.Conflict || ex.Status == (int)HttpStatusCode.PreconditionFailed;

		/// <summary>
		/// Synchronous variants of the operations (command line). Runs on the thread pool to not deadlock on a UI synchronization context.
		/// </summary>
		private static T runSynchronously<T>(Func<Task<T>> operation) => Task.Run(operation).GetAwaiter().GetResult();

		private static void runSynchronously(Func<Task> operation) => Task.Run(operation).GetAwaiter().GetResult();

		#endregion
	}
}
