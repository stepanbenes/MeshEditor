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

		public double[] Compress(double[] dataValues)
		{
			return dataValues;
		}

		public double[] Decompress(double[] compressedData)
		{
			return compressedData;
		}

		#endregion

		#region Private methods

		#endregion
	}
}
