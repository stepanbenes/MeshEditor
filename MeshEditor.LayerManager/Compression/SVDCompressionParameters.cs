using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.LayerManager.Compression
{
	public class SVDCompressionParameters : CompressionParameters
	{
		public override CompressionMethod Method => CompressionMethod.SVD;

		/// <summary>
		/// Number of singular values taken
		/// </summary>
		public int Rank { get; set; }

#if DEBUG
		public double CompressionFactor { get; set; }
		public double MaxDataValue { get; set; }
		public double MinDataValue { get; set; }
		public double MaxRelativeError { get; set; }
#endif
	}
}
