using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.LayerManager.Common;
using MeshEditor.LayerManager.Serialization;
using MeshEditor.LayerManager.Storage;

namespace MeshEditor.SolutionManager.IO
{
	class LocalSolutionProvider : ISolutionProvider
	{
		private static readonly string SolutionFileExtension = ".solution";

		IStorageService localStorage;
		ISerializationService serializer;
		string solutionDirectory;

		public LocalSolutionProvider(string solutionDirectory)
		{
			Debug.Assert(solutionDirectory != null);
			this.solutionDirectory = solutionDirectory;
			localStorage = new LocalFileSystemStorageService(solutionDirectory);
			serializer = new JsonSerializationService();
		}

		public IEnumerable<ISolutionInfo> GetAll()
		{
			foreach (string solutionFile in findAllSolutionFilesInSolutionDirectory())
			{
				using (Stream stream = localStorage.Load(Path.GetFileName(solutionFile)))
				{
					yield return serializer.Deserialize<SolutionBase>(stream);
				}
			}
		}

		public Solution Get(ISolutionInfo solutionInfo)
		{
			using (Stream stream = localStorage.Load(findRecordNameOfSolution(solutionInfo)))
			{
				return serializer.Deserialize<Solution>(stream);
			}
		}

		public void Create(Solution solution)
		{
			string projectNameAsValidFileName = solution.ProjectName.MakeAlphanumericFilename();
			string solutionRecordName = projectNameAsValidFileName + SolutionFileExtension + serializer.FileExtension;
			solution.Id = findAvailableSolutionId();
			using (Stream stream = localStorage.Save(solutionRecordName))
			{
				serializer.Serialize(solution, stream);
			}
		}

		public void Update(Solution solution)
		{
			using (Stream stream = localStorage.Save(findRecordNameOfSolution(solution)))
			{
				serializer.Serialize(solution, stream);
			}
		}

		#region Private methods

		private IEnumerable<string> findAllSolutionFilesInSolutionDirectory()
		{
			return Directory.EnumerateFiles(solutionDirectory, "*" + SolutionFileExtension + serializer.FileExtension, SearchOption.TopDirectoryOnly);
		}

		private string findRecordNameOfSolution(ISolutionInfo solutionInfo)
		{
			foreach (string solutionFile in findAllSolutionFilesInSolutionDirectory())
			{
				using (Stream stream = localStorage.Load(Path.GetFileName(solutionFile)))
				{
					var testSolution = serializer.Deserialize<SolutionBase>(stream);
					if (testSolution.Id == solutionInfo.Id)
					{
						return Path.GetFileName(solutionFile);
					}
				}
			}
			throw new FileNotFoundException();
		}

		private int findAvailableSolutionId()
		{
			int maxId = 0;
			foreach (string solutionFile in findAllSolutionFilesInSolutionDirectory())
			{
				using (Stream stream = localStorage.Load(Path.GetFileName(solutionFile)))
				{
					var testSolution = serializer.Deserialize<SolutionBase>(stream);
					maxId = Math.Max(maxId, testSolution.Id);
				}
			}
			return maxId + 1;
		}

		#endregion
	}
}
