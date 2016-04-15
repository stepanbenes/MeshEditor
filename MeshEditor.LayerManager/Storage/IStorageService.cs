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
		Stream Load(string record);
	}

	public interface IWriteStorageService
	{
		Stream Save(string record);
		void Delete(string record);
	}
}
