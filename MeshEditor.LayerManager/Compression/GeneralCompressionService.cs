using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.LayerManager.Data;

namespace MeshEditor.LayerManager.Compression
{
	internal class GeneralCompressionService : ICompressionService
	{
		public byte[] Compress(double[] dataValues, Dictionary<string, object> compressionParameters)
		{
			//if ((string)compressionParameters["precision"] == "single")
			//{
			//	float[] convertedArray = Array.ConvertAll(dataValues, Convert.ToSingle);
			//	// copy to byte array...
			//}

			compressionParameters["level"] = 0; // no compression, only copying data to byte array
			compressionParameters["precision"] = "double"; // or single, or half
			compressionParameters["dimensions"] = new int[] { dataValues.Length, 1 /* time steps count */ };

			byte[] bytes = new byte[dataValues.Length * sizeof(double)];
			Buffer.BlockCopy(dataValues, 0, bytes, 0, bytes.Length);
			return bytes;
		}
	}
}
