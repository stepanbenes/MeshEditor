using System;
using System.Collections.Generic;
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

		/// <summary>
		/// Compression ratio
		/// </summary>
		public double Factor { get; set; } // TODO: remove, only informative for debugging

		//public double Error { get; set; }
	}
}
