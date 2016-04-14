using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.LayerManager.Compression
{
	public class WaveletCompressionParameters : CompressionParameters
	{
		public override CompressionMethod Method => CompressionMethod.WT;

		public double MinDataValue { get; set; }
		public double MaxDataValue { get; set; }
		public int Iterations { get; set; }
	}
}
