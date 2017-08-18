using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.Common.Logging;

namespace MeshEditor.LayerManager.Compression
{
	public static class CompressionServiceFactory
	{
		public static ICompressionService Create(CompressionMethod compressionMethod, ILogger logger)
		{
			switch (compressionMethod)
			{
				case CompressionMethod.Transparent:
					return new TransparentCompressionService();
				case CompressionMethod.SVD:
					return new SVDCompressionService(randomized: false, logger: logger);
				case CompressionMethod.WT:
					return new WaveletCompressionService();
				default:
					throw new NotSupportedException();
			}
		}

		public static ICompressionService Create(IEnumerable<string> parameters, ILogger logger)
		{
			if (parameters == null)
				throw new ArgumentNullException(nameof(parameters));

			CompressionMethod compressionMethod;
			if (!parameters.Any())
			{
				//throw new ArgumentException("Parameters can not be empty. First parameter has to be name of compression method.", nameof(parameters));
				compressionMethod = CompressionMethod.Default;
			}
			else
			{
				string compressionMethodName = parameters.First();
				if (!Enum.TryParse(compressionMethodName, ignoreCase: true, result: out compressionMethod))
					throw new ArgumentException($"Unknown compression method passed as first parameter ({compressionMethodName})", nameof(parameters));
			}

			switch (compressionMethod)
			{
				case CompressionMethod.Transparent:
					return new TransparentCompressionService();
				case CompressionMethod.SVD:
					{
						bool randomized = false;
						SVDCompressionFocus focus = SVDCompressionFocus.None;
						double? value = null;

						foreach (string parameter in parameters.Skip(1))
						{
							SVDCompressionFocus testFocus;
							double testValue;
							if (string.Equals(parameter, nameof(randomized), StringComparison.InvariantCultureIgnoreCase))
							{
								randomized = true;
							}
							else if (double.TryParse(parameter, out testValue))
							{
								value = testValue;
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
						return new SVDCompressionService(randomized, logger, focus, value);
					}
				case CompressionMethod.WT:
					return new WaveletCompressionService();
				default:
					throw new NotSupportedException();
			}
		}
	}
}
