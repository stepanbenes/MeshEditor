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
		double[] Compress(double[] dataValues);
		double[] Decompress(double[] compressedData);
	}
}
