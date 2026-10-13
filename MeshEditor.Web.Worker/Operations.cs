using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MeshEditor.Common.Extensions;
using MeshEditor.Common.Logging;
using MeshEditor.LayerManager;
using MeshEditor.LayerManager.Compression;
using MeshEditor.LayerManager.Data;
using MeshEditor.LayerManager.Filters;
using MeshEditor.LayerManager.Import;
using MeshEditor.LayerManager.Serialization;
using MeshEditor.SolutionManager;
using MeshEditor.SolutionManager.IO;

namespace MeshEditor.Web.Worker
{
	/// <summary>
	/// What the web app does with the remote solutions: the same steps as the desktop app (SolutionHub), but the layer records are
	/// downloaded before and uploaded after the layer generator runs, because the browser can't read blobs synchronously.
	/// </summary>
	sealed class Operations
	{
		private const string DefaultMasterLayerName = "master";

		private readonly RemoteSession session;
		private readonly Dictionary<(int solutionId, Guid layerId, int meshIndex), MeshView> views = new Dictionary<(int, Guid, int), MeshView>();
		private readonly Dictionary<Guid, SummaryFile> summaries = new Dictionary<Guid, SummaryFile>();
		private Solution openSolution;

		public Operations(RemoteSession session)
		{
			this.session = session ?? throw new ArgumentNullException(nameof(session));
		}

		#region Solutions

		public async Task<object> ListSolutionsAsync(CancellationToken cancellationToken)
		{
			var infos = await session.Controller.GetAllAsync(cancellationToken);
			var solutions = await Task.WhenAll(infos.Select(info => session.Controller.GetAsync(info.Id, cancellationToken)));
			return solutions.OrderByDescending(s => s.Id).Select(s => new
			{
				id = s.Id,
				projectName = s.ProjectName,
				layerCount = s.Layers.EmptyIfNull().SelectMany(Solution.TraverseLayerTreePostOrder).Count(),
				resultFiles = s.Results.EmptyIfNull().SelectMany(r => r.MeshRecordNames.EmptyIfNull().Concat(r.DataRecordNames.EmptyIfNull())).Distinct().Select(Path.GetFileName).ToArray(),
				location = s.Location
			}).ToArray();
		}

		public async Task<object> GetSolutionAsync(int solutionId, CancellationToken cancellationToken)
		{
			var solution = await openAsync(solutionId, refresh: true, cancellationToken);
			return new
			{
				id = solution.Id,
				projectName = solution.ProjectName,
				location = solution.Location,
				layers = solution.Layers.EmptyIfNull().Select(toLayerTree).ToArray()
			};

			static object toLayerTree(Solution.Layer layer) => new
			{
				id = layer.Id,
				name = layer.Name,
				filterType = layer.FilterType,
				children = layer.Children.EmptyIfNull().Select(toLayerTree).ToArray()
			};
		}

		public async Task<object> DeleteSolutionAsync(int solutionId, IProgress<RemoteOperationProgress> progress, CancellationToken cancellationToken)
		{
			await session.Controller.DeleteAsync(solutionId, progress, cancellationToken);
			if (openSolution?.Id == solutionId)
				closeSolution();
			return new { deleted = solutionId };
		}

		#endregion

		#region Layers

		public async Task<object> GetLayerAsync(int solutionId, Guid layerId, CancellationToken cancellationToken)
		{
			var solution = await openAsync(solutionId, refresh: false, cancellationToken);
			var summary = await loadSummaryAsync(solution, layerId, cancellationToken);
			return new
			{
				id = summary.Id,
				name = summary.Name,
				parentId = summary.ParentId,
				filterType = summary.Filter?.Type.ToString(),
				filter = summary.Filter == null ? null : Newtonsoft.Json.JsonConvert.SerializeObject(summary.Filter),
				meshes = summary.Meshes.EmptyIfNull().Select(m => new { index = m.Index, timeSteps = m.TimeSteps.EmptyIfNull() }).ToArray(),
				fields = summary.Fields.EmptyIfNull().Select(f => new
				{
					name = f.Key,
					components = f.Value.Components.Keys.ToArray(),
					timeSteps = f.Value.Components.Values.SelectMany(c => c.TimeSteps.Keys).Distinct().OrderBy(t => t).ToArray()
				}).ToArray(),
				stored = session.GetViewStore(solution).CachedBytes
			};
		}

