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
		Stream Save(string fileName);
		Stream Save(string recordName, string fileExtension);
		Stream Load(string fileName);
		Stream Load(string recordName, string fileExtension);
	}
}
