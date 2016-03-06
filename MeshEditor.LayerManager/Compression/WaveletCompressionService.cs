using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.LayerManager.Data;

namespace MeshEditor.LayerManager.Compression
{
	internal class WaveletCompressionService : ICompressionService
	{
		public byte[] Compress(double[] dataValues, Dictionary<string, object> compressionParameters)
		{
			compressionParameters["level"] = 0;
			compressionParameters["precision"] = "double";

			byte[] bytes = new byte[dataValues.Length * sizeof(double)];
			Buffer.BlockCopy(dataValues, 0, bytes, 0, bytes.Length);
			return bytes;
		}
	}
}
