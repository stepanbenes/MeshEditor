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

		public double[] Compress(IEnumerable<double[]> dataValues, int rows, int columns, out CompressionParameters parameters)
		{
			parameters = new CompressionParameters
			{
				Rows = rows,
				Columns = columns,
			};

			if (rows == 1)
			{
				return dataValues.Single();
			}

			double[] result = new double[rows * columns];
			int rowIndex = 0;
			foreach (double[] rowValues in dataValues)
			{
				Debug.Assert(rowValues.Length == columns);
				Array.Copy(rowValues, 0, result, rowIndex * columns, columns);
				rowIndex += 1;
			}
			Debug.Assert(rowIndex == rows);
			return result;
			//return dataValues.SelectMany(row => row).ToArray();
		}

		public IEnumerable<double[]> Decompress(double[] compressedData, CompressionParameters parameters)
		{
			Debug.Assert(parameters != null);
			Debug.Assert(parameters.Method == CompressionMethod.None);

			Debug.Assert(compressedData.Length == parameters.Rows * parameters.Columns);
			if (parameters.Rows == 1) // optimize for single row
			{
				return Enumerable.Repeat(compressedData, 1); // return original array
			}
			return splitToChunks(compressedData, parameters.Columns);
		}

		#endregion

		#region Private methods

		private IEnumerable<double[]> splitToChunks(double[] array, int chunkLength)
		{
			Debug.Assert(chunkLength <= array.Length);
			Debug.Assert(array.Length % chunkLength == 0);
			for (int i = 0; i < array.Length; i += chunkLength)
			{
				double[] chunk = new double[chunkLength];
				Array.Copy(array, i, chunk, 0, chunkLength);
				yield return chunk;
			}
		}

		#endregion
	}
}
