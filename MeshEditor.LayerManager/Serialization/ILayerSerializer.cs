using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.LayerManager.Serialization
{
	public interface ILayerSerializer
	{
		string FileExtension { get; }
		void Serialize<T>(T layerObject, Stream stream);
		T Deserialize<T>(Stream layerStream);
	}
}
