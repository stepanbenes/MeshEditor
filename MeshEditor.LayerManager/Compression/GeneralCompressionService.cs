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
			//if ((string)compressionParameters["precision"] == "Single")
			//{
			//	float[] convertedArray = Array.ConvertAll(dataValues, Convert.ToSingle);
			//	// copy to byte array...
			//}

			compressionParameters["level"] = 0; // no compression, only copying data to byte array
			compressionParameters["type"] = "Double"; // or Single, or Int32, ...
			compressionParameters["dimensions"] = new int[] { dataValues.Length, 1 /* time steps count */ };

			// ignoring parameters

			byte[] bytes = new byte[dataValues.Length * sizeof(double)];
			Buffer.BlockCopy(dataValues, 0, bytes, 0, bytes.Length);
			return bytes;
		}

		public double[] Decompress(byte[] compressedData, Dictionary<string, object> compressionParameters)
		{
			int level = 0;
			string type = "Double";
			int[] dimensions = null;

			object parameter;
			if (compressionParameters.TryGetValue("level", out parameter))
				level = (int)parameter;
			if (compressionParameters.TryGetValue("type", out parameter))
				type = (string)parameter;
			if (compressionParameters.TryGetValue("dimensions", out parameter))
				dimensions = (int[])parameter;

			if (dimensions?.Length != 2 || dimensions[0] < 0 || dimensions[1] < 1)
				throw new Exception("Unknown dimensions");
			if (dimensions[1] > 1)
				throw new NotImplementedException();
			if (level != 0)
				throw new NotImplementedException();
			if (type != "Double")
				throw new NotImplementedException();

			double[] values = new double[dimensions[0]];
			Buffer.BlockCopy(compressedData, 0, values, 0, compressedData.Length);
			return values;
		}
	}
}
