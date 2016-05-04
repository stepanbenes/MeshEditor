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
						bool randomize = false;
						SVDCompressionFocus? focus = null;
						double? factor = null;

						foreach (string parameter in parameters ?? Enumerable.Empty<string>())
						{
							SVDCompressionFocus testFocus;
							double testFactor;
							if (string.Equals(parameter, "randomize", StringComparison.InvariantCultureIgnoreCase))
							{
								randomize = true;
							}
							else if (double.TryParse(parameter, out testFactor))
							{
								factor = testFactor;
							}
							else if (Enum.TryParse(parameter, ignoreCase: true, result: out testFocus))
							{
								focus = testFocus;
							}
						}

						if (focus.HasValue)
						{
							if (factor.HasValue)
							{
								return new SVDCompressionService(randomize, focus.Value, factor.Value);
							}
							return new SVDCompressionService(randomize, focus.Value);
						}
						return new SVDCompressionService(randomize);
					}
				case CompressionMethod.WT:
					return new WaveletCompressionService();
				default:
					throw new NotSupportedException();
			}
		}
	}
}
