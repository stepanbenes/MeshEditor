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

		public ISolutionInfo LoadSolutionFromFileName(string relativeFilename)
		{
			using (Stream stream = localStorage.Load(relativeFilename))
			{
				return serializer.Deserialize<SolutionBase>(stream);
			}
		}

		#region Private methods

		private IEnumerable<string> findAllSolutionFilesInSolutionDirectory()
		{
			return Directory.EnumerateFiles(solutionDirectory, "*" + SolutionFileSuffix + serializer.FileExtension, SearchOption.TopDirectoryOnly);
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
