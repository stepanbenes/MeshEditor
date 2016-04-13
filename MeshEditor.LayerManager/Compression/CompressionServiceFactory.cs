using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.LayerManager.Compression
{
	public static class CompressionServiceFactory
	{
		public static ICompressionService Create(CompressionMethod compressionMethod)
		{
			switch (compressionMethod)
			{
				case CompressionMethod.None:
					return new TransparentCompressionService();
				case CompressionMethod.SVD:
					return new SVDCompressionService();
				case CompressionMethod.WT:
					return new WaveletCompressionService();
				default:
					throw new NotSupportedException();
			}
		}
	}
}
