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
		public byte[] Compress(double[] dataValues, CompressionDescriptor compressionParameters)
		{
			//if ((string)compressionParameters["precision"] == "Single")
			//{
			//	float[] convertedArray = Array.ConvertAll(dataValues, Convert.ToSingle);
			//	// copy to byte array...
			//}

			compressionParameters.Level = 0; // no compression, only copying data to byte array
			compressionParameters.DataType = DataArrayType.Float64;
			compressionParameters.Dimensions = new int[] { dataValues.Length, 1 /* time steps count */ };

			// ignoring parameters

			byte[] bytes = new byte[dataValues.Length * sizeof(double)];
			Buffer.BlockCopy(dataValues, 0, bytes, 0, bytes.Length);
			return bytes;
		}

		public double[] Decompress(byte[] compressedData, CompressionDescriptor compressionParameters)
		{
			if (compressionParameters.Dimensions?.Length != 2 || compressionParameters.Dimensions[0] < 0 || compressionParameters.Dimensions[1] < 1)
				throw new Exception("Unknown dimensions");
			if (compressionParameters.Dimensions[1] > 1)
				throw new NotImplementedException();
			if (compressionParameters.Level != 0)
				throw new NotImplementedException();
			if (compressionParameters.DataType != DataArrayType.Float64)
				throw new NotImplementedException();

			double[] values = new double[compressionParameters.Dimensions[0]];
			Buffer.BlockCopy(compressedData, 0, values, 0, compressedData.Length);
			return values;
		}
	}
}
