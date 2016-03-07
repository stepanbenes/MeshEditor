using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.LayerManager.Storage;

namespace MeshEditor.LayerManager.Serialization
{
	public class MemorySerializer : ILayerSerializer
	{
		Dictionary<string, object> layerObjects = new Dictionary<string, object>();

		public T Deserialize<T>(string recordName, IStorageService ignored)
		{
			return (T)layerObjects[recordName];
		}

		public void Serialize<T>(T layerObject, string recordName, IStorageService ignored)
		{
			layerObjects[recordName] = layerObject;
		}
	}
}
