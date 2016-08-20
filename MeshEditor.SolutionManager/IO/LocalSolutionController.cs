using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MeshEditor.LayerManager.Common;
using MeshEditor.LayerManager.Import;
using MeshEditor.LayerManager.Serialization;
using MeshEditor.LayerManager.Storage;

namespace MeshEditor.SolutionManager.IO
{
	class LocalSolutionController : ISolutionController
	{
		private static readonly string SolutionFileSuffix = ".solution";

		readonly IStorageService localStorage;
		readonly ISerializationService serializer;
		readonly string solutionDirectory;

		public LocalSolutionController(string solutionDirectory)
		{
			Debug.Assert(solutionDirectory != null);
			this.solutionDirectory = solutionDirectory;
			localStorage = new LocalFileSystemStorageService(solutionDirectory);
			serializer = new JsonSerializationService();
		}

		public Solution CreateNew(object solutionLocator /*ignored*/, IEnumerable<AnalysisResult> analysisResults, string projectName = null)
		{
			projectName = string.IsNullOrWhiteSpace(projectName) ? Path.GetFileNameWithoutExtension(analysisResults.First().MeshRecordNames.First()) : projectName;

			string prefix = projectName.MakeAlphanumeric();
			string suffix = SolutionFileSuffix + serializer.FileExtension;

			// solve conflicts (existing solution files with same name)
			var conflicts = Directory.Exists(solutionDirectory) ?
								(from fileMatch in Directory.EnumerateFiles(solutionDirectory, prefix + "*" + suffix, SearchOption.AllDirectories)
								 let fileMatchRelativePath = Path.GetFileName(fileMatch)
								 select fileMatchRelativePath.Substring(0, fileMatchRelativePath.Length - suffix.Length)).ToArray()
								:
								Array.Empty<string>();
			int solutionId;
			string recordName;
			if (conflicts.Length > 0)
			{
				int maxNumber = conflicts.Select(conflict => conflict.GetNumberAtTheEnd() ?? 0).Max();
				solutionId = maxNumber + 1;
				recordName = $"{prefix}_{solutionId}{suffix}";
			}
			else
			{
				solutionId = 0;
				recordName = prefix + suffix;
			}

			Solution solution = new Solution
			{
				Id = solutionId,
				ProjectName = projectName,
				Layers = new Solution.Layer[0]
			};

			Debug.Assert(!File.Exists(Path.Combine(solutionDirectory, recordName)));

			using (Stream stream = localStorage.Save(recordName))
			{
				serializer.Serialize(solution, stream);
			}

			solution.Location = Path.Combine(solutionDirectory, recordName);

			return solution;
		}

		IEnumerable<ISolutionInfo> ISolutionController.GetAll() => GetAll(includeSubDirectories: true);

		public IEnumerable<ISolutionInfo> GetAll(bool includeSubDirectories)
		{
			foreach (string solutionFile in getAllSolutionFiles(includeSubDirectories))
			{
				using (Stream stream = localStorage.Load(solutionFile))
				{
					var solution = serializer.Deserialize<SolutionInfo>(stream);
					solution.Location = solutionFile;
					yield return solution;
				}
			}
		}

		Task<IEnumerable<ISolutionInfo>> ISolutionController.GetAllAsync(CancellationToken cancellationToken) => GetAllAsync(includeSubDirectories: true, cancellationToken: cancellationToken);

		public async Task<IEnumerable<ISolutionInfo>> GetAllAsync(bool includeSubDirectories, CancellationToken cancellationToken)
		{
			return await Task.WhenAll(from solutionFile in getAllSolutionFiles(includeSubDirectories)
									  select loadSolutionInfoAsync(solutionFile, cancellationToken));
		}

		public Solution Get(object solutionLocator)
		{
			if (!(solutionLocator is string))
				throw new ArgumentException("Solution file is not specified", nameof(solutionLocator));

			string solutionFile = (string)solutionLocator;
			using (Stream stream = localStorage.Load(solutionFile))
			{
				var solution = serializer.Deserialize<Solution>(stream);
				solution.Location = solutionFile;
				return solution;
			}
		}

		public async Task<Solution> GetAsync(object solutionLocator, CancellationToken cancellationToken)
		{
			if (!(solutionLocator is string))
				throw new ArgumentException("Solution file is not specified", nameof(solutionLocator));

			string solutionFile = (string)solutionLocator;
			using (Stream stream = localStorage.Load(solutionFile))
			{
				var solution = await serializer.DeserializeAsync<Solution>(stream, cancellationToken);
				solution.Location = solutionFile;
				return solution;
			}
		}

