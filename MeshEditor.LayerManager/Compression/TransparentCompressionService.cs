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
		#region Static members

		public static double[] LinearizeDataRows(IEnumerable<double[]> dataValues, int rows, int columns)
		{
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

		public static IEnumerable<double[]> EnumerateDataRows(double[] linearizedDataValues, int rows, int columns)
		{
			Debug.Assert(linearizedDataValues.Length == rows * columns);
			if (rows == 1) // optimize for single row
			{
				Debug.Assert(linearizedDataValues.Length == columns);
				return Enumerable.Repeat(linearizedDataValues, 1); // return original array
			}
			return splitToChunks(linearizedDataValues, columns);
		}

		#endregion

		#region Public methods

		public double[] Compress(IEnumerable<double[]> dataValues, int rows, int columns, out CompressionParameters parameters)
		{
			parameters = new CompressionParameters
			{
				Rows = rows,
				Columns = columns,
			};
			return LinearizeDataRows(dataValues, rows, columns);
		}

		public IEnumerable<double[]> Decompress(double[] compressedData, CompressionParameters parameters)
		{
			Debug.Assert(parameters != null);
			Debug.Assert(parameters.Method == CompressionMethod.None);
			return EnumerateDataRows(compressedData, parameters.Rows, parameters.Columns);
		}

		#endregion

		#region Private methods

		private static IEnumerable<double[]> splitToChunks(double[] array, int chunkLength)
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
