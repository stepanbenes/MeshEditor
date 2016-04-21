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

		public Solution Get(int solutionId)
		{
			using (Stream stream = localStorage.Load(findRecordNameOfSolution(solutionId)))
			{
				return serializer.Deserialize<Solution>(stream);
			}
		}

		public void CreateNew(SolutionBase solution)
		{
			string projectNameAsValidFileName = solution.ProjectName.MakeAlphanumericFilename();
			string solutionRecordName = projectNameAsValidFileName + SolutionFileExtension + serializer.FileExtension;
			using (Stream stream = localStorage.Save(solutionRecordName))
			{
				serializer.Serialize(solution, stream);
			}
		}

		public void AddLayer(Solution solution, Solution.Layer parentLayer, Solution.Layer newLayer)
		{
			if (parentLayer == null)
			{
				solution.Layers = solution.Layers.EmptyIfNull().Append(newLayer).ToArray();
			}
			else
			{
				parentLayer.Children = parentLayer.Children.EmptyIfNull().Append(newLayer).ToArray();
			}
			using (Stream stream = localStorage.Save(findRecordNameOfSolution(solution.Id)))
			{
				serializer.Serialize(solution, stream);
			}
		}

		#region Private methods

		private IEnumerable<string> findAllSolutionFilesInSolutionDirectory()
		{
			return Directory.EnumerateFiles(solutionDirectory, "*" + SolutionFileExtension + serializer.FileExtension, SearchOption.TopDirectoryOnly);
		}

		private string findRecordNameOfSolution(int solutionId)
		{
			foreach (string solutionFile in findAllSolutionFilesInSolutionDirectory())
			{
				using (Stream stream = localStorage.Load(Path.GetFileName(solutionFile)))
				{
					var testSolution = serializer.Deserialize<SolutionBase>(stream);
					if (testSolution.Id == solutionId)
					{
						return Path.GetFileName(solutionFile);
					}
				}
			}
			throw new FileNotFoundException();
		}

		#endregion
	}
}