		public void Delete(object solutionLocator)
		{
			if (!(solutionLocator is string))
				throw new ArgumentException("Solution file is not specified", nameof(solutionLocator));

			Solution solution = Get(solutionLocator);
			deleteSolution(solution, (string)solutionLocator);
		}

		public async Task DeleteAsync(object solutionLocator, CancellationToken cancellationToken)
		{
			if (!(solutionLocator is string))
				throw new ArgumentException("Solution file is not specified", nameof(solutionLocator));

			Solution solution = await GetAsync(solutionLocator, cancellationToken);
			deleteSolution(solution, (string)solutionLocator);
		}

		public Solution AddLayer(Solution solution, Solution.Layer parentLayer, Solution.Layer newLayer)
		{
			Solution updatedSolution = Solution.CreateNewByAddingLayer(solution, newLayer, parentLayer?.Id);
			string solutionFile = solution.Location;
			using (Stream stream = localStorage.Save(solutionFile))
			{
				serializer.Serialize(updatedSolution, stream);
			}
			updatedSolution.Location = solutionFile;
			return updatedSolution;
		}

		public Solution DeleteLayer(Solution solution, Solution.Layer layerToDelete /*root layer*/)
		{
			deleteAllLayerFilesOfLayerTree(layerToDelete);

			Solution updatedSolution = Solution.CreateNewByDeletingLayer(solution, layerToDelete.Id);
			string solutionFile = solution.Location;
			using (Stream stream = localStorage.Save(solutionFile))
			{
				serializer.Serialize(updatedSolution, stream);
			}
			updatedSolution.Location = solutionFile;
			return updatedSolution;
		}

		public async Task<Solution> DeleteLayerAsync(Solution solution, Solution.Layer layerToDelete, CancellationToken cancellationToken)
		{
			deleteAllLayerFilesOfLayerTree(layerToDelete);

			Solution updatedSolution = Solution.CreateNewByDeletingLayer(solution, layerToDelete.Id);
			string solutionFile = solution.Location;
			using (Stream stream = localStorage.Save(solutionFile))
			{
				await serializer.SerializeAsync(updatedSolution, stream);
			}
			updatedSolution.Location = solutionFile;
			return updatedSolution;
		}

		#region Private methods

		private void deleteSolution(Solution solution, string solutionFile)
		{
			foreach (var rootLayer in solution.Layers)
			{
				deleteAllLayerFilesOfLayerTree(rootLayer);
			}

			localStorage.Delete(solutionFile); // delete solution file itself

			string solutionFileDirectory = Path.GetDirectoryName(solutionFile);
			if (!string.Equals(Path.GetFullPath(SolutionHub.GetLocalStorageDefaultDirectory()), Path.GetFullPath(solutionFileDirectory))) // directory is different from default solution directory
			{
				DirectoryInfo solutionFileDirectoryInfo = new DirectoryInfo(Path.GetDirectoryName(solutionFile));
				if (Path.GetFileName(solutionFile).StartsWith(solutionFileDirectoryInfo.Name)) // directory has same name as solution file and therefore was probably created during creation of solution
				{
					if (!Directory.EnumerateFileSystemEntries(solutionFileDirectory).Any()) // if directory is empty
					{
						Directory.Delete(solutionFileDirectory);
					}
				}
			}
		}

		private async Task<ISolutionInfo> loadSolutionInfoAsync(string solutionFile, CancellationToken cancellationToken)
		{
			using (Stream stream = localStorage.Load(solutionFile))
			{
				var solution = await serializer.DeserializeAsync<Solution>(stream, cancellationToken);
				solution.Location = solutionFile;
				return solution;
			}
		}

		private IEnumerable<string> getAllSolutionFiles(bool includeSubDirectories)
		{
			// NOTE: nested directories are joined using '\' (backslash) instead of '/' (forward slash)
			return Directory.EnumerateFiles(solutionDirectory, "*" + SolutionFileSuffix + serializer.FileExtension, includeSubDirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
		}

		private static IEnumerable<Solution.Layer> traverseLayerTreePostOrder(Solution.Layer root)
		{
			Stack<Solution.Layer> result = new Stack<Solution.Layer>();
			Stack<Solution.Layer> children = new Stack<Solution.Layer>();
			children.Push(root);
			while (children.Count > 0)
			{
				var layer = children.Pop();
				result.Push(layer);
				if (layer.Children != null)
				{
					foreach (var child in layer.Children)
					{
						children.Push(child);
					}
				}
			}
			return result;
		}

		private void deleteAllLayerFilesOfLayerTree(Solution.Layer rootLayer)
		{
			foreach (var childLayer in traverseLayerTreePostOrder(rootLayer))
			{
				localStorage.DeleteDirectory(childLayer.Id.ToString()); // WARNING: deletes all content in layer directory
			}
		}

		#endregion

	}
}
