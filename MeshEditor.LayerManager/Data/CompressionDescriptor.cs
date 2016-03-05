using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.LayerManager.Data
{
	public class CompressionDescriptor
	{
		/// <summary>
		/// Wavelet transform level, 0 means no transform
		/// </summary>
		public int Level { get; set; }

		// Wavelet coefficients
	}
}
