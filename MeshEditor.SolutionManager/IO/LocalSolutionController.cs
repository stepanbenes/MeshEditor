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

		IStorageService localStorage;
		ISerializationService serializer;
		string solutionDirectory;

		public LocalSolutionController(string solutionDirectory)
		{
			Debug.Assert(solutionDirectory != null);
			this.solutionDirectory = solutionDirectory;
			localStorage = new LocalFileSystemStorageService(solutionDirectory);
			serializer = new JsonSerializationService();
		}

		public Solution CreateNew(object solutionLocator /*ignored*/, IEnumerable<AnalysisResult> analysisResults, string projectName = null)
		{
			// TODO: solve conflicts (existing solution file)
			projectName = string.IsNullOrWhiteSpace(projectName) ? Path.GetFileNameWithoutExtension(analysisResults.First().MeshRecordNames.First()) : projectName;
			string recordName = projectName.MakeAlphanumericFilename() + SolutionFileSuffix + serializer.FileExtension;
			Solution solution = new Solution
			{
				Id = 0,
				ProjectName = projectName,
				Layers = new Solution.Layer[0]
			};
			using (Stream stream = localStorage.Save(recordName))
			{
				serializer.Serialize(solution, stream);
			}
			solution.Location = Path.Combine(solutionDirectory, recordName);
			return solution;
		}

		public IEnumerable<ISolutionInfo> GetAll()
		{
			foreach (string solutionFile in getAllSolutionFiles())
			{
				using (Stream stream = localStorage.Load(solutionFile))
				{
					var solution = serializer.Deserialize<SolutionBase>(stream);
					solution.Location = solutionFile;
					yield return solution;
				}
			}
		}

		public async Task<IEnumerable<ISolutionInfo>> GetAllAsync(CancellationToken cancellationToken)
		{
			return await Task.WhenAll(from solutionFile in getAllSolutionFiles()
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

		public Solution DeleteLayer(Solution solution, Solution.Layer layerToDelete)
		{
			Solution updatedSolution = Solution.CreateNewByDeletingLayer(solution, layerToDelete.Id);
			string solutionFile = solution.Location;
			using (Stream stream = localStorage.Save(solutionFile))
			{
				serializer.Serialize(updatedSolution, stream);
			}
			updatedSolution.Location = solutionFile;
			return updatedSolution;
		}

		#region Private methods

		private async Task<ISolutionInfo> loadSolutionInfoAsync(string solutionFile, CancellationToken cancellationToken)
		{
			using (Stream stream = localStorage.Load(solutionFile))
			{
				var solution = await serializer.DeserializeAsync<Solution>(stream, cancellationToken);
				solution.Location = solutionFile;
				return solution;
			}
		}

		private IEnumerable<string> getAllSolutionFiles()
		{
			// NOTE: nested directories are joined using '\' (backslash) instead of '/' (forward slash)
			return Directory.EnumerateFiles(solutionDirectory, "*" + SolutionFileSuffix + serializer.FileExtension, SearchOption.AllDirectories);
		}

		#endregion

	}
}
