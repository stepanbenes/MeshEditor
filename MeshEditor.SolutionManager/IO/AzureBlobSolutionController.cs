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
	/// Remote solutions of one user stored in Azure Blob Storage. Everything of the user is under the folder named by the user's id:
	/// solution "{owner}/{id}.solution.json" in the solutions container (same format as the local solution file, with the owner recorded),
	/// layers "{owner}/{layerId}/..." in the layers container. Solution locator is the integer solution id (unique per user).
	/// Only the solutions of the owner are visible. Concurrent modifications are detected using blob ETags.
	/// The containers are not created here, they are set up together with the access rights of the users.
	/// </summary>
	class AzureBlobSolutionController : ISolutionController
	{
		#region Fields, constructor

		private static readonly string SolutionBlobSuffix = ".solution.json";
		private const int MaxConcurrencyRetries = 10;

		private readonly BlobContainerClient solutionContainer;
		private readonly BlobContainerClient layerContainer;
		private readonly RemoteUser owner;
		private readonly string ownerFolder;
		private readonly ISerializationService serializer;

		public AzureBlobSolutionController(BlobContainerClient solutionContainer, BlobContainerClient layerContainer, RemoteUser owner)
		{
			this.solutionContainer = solutionContainer ?? throw new ArgumentNullException(nameof(solutionContainer));
			this.layerContainer = layerContainer ?? throw new ArgumentNullException(nameof(layerContainer));
			this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
			if (string.IsNullOrWhiteSpace(owner.Id))
				throw new ArgumentException("Owner id is not specified", nameof(owner));
			ownerFolder = owner.Id + "/";
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
			var solutionIds = await getSolutionIdsAsync(cancellationToken).ConfigureAwait(false);
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
		/// Copies the local solution (solution file and all layer files) to the blob storage as a new remote solution of the owner.
		/// The solution blob is created last so that a listed remote solution is always complete.
		/// </summary>
		public async Task<Solution> UploadAsync(Solution localSolution, string localSolutionDirectory, IProgress<string> progress, CancellationToken cancellationToken)
		{
			foreach (var layer in localSolution.Layers.EmptyIfNull().SelectMany(Solution.TraverseLayerTreePostOrder))
			{
				string layerDirectory = Path.Combine(localSolutionDirectory, layer.Id.ToString());
				if (!Directory.Exists(layerDirectory))
					throw new DirectoryNotFoundException($"Files of layer '{layer.Name}' ({layer.Id}) not found in '{localSolutionDirectory}'.");

				foreach (string file in Directory.EnumerateFiles(layerDirectory, "*", SearchOption.AllDirectories))
				{
					string blobName = AzureBlobStorageService.ToBlobName(owner.Id, Path.GetRelativePath(localSolutionDirectory, file));
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

		private async Task<List<int>> getSolutionIdsAsync(CancellationToken cancellationToken)
		{
			var solutionIds = new List<int>();
			await foreach (var blob in solutionContainer.GetBlobsAsync(BlobTraits.None, BlobStates.None, ownerFolder, cancellationToken).ConfigureAwait(false))
			{
				if (tryParseSolutionId(blob.Name, out int solutionId))
					solutionIds.Add(solutionId);
			}
			return solutionIds;
		}

		private async Task<Solution> createAsync(Solution solution, CancellationToken cancellationToken)
		{
			solution.Owner = owner;
			for (int attempt = 0; ; attempt++)
			{
				var existingIds = await getSolutionIdsAsync(cancellationToken).ConfigureAwait(false);
				solution.Id = existingIds.Count > 0 ? existingIds.Max() + 1 : 1;

				try
				{
					// fails if another client of the same user has just created a solution with the same id
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
				Owner = owner,
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
				if (solution.Owner?.Id != owner.Id)
					return (null, default); // not a solution of this user
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
			foreach (var layer in Solution.TraverseLayerTreePostOrder(rootLayer))
			{
				string prefix = ownerFolder + layer.Id + "/";
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

		private BlobClient getSolutionBlob(int solutionId) => solutionContainer.GetBlobClient(ownerFolder + getSolutionBlobName(solutionId));

		/// <summary>
		/// Address of the solution blob (without a shared access signature, if any).
		/// </summary>
		private string getLocation(int solutionId)
		{
			var uriBuilder = new UriBuilder(getSolutionBlob(solutionId).Uri) { Query = string.Empty };
			return uriBuilder.Uri.ToString();
		}

		private static string getSolutionBlobName(int solutionId) => solutionId.ToString(CultureInfo.InvariantCulture) + SolutionBlobSuffix;

		private bool tryParseSolutionId(string blobName, out int solutionId)
		{
			solutionId = 0;
			if (!blobName.StartsWith(ownerFolder, StringComparison.Ordinal) || !blobName.EndsWith(SolutionBlobSuffix, StringComparison.OrdinalIgnoreCase))
				return false;
			string idPart = blobName.Substring(ownerFolder.Length, blobName.Length - ownerFolder.Length - SolutionBlobSuffix.Length);
			return int.TryParse(idPart, NumberStyles.None, CultureInfo.InvariantCulture, out solutionId);
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
