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
		double[] Compress(IEnumerable<double[]> dataValues, int rows, int columns, out CompressionParameters parameters);
		IEnumerable<double[]> Decompress(double[] compressedData, CompressionParameters parameters);
		double[] Decompress(double[] compressedData, int rowIndex, CompressionParameters parameters);
	}
}
