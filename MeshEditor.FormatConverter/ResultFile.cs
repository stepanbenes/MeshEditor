using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.FormatConverter
{
	class ResultFile : IResultDescription
	{
		public Guid LayerId { get; set; }

		public string ResultName { get; set; }

		public string ComponentName { get; set; }

		public double TimeStep { get; set; }

		// Data, wavelet coefficients ...

		/// <summary>
		/// Wavelet transform level, 0 means no transform
		/// </summary>
		public int CompressionLevel { get; set; }

		/// <summary>
		/// double array data in Base64 string format
		/// </summary>
		public string Data { get; set; }
	}
}
