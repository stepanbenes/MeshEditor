using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.LayerManager.Data;

namespace MeshEditor.LayerManager.Compression
{
	public interface ICompressionService
	{
		byte[] Compress(double[] dataValues, Dictionary<string, object> compressionParameters);
		//byte[] Compress(float[] dataValues, Dictionary<string, object> compressionParameters);
		//byte[] Compress(double[,] dataValues, Dictionary<string, object> compressionParameters);
		//double[] Decompress(byte[] compressedData, Dictionary<string, object> compressionParameters);
		//double[,] DecompressMatrix(byte[] compressedData, Dictionary<string, object> compressionParameters);
	}
}
