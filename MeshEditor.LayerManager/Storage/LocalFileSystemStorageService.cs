using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.LayerManager.Serialization;

namespace MeshEditor.LayerManager.Storage
{
	public class LocalFileSystemStorageService : IStorageService
	{
		readonly string basePath;

		public LocalFileSystemStorageService(string basePath)
		{
			Debug.Assert(basePath != null);
			this.basePath = basePath;
		}

		public Stream Load(string record)
		{
			return new FileStream(combineWithBasePath(record), FileMode.Open, FileAccess.Read, FileShare.Read);
		}

		public Stream Save(string record)
		{
			string path = combineWithBasePath(record);
			string directory = Path.GetDirectoryName(path);
			if (!Directory.Exists(directory))
			{
				Directory.CreateDirectory(directory);
			}
			return new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
		}

		public void Delete(string record)
		{
			File.Delete(combineWithBasePath(record));
		}

		public void DeleteDirectory(string name)
		{
			Directory.Delete(combineWithBasePath(name), recursive: true);
		}

		private string combineWithBasePath(string path)
		{
			Debug.Assert(basePath != null);
			Debug.Assert(path != null);
			if (Path.IsPathRooted(path))
				return path;
			return Path.Combine(basePath, path);
		}
	}
}
