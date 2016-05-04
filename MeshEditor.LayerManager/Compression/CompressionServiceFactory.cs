using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.LayerManager.Compression
{
	public static class CompressionServiceFactory
	{
		public static ICompressionService Create(CompressionMethod compressionMethod, IEnumerable<string> parameters = null)
		{
			switch (compressionMethod)
			{
				case CompressionMethod.Transparent:
					return new TransparentCompressionService();
				case CompressionMethod.SVD:
					{
						string strategyString = parameters?.ElementAtOrDefault(0);
						string factorString = parameters?.ElementAtOrDefault(1);
						if (strategyString != null)
						{
							var strategy = (SVDCompressionStrategy)Enum.Parse(typeof(SVDCompressionStrategy), strategyString, ignoreCase: true);
							if (factorString != null)
							{
								var factor = double.Parse(factorString);
								return new SVDCompressionService(strategy, factor);
							}
							return new SVDCompressionService(strategy);
						}
						return new SVDCompressionService();
					}
				case CompressionMethod.WT:
					return new WaveletCompressionService();
				default:
					throw new NotSupportedException();
			}
		}
	}
}
