using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.LayerManager.Storage
{
	public interface IStorageService : IReadStorageService, IWriteStorageService
	{ }

	public interface IReadStorageService
	{
		Stream Load(Uri uri);
	}

	public interface IWriteStorageService
	{
		Stream Save(Uri uri);
		// void Delete(Uri uri);
	}
}