		/// <summary>
		/// Generates a sub layer of the parent layer (filter, or compression only when the filter type is "Compress") and adds it to the solution.
		/// </summary>
		public async Task<object> CreateLayerAsync(CreateLayerRequest request, IProgress<RemoteOperationProgress> progress, CancellationToken cancellationToken)
		{
			var solution = await session.Controller.GetAsync(request.SolutionId, cancellationToken);
			var store = new BlobRecordStore(session.LayersContainer, session.Controller.GetLayerFolder(solution));

			progress.Report(new RemoteOperationProgress("Reading the parent layer", -1));
			if (!await store.PrefetchAsync(summaryRecord(request.ParentLayerId), cancellationToken))
				throw new FileNotFoundException("The parent layer was not found in the remote storage.");
			var parent = new LayerGenerator(store, null).LoadLayerSummary(request.ParentLayerId);

			// the parent's records, and the records it shares with its own parents (fallback layers)
			var folders = new[] { (Guid?)parent.Id, parent.MeshFallbackLayerId, parent.AttributeFallbackLayerId, parent.DataFallbackLayerId }
				.Where(id => id.HasValue).Select(id => id.Value).Distinct().ToArray();
			for (int i = 0; i < folders.Length; i++)
			{
				string what = folders[i] == parent.Id ? "parent layer" : "parent's base layer";
				await store.PrefetchFolderAsync(folders[i] + "/", (done, total) =>
					progress.Report(new RemoteOperationProgress($"Downloading the {what}, file {done} of {total}", total == 0 ? 100 : done * 100 / total)), cancellationToken);
			}

			progress.Report(new RemoteOperationProgress("Computing the layer in this browser tab", -1));
			await Task.Yield(); // let the progress out before the synchronous computation
			var logger = new ProgressLogger(progress);
			var generator = new LayerGenerator(store, store, compressionService: CompressionServiceFactory.Create(request.Compression.EmptyIfNull(), logger), logger: logger);
			var keyTimeSteps = request.KeyTimeSteps.EmptyIfNull();
			string fieldName = string.IsNullOrWhiteSpace(request.FieldName) ? null : request.FieldName;
			SummaryFile summary;
			if (string.Equals(request.FilterType, "Compress", StringComparison.OrdinalIgnoreCase))
			{
				string name = string.IsNullOrWhiteSpace(request.Name) ? $"compressed ({string.Join(" ", request.Compression.EmptyIfNull())})" : request.Name;
				summary = generator.CompressLayer(parent.Id, keyTimeSteps, name, fieldName, cancellationToken);
			}
			else
			{
				if (!Enum.TryParse(request.FilterType, ignoreCase: true, out FilterType filterType))
					throw new ArgumentException($"Unknown filter type ({request.FilterType})");
				var filter = FilterFactory.Create(filterType, request.FilterParameters.EmptyIfNull());
				summary = generator.GenerateFilterLayer(parent.Id, filter, request.Name, keyTimeSteps, fieldName, cancellationToken);
			}

			var newLayer = await uploadLayerAsync(store, summary, request.SolutionId, parent.Id, progress, cancellationToken);
			if (openSolution?.Id == request.SolutionId)
				openSolution = null; // re-read with the new layer
			return new { id = newLayer.Id, name = newLayer.Name };
		}

		public async Task<object> DeleteLayerAsync(int solutionId, Guid layerId, IProgress<RemoteOperationProgress> progress, CancellationToken cancellationToken)
		{
			var solution = await session.Controller.GetAsync(solutionId, cancellationToken);
			var layer = solution.Layers.EmptyIfNull().SelectMany(Solution.TraverseLayerTreePostOrder).FirstOrDefault(l => l.Id == layerId)
				?? throw new ArgumentException("The layer is not in the solution any more.");
			progress.Report(new RemoteOperationProgress($"Deleting layer '{layer.Name}' and its sub layers", -1));
			await session.Controller.DeleteLayerAsync(solutionId, layer, cancellationToken);
			if (openSolution?.Id == solutionId)
			{
				var store = session.GetViewStore(openSolution);
				foreach (var deleted in Solution.TraverseLayerTreePostOrder(layer))
				{
					store.Forget(deleted.Id + "/");
					summaries.Remove(deleted.Id);
					foreach (var key in views.Keys.Where(k => k.layerId == deleted.Id).ToList())
						views.Remove(key);
				}
				openSolution = null;
			}
			return new { deleted = layerId };
		}

