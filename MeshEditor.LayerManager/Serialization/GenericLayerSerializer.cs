using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.LayerManager.Serialization
{
	internal class GenericLayerSerializer : ILayerSerializer
	{
		public T Deserialize<T>(Stream layerStream)
		{
			throw new NotImplementedException();
		}

		public void Serialize<T>(T layerObject, Stream stream)
		{
			throw new NotImplementedException();
		}
	}
}
