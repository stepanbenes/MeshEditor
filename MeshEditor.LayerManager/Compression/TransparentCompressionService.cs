using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.LayerManager.Data;
using System.Diagnostics;

namespace MeshEditor.LayerManager.Compression
{
	internal class TransparentCompressionService : ICompressionService
	{
		#region Public methods

		public double[] Compress(double[] dataValues, out CompressionParameters parameters)
		{
			parameters = null;
			return dataValues;
		}

		public double[] Decompress(double[] compressedData, CompressionParameters parameters)
		{
			Debug.Assert(parameters == null || parameters.Method == CompressionMethod.None);
			return compressedData;
		}

		#endregion

		#region Private methods

		#endregion
	}
}