		#endregion

		#region Import and upload

		/// <summary>
		/// New remote solution from FEM result files read by the browser: the files are stored in the results container,
		/// the master layer is generated here and uploaded, and the solution is created last, so that a listed solution is complete.
		/// </summary>
		public async Task<object> ImportResultsAsync(ImportRequest request, IReadOnlyList<StagedFile> files, IProgress<RemoteOperationProgress> progress, CancellationToken cancellationToken)
		{
			var meshFile = files.FirstOrDefault(f => f.Name == request.MeshFile) ?? throw new ArgumentException("The mesh file is missing.");
			var dataFiles = request.DataFiles.EmptyIfNull().Select(name => files.FirstOrDefault(f => f.Name == name) ?? throw new ArgumentException($"The result file {name} is missing.")).ToList();
			string importFolder = Guid.NewGuid().ToString("N");
			string recordOf(StagedFile file) => importFolder + "/" + file.Name;

			// FEM results, for imports into the solution later (the same records as the desktop import)
			var resultStore = new BlobRecordStore(session.ResultsContainer, session.User.Id);
			foreach (var file in dataFiles.Prepend(meshFile).Distinct())
				using (var stream = resultStore.Save(recordOf(file)))
					stream.Write(file.Content, 0, file.Content.Length);
			var uploadedResults = await resultStore.FlushAsync((done, total) =>
				progress.Report(new RemoteOperationProgress("Uploading the result files", percent(done, total))), cancellationToken);

			var solution = session.Controller.PrepareNew(
				string.IsNullOrWhiteSpace(request.ProjectName) ? Path.GetFileNameWithoutExtension(meshFile.Name) : request.ProjectName,
				new[] { new AnalysisResult { TimeStep = null, MeshRecordNames = new[] { recordOf(meshFile) }, DataRecordNames = dataFiles.Select(recordOf).ToArray() } });
			var layerStore = new BlobRecordStore(session.LayersContainer, session.Controller.GetLayerFolder(solution));
			try
			{
				progress.Report(new RemoteOperationProgress("Generating the master layer in this browser tab", -1));
				await Task.Yield();
				var logger = new ProgressLogger(progress);
				var generator = new LayerGenerator(layerStore, layerStore, compressionService: CompressionServiceFactory.Create(request.Compression.EmptyIfNull(), logger), logger: logger);
				var importServices = solution.Results.Select(result => AnalysisResultImportServiceFactory.Create(resultStore, result, request.GaussPointsExtrapolationStrategy));
				var master = generator.GenerateMasterLayer(string.IsNullOrWhiteSpace(request.MasterLayerName) ? DefaultMasterLayerName : request.MasterLayerName,
					importServices, request.KeyTimeSteps.EmptyIfNull(), fieldName: null, cancellationToken);

				var uploadedLayers = new List<string>();
				try
				{
					uploadedLayers.AddRange(await layerStore.FlushAsync((done, total) =>
						progress.Report(new RemoteOperationProgress("Uploading the master layer", percent(done, total))), cancellationToken));
				}
				catch (Exception)
				{
					await layerStore.DeleteBlobsAsync(layerStore.GetBlobNamesOfWrittenRecords());
					throw;
				}
				solution.Layers = new[] { toLayerRecord(master) };
				progress.Report(new RemoteOperationProgress("Creating the remote solution", -1));
				var created = await session.Controller.CreateAsync(solution, CancellationToken.None); // once the layer is uploaded, finish
				return new { id = created.Id, projectName = created.ProjectName };
			}
			catch (Exception)
			{
				await resultStore.DeleteBlobsAsync(uploadedResults);
				throw;
			}
		}

