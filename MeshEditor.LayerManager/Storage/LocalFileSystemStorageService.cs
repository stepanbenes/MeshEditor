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
		string directoryName;

		public LocalFileSystemStorageService(string directoryName)
		{
			this.directoryName = directoryName;
		}

		public Stream Load(string fileName)
		{
			Debug.Assert(!Path.IsPathRooted(fileName));
			Debug.Assert(!string.IsNullOrEmpty(fileName));
			Debug.Assert(Path.HasExtension(fileName));

			return new FileStream(Path.Combine(directoryName, fileName), FileMode.Open, FileAccess.Read, FileShare.Read);
		}

		public Stream Load(string recordName, string fileExtension)
		{
			Debug.Assert(!Path.IsPathRooted(recordName));
			Debug.Assert(!string.IsNullOrEmpty(recordName));
			Debug.Assert(!string.IsNullOrEmpty(fileExtension));
			Debug.Assert(fileExtension.StartsWith("."));

			return Load(recordName + fileExtension);
		}

		public Stream Save(string fileName)
		{
			Debug.Assert(!Path.IsPathRooted(fileName));
			Debug.Assert(!string.IsNullOrEmpty(fileName));
			Debug.Assert(Path.HasExtension(fileName));

			return new FileStream(Path.Combine(directoryName, fileName), FileMode.OpenOrCreate/**/, FileAccess.Write, FileShare.None);
		}

		public Stream Save(string recordName, string fileExtension)
		{
			Debug.Assert(!Path.IsPathRooted(recordName));
			Debug.Assert(!string.IsNullOrEmpty(recordName));
			Debug.Assert(!string.IsNullOrEmpty(fileExtension));
			Debug.Assert(fileExtension.StartsWith("."));

			return Save(recordName + fileExtension);
		}
	}
}
