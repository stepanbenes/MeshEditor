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
						bool randomized = false;
						SVDCompressionFocus? focus = null;
						double? factor = null;

						foreach (string parameter in parameters ?? Enumerable.Empty<string>())
						{
							SVDCompressionFocus testFocus;
							double testFactor;
							if (string.Equals(parameter, nameof(randomized), StringComparison.InvariantCultureIgnoreCase))
							{
								randomized = true;
							}
							else if (double.TryParse(parameter, out testFactor))
							{
								factor = testFactor;
							}
							else if (Enum.TryParse(parameter, ignoreCase: true, result: out testFocus))
							{
								focus = testFocus;
							}
							else
							{
								throw new FormatException($"Unknown compression parameter '{parameter}'");
							}
						}

						if (focus.HasValue)
						{
							if (factor.HasValue)
							{
								return new SVDCompressionService(randomized, focus.Value, factor.Value);
							}
							return new SVDCompressionService(randomized, focus.Value);
						}
						return new SVDCompressionService(randomized);
					}
				case CompressionMethod.WT:
					return new WaveletCompressionService();
				default:
					throw new NotSupportedException();
			}
		}
	}
}