		/// <summary>
		/// Copies a local solution folder read by the browser (the solution file and the layer folders) as a new remote solution.
		/// </summary>
		public async Task<object> UploadLocalSolutionAsync(IReadOnlyList<StagedFile> files, IProgress<RemoteOperationProgress> progress, CancellationToken cancellationToken)
		{
			// paths are relative to the picked folder: "{folder}/{name}.solution.json", "{folder}/{layerId}/..."
			var solutionFile = files.Where(f => f.RelativePath.EndsWith(".solution.json", StringComparison.OrdinalIgnoreCase))
				.OrderBy(f => f.RelativePath.Count(ch => ch == '/')).FirstOrDefault()
				?? throw new ArgumentException("The folder has no solution file (*.solution.json).");
			string solutionDirectory = solutionFile.RelativePath.Contains('/') ? solutionFile.RelativePath.Substring(0, solutionFile.RelativePath.LastIndexOf('/') + 1) : "";
			Solution localSolution;
			using (var stream = new MemoryStream(solutionFile.Content))
				localSolution = new JsonSerializationService().Deserialize<Solution>(stream);

			var layerFiles = files.Where(f => f != solutionFile && f.RelativePath.StartsWith(solutionDirectory, StringComparison.Ordinal))
				.Select(f => new AzureBlobSolutionController.LayerFile(f.RelativePath.Substring(solutionDirectory.Length), f.Content.Length,
					_ => Task.FromResult<Stream>(new MemoryStream(f.Content, writable: false))))
				.ToList();
			var remote = await session.Controller.UploadAsync(localSolution, layerFiles, progress, cancellationToken);
			return new { id = remote.Id, projectName = remote.ProjectName };
		}

		#endregion

		#region View data

		/// <summary>
		/// Triangles and edges of one mesh of the layer. The buffers are returned separately (see WorkerApi).
		/// </summary>
		public async Task<(object meta, Dictionary<string, byte[]> buffers)> BuildViewAsync(int solutionId, Guid layerId, int meshIndex, CancellationToken cancellationToken)
		{
			var view = await getViewAsync(solutionId, layerId, meshIndex, cancellationToken);
			var meta = new
			{
				meshIndex,
				points = view.NumberOfPoints,
				cells = view.NumberOfCells,
				triangles = view.Positions.Length / 9,
				edges = view.EdgePositions.Length / 6,
				boundsMin = view.BoundsMin,
				boundsMax = view.BoundsMax
			};
			return (meta, new Dictionary<string, byte[]> { ["positions"] = toBytes(view.Positions), ["edges"] = toBytes(view.EdgePositions) });
		}

		/// <summary>
		/// One component (or the magnitude of the field) at one time step, per triangle corner of the mesh the time step belongs to.
		/// </summary>
		public async Task<(object meta, Dictionary<string, byte[]> buffers)> GetScalarsAsync(int solutionId, Guid layerId, string fieldName, string componentName, decimal timeStep, CancellationToken cancellationToken)
		{
			var (meshIndex, components) = await loadFieldAsync(solutionId, layerId, fieldName, componentName, timeStep, cancellationToken);
			var view = await getViewAsync(solutionId, layerId, meshIndex, cancellationToken);
			var location = components[0].Location;
			Func<int, double> valueAt = components.Count == 1
				? i => components[0].Values[i]
				: i => Math.Sqrt(components.Sum(c => c.Values[i] * c.Values[i]));
			float[] values = view.MapToCorners(location, valueAt);
			float min = values.Length == 0 ? 0 : values.Where(v => !float.IsNaN(v)).DefaultIfEmpty(0).Min();
			float max = values.Length == 0 ? 0 : values.Where(v => !float.IsNaN(v)).DefaultIfEmpty(0).Max();
			return (new { meshIndex, min, max, location = location.ToString() }, new Dictionary<string, byte[]> { ["scalars"] = toBytes(values) });
		}

