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

		public Solution CreateNew(int solutionId, IEnumerable<AnalysisResult> analysisResults, string projectName = null)
		{
			projectName = string.IsNullOrWhiteSpace(projectName) ? Path.GetFileNameWithoutExtension(analysisResults.First().MeshRecordNames.First()) : projectName;
			Solution solution = new Solution { Id = solutionId, ProjectName = projectName, Layers = new Solution.Layer[0] };
			using (Stream stream = localStorage.Save(projectName.MakeAlphanumericFilename() + SolutionFileSuffix + serializer.FileExtension))
			{
				serializer.Serialize(solution, stream);
			}
			return solution;
		}

		public IEnumerable<ISolutionInfo> GetAll()
		{
			foreach (string solutionRecord in findAllRecordsInSolutionDirectory())
			{
				using (Stream stream = localStorage.Load(solutionRecord))
				{
					yield return serializer.Deserialize<SolutionBase>(stream);
				}
			}
		}

		public Solution Get(int solutionId)
		{
			using (Stream stream = localStorage.Load(findRecordNameOfSolution(solutionId)))
			{
				return serializer.Deserialize<Solution>(stream);
			}
		}

		public Solution AddLayer(Solution solution, Solution.Layer parentLayer, Solution.Layer newLayer)
		{
			Solution updatedSolution = Solution.CreateNewByAddingLayer(solution, newLayer, parentLayer?.Id);
			using (Stream stream = localStorage.Save(findRecordNameOfSolution(solution.Id)))
			{
				serializer.Serialize(updatedSolution, stream);
			}
			return updatedSolution;
		}

		public Solution DeleteLayer(Solution solution, Solution.Layer layerToDelete)
		{
			Solution updatedSolution = Solution.CreateNewByDeletingLayer(solution, layerToDelete.Id);
			using (Stream stream = localStorage.Save(findRecordNameOfSolution(solution.Id)))
			{
				serializer.Serialize(updatedSolution, stream);
			}
			return updatedSolution;
		}

		public ISolutionInfo LoadSolutionFromFileName(string relativeFilename)
		{
			using (Stream stream = localStorage.Load(relativeFilename))
			{
				return serializer.Deserialize<SolutionBase>(stream);
			}
		}

		public async Task<IEnumerable<ISolutionInfo>> GetAllAsync(CancellationToken cancellationToken)
		{
			return await Task.WhenAll(from solutionRecord in findAllRecordsInSolutionDirectory()
									  select loadSolutionInfo(solutionRecord, cancellationToken));
		}

		public async Task<Solution> GetAsync(int solutionId, CancellationToken cancellationToken)
		{
			using (Stream stream = localStorage.Load(await findRecordNameOfSolutionAsync(solutionId, cancellationToken)))
			{
				return await serializer.DeserializeAsync<Solution>(stream, cancellationToken);
			}
		}

		#region Private methods

		private async Task<ISolutionInfo> loadSolutionInfo(string solutionRecord, CancellationToken cancellationToken)
		{
			using (Stream stream = localStorage.Load(solutionRecord))
			{
				return await serializer.DeserializeAsync<Solution>(stream, cancellationToken);
			}
		}

		private IEnumerable<string> findAllRecordsInSolutionDirectory()
		{
			return from filepath in Directory.EnumerateFiles(solutionDirectory, "*" + SolutionFileSuffix + serializer.FileExtension, SearchOption.TopDirectoryOnly)
				   select Path.GetFileName(filepath);
		}

		private string findRecordNameOfSolution(int solutionId)
		{
			foreach (string solutionRecord in findAllRecordsInSolutionDirectory())
			{
				using (Stream stream = localStorage.Load(solutionRecord))
				{
					var testSolution = serializer.Deserialize<SolutionBase>(stream);
					if (testSolution.Id == solutionId)
					{
						return solutionRecord;
					}
				}
			}
			throw new FileNotFoundException();
		}

		private async Task<string> findRecordNameOfSolutionAsync(int solutionId, CancellationToken cancellationToken)
		{
			foreach (string solutionRecord in findAllRecordsInSolutionDirectory())
			{
				using (Stream stream = localStorage.Load(solutionRecord))
				{
					var testSolution = await serializer.DeserializeAsync<SolutionBase>(stream, cancellationToken);
					if (testSolution.Id == solutionId)
					{
						return solutionRecord;
					}
				}
			}
			throw new FileNotFoundException();
		}

		#endregion

	}
}
