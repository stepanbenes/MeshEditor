using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.LayerManager.Serialization
{
	public interface IStorageService
	{
		void Save(Stream stream, string filename);
		Stream Load(string filename);
	}
}