		/// <summary>
		/// Vector field (e.g. displacement) at one time step, for the triangle corners and edge ends of the mesh.
		/// </summary>
		public async Task<(object meta, Dictionary<string, byte[]> buffers)> GetVectorsAsync(int solutionId, Guid layerId, string fieldName, decimal timeStep, CancellationToken cancellationToken)
		{
			var (meshIndex, components) = await loadFieldAsync(solutionId, layerId, fieldName, componentName: null, timeStep, cancellationToken);
			var view = await getViewAsync(solutionId, layerId, meshIndex, cancellationToken);
			float[] atPoints = view.VectorsAtPoints(components[0].Location, components.Select(c => c.Values).ToList());
			var (corners, edges) = view.PickVectors(atPoints);
			double maxMagnitude = 0;
			for (int p = 0; p < view.NumberOfPoints; p++)
				maxMagnitude = Math.Max(maxMagnitude, Math.Sqrt(atPoints[3 * p] * atPoints[3 * p] + atPoints[3 * p + 1] * atPoints[3 * p + 1] + atPoints[3 * p + 2] * atPoints[3 * p + 2]));
			return (new { meshIndex, maxMagnitude }, new Dictionary<string, byte[]> { ["cornerVectors"] = toBytes(corners), ["edgeVectors"] = toBytes(edges) });
		}

		#endregion

		#region Private methods

		private async Task<Solution> openAsync(int solutionId, bool refresh, CancellationToken cancellationToken)
		{
			if (openSolution == null || openSolution.Id != solutionId || refresh)
			{
				var solution = await session.Controller.GetAsync(solutionId, cancellationToken);
				if (openSolution?.Id != solutionId)
					closeSolution();
				openSolution = solution;
			}
			return openSolution;
		}

		private void closeSolution()
		{
			if (openSolution != null)
				session.ForgetViewStore(openSolution.Id);
			openSolution = null;
			views.Clear();
			summaries.Clear();
		}

		private async Task<SummaryFile> loadSummaryAsync(Solution solution, Guid layerId, CancellationToken cancellationToken)
		{
			if (!summaries.TryGetValue(layerId, out var summary))
			{
				var store = session.GetViewStore(solution);
				if (!await store.PrefetchAsync(summaryRecord(layerId), cancellationToken))
					throw new FileNotFoundException("The layer was not found in the remote storage.");
				summary = new LayerGenerator(store, null).LoadLayerSummary(layerId);
				summaries[layerId] = summary;
			}
			return summary;
		}

		private async Task<MeshView> getViewAsync(int solutionId, Guid layerId, int meshIndex, CancellationToken cancellationToken)
		{
			if (!views.TryGetValue((solutionId, layerId, meshIndex), out var view))
			{
				var solution = await openAsync(solutionId, refresh: false, cancellationToken);
				var summary = await loadSummaryAsync(solution, layerId, cancellationToken);
				var store = session.GetViewStore(solution);
				string record = $"{summary.MeshFallbackLayerId ?? layerId}/{meshIndex.ToString(CultureInfo.InvariantCulture)}.mesh.json";
				if (!await store.PrefetchAsync(record, cancellationToken))
					throw new FileNotFoundException($"The mesh of the layer was not found ({record}).");
				var geometry = new LayerGenerator(store, null).LoadGeometry(layerId, summary.MeshFallbackLayerId, meshIndex);
				view = MeshView.Build(geometry);
				views[(solutionId, layerId, meshIndex)] = view;
			}
			return view;
		}

		/// <summary>
		/// Components of the field at the time step (one component, or all of them when <paramref name="componentName"/> is null or "magnitude").
		/// </summary>
		private async Task<(int meshIndex, IReadOnlyList<ComponentDataDescription> components)> loadFieldAsync(int solutionId, Guid layerId, string fieldName, string componentName, decimal timeStep, CancellationToken cancellationToken)
		{
			var solution = await openAsync(solutionId, refresh: false, cancellationToken);
			var summary = await loadSummaryAsync(solution, layerId, cancellationToken);
			if (!summary.Fields.TryGetValue(fieldName, out var field))
				throw new ArgumentException($"The layer has no field '{fieldName}'.");
			bool all = string.IsNullOrEmpty(componentName) || componentName == "magnitude";
			var descriptors = (all ? field.Components.Values.ToArray() : new[] { field.Components[componentName] })
				.Select(c => c.TimeSteps.TryGetValue(timeStep, out var d) ? d : throw new ArgumentException($"The field '{fieldName}' has no time step {timeStep}."))
				.ToList();

			var store = session.GetViewStore(solution);
			var generator = new LayerGenerator(store, null);
			var components = new List<ComponentDataDescription>();
			foreach (var descriptor in descriptors)
			{
				string record = $"{summary.DataFallbackLayerId ?? layerId}/{descriptor.DataIndex.ToString(CultureInfo.InvariantCulture)}.result.json";
				if (!await store.PrefetchAsync(record, cancellationToken))
					throw new FileNotFoundException($"The data of the layer were not found ({record}).");
				components.Add(await generator.LoadDataForSingleTimeStepAsync(layerId, summary.DataFallbackLayerId, descriptor.DataIndex, timeStep, cancellationToken));
			}
			return (descriptors[0].MeshIndex, components);
		}

