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

		IStorageService storage;
		ISerializationService serializer;
		string solutionDirectory;

		public LocalSolutionProvider(string solutionDirectory)
		{
			Debug.Assert(solutionDirectory != null);
			this.solutionDirectory = solutionDirectory;
			storage = new LocalFileSystemStorageService(solutionDirectory);
			serializer = new JsonSerializationService();
		}

		public IEnumerable<ISolutionInfo> GetAll()
		{
			foreach (string solutionFile in findAllSolutionFilesInSolutionDirectory())
			{
				using (Stream stream = storage.Load(Path.GetFileName(solutionFile)))
				{
					var solutionBase = serializer.Deserialize<SolutionBase>(stream);
					solutionBase.Uri = new Uri(solutionFile);
					yield return solutionBase;
				}
			}
		}

		public Solution Get(Uri uri)
		{
			using (Stream stream = storage.Load(uri.LocalPath))
			{
				var solution = serializer.Deserialize<Solution>(stream);
				solution.Uri = uri;
				return solution;
			}
		}

		public void Create(Solution solution)
		{
			string projectNameAsValidFileName = solution.ProjectName.MakeAlphanumericFilename();
			string solutionRecordName = projectNameAsValidFileName + SolutionFileExtension + serializer.FileExtension;
			
			using (Stream stream = storage.Save(solutionRecordName))
			{
				// TODO: create Id
				solution.Uri = new Uri(Path.Combine(solutionDirectory, solutionRecordName));
				serializer.Serialize(solution, stream);
			}
		}

		public void Update(Solution solution)
		{
			using (Stream stream = storage.Save(Path.GetFileName(solution.Uri.LocalPath)))
			{
				serializer.Serialize(solution, stream);
			}
		}

		#region Private methods

		private IEnumerable<string> findAllSolutionFilesInSolutionDirectory()
		{
			return Directory.EnumerateFiles(solutionDirectory, "*" + SolutionFileExtension + serializer.FileExtension, SearchOption.TopDirectoryOnly);
		}

		#endregion
	}
}
