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
		public Stream Load(Uri uri)
		{
			return new FileStream(uri.LocalPath, FileMode.Open, FileAccess.Read, FileShare.Read);
		}

		public Stream Save(Uri uri)
		{
			string localPath = uri.LocalPath;
			string directory = Path.GetDirectoryName(localPath);
			if (!Directory.Exists(directory))
			{
				Directory.CreateDirectory(directory);
			}
			return new FileStream(localPath, FileMode.Create, FileAccess.Write, FileShare.None);
		}

		public void Delete(Uri uri)
		{
			File.Delete(uri.LocalPath);
		}
	}
}