		/// <summary>
		/// Uploads the records of the new layer and adds it to the solution. A failed upload deletes the uploaded records.
		/// </summary>
		private async Task<Solution.Layer> uploadLayerAsync(BlobRecordStore store, SummaryFile summary, int solutionId, Guid parentLayerId, IProgress<RemoteOperationProgress> progress, CancellationToken cancellationToken)
		{
			var blobNames = store.GetBlobNamesOfWrittenRecords().ToList();
			try
			{
				await store.FlushAsync((done, total) => progress.Report(new RemoteOperationProgress("Uploading the new layer", percent(done, total))), cancellationToken);
				progress.Report(new RemoteOperationProgress("Adding the layer to the solution", -1));
				var layer = toLayerRecord(summary);
				await session.Controller.AddLayerAsync(solutionId, parentLayerId, layer, cancellationToken);
				return layer;
			}
			catch (Exception)
			{
				await store.DeleteBlobsAsync(blobNames);
				throw;
			}
		}

		private static Solution.Layer toLayerRecord(SummaryFile summary) => new Solution.Layer
		{
			Id = summary.Id,
			Name = summary.Name,
			FilterType = summary.Filter?.Type.ToString(),
			Children = null
		};

		private static string summaryRecord(Guid layerId) => $"{layerId}/summary.json";

		private static int percent(long done, long total) => total <= 0 ? 100 : (int)(done * 100 / total);

		private static byte[] toBytes(float[] values)
		{
			var bytes = new byte[values.Length * sizeof(float)];
			Buffer.BlockCopy(values, 0, bytes, 0, bytes.Length);
			return bytes;
		}

		#endregion
	}

	sealed class CreateLayerRequest
	{
		public int SolutionId { get; set; }
		public Guid ParentLayerId { get; set; }
		public string FilterType { get; set; }
		public string[] FilterParameters { get; set; }
		public decimal[] KeyTimeSteps { get; set; }
		public string[] Compression { get; set; }
		public string FieldName { get; set; }
		public string Name { get; set; }
	}

	sealed class ImportRequest
	{
		public string ProjectName { get; set; }
		public string MeshFile { get; set; }
		public string[] DataFiles { get; set; }
		public decimal[] KeyTimeSteps { get; set; }
		public string[] Compression { get; set; }
		public string GaussPointsExtrapolationStrategy { get; set; }
		public string MasterLayerName { get; set; }
	}

	/// <summary>
	/// File read by the browser for an import or upload. Relative path: in a picked folder, otherwise the name.
	/// </summary>
	sealed record StagedFile(string Name, string RelativePath, byte[] Content);

	/// <summary>
	/// Messages of the layer generator as progress of the operation.
	/// </summary>
	sealed class ProgressLogger : ILogger
	{
		private readonly IProgress<RemoteOperationProgress> progress;

		public ProgressLogger(IProgress<RemoteOperationProgress> progress) => this.progress = progress;

		public System.Diagnostics.TraceLevel Level { get; set; } = System.Diagnostics.TraceLevel.Info;

		public void LogOperationProgress(string message) => progress.Report(new RemoteOperationProgress(message, -1));
		public void LogMessage(string message) { }
		public void LogWarning(string message) => progress.Report(new RemoteOperationProgress("Warning: " + message, -1));
		public void LogError(string message) => progress.Report(new RemoteOperationProgress("Error: " + message, -1));
	}
}
