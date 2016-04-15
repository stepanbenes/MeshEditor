using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.LayerManager.Compression
{
	internal class WaveletCompressionService : ICompressionService
	{
		#region Fields, constructor

		private const double W0 = 0.5;
		private const double W1 = -0.5;
		private const double S0 = 0.5;
		private const double S1 = 0.5;

		#endregion

		#region Public methods

		public double[] Compress(IEnumerable<double[]> dataValues, int rows, int columns, out CompressionParameters parameters)
		{
			// TODO: deal with NaN values
			// TODO: do 2D transform instead of sequence of 1D transforms

			double[][] allValues = dataValues.ToArray();

			double minValue = double.MaxValue;
			double maxValue = double.MinValue;
			for (int row = 0; row < rows; row++)
			{
				double[] rowValues = allValues[row];
				for (int column = 0; column < columns; column++)
				{
					minValue = Math.Min(minValue, rowValues[column]);
					maxValue = Math.Max(maxValue, rowValues[column]);
				}
			}

			const int ITERATIONS_COUNT = 1;

			parameters = new WaveletCompressionParameters
			{
				Rows = rows,
				Columns = columns,
				MinDataValue = minValue,
				MaxDataValue = maxValue,
				Iterations = ITERATIONS_COUNT
			};

			return TransparentCompressionService.LinearizeDataRows(allValues.Select(row => FWT(row, ITERATIONS_COUNT, minValue, maxValue)), rows, columns);
		}

		public IEnumerable<double[]> Decompress(double[] compressedData, CompressionParameters parameters)
		{
			Debug.Assert(parameters is WaveletCompressionParameters);
			Debug.Assert(parameters.Method == CompressionMethod.WT);
			WaveletCompressionParameters waveletParameters = (WaveletCompressionParameters)parameters;

			foreach (double[] compressedRow in TransparentCompressionService.EnumerateDataRows(compressedData, parameters.Rows, parameters.Columns))
			{
				yield return IWT(compressedRow, waveletParameters.Iterations, waveletParameters.MinDataValue, waveletParameters.MaxDataValue);
			}
		}

		#endregion

		#region Wavelet transform

		private static double[] FWT(double[] input, int iterations, double minDataValue, double maxDataValue)
		{
			int newLength = findClosestNumberDivisibleBy(number: input.Length, divider: 1 << iterations);
			double[] scaledInput = enlarge(input, newLength);
			for (int i = 0; i < input.Length; i++)
			{
				scaledInput[i] = scale(minDataValue, maxDataValue, -1, 1, scaledInput[i]);
			}
			int usableLength = scaledInput.Length;
			Debug.Assert((usableLength >> iterations) > 1);
			for (int i = 0; i < iterations; i++)
			{
				FWTiteration(scaledInput, usableLength);
				usableLength >>= 1;
			}

			//for (int i = usableLength; i < scaledInput.Length; i++) // throw away details
			//{
			//	scaledInput[i] = 0.0;
			//}

			return shrink(scaledInput, input.Length);
		}

		private static double[] IWT(double[] input, int iterations, double minDataValue, double maxDataValue)
		{
			int newLength = findClosestNumberDivisibleBy(number: input.Length, divider: 1 << iterations);
			double[] result = enlarge(input, newLength);
			if (iterations > 0)
			{
				int usableLength = result.Length >> (iterations - 1);
				Debug.Assert(usableLength > 1);
				for (int i = 0; i < iterations; i++)
				{
					IWTiteration(result, usableLength);
					usableLength <<= 1;
				}
			}
			for (int i = 0; i < input.Length; i++)
			{
				result[i] = scale(-1, 1, minDataValue, maxDataValue, result[i]); // scale output back
			}
			return shrink(result, input.Length);
		}

		private static void FWTiteration(double[] input, int usableLength)
		{
			Debug.Assert(input != null);
			Debug.Assert(usableLength <= input.Length);
			double[] output = new double[usableLength];
			int h = usableLength >> 1;
			for (int i = 0; i < h; i++)
			{
				int k = (i << 1);
				output[i] = input[k] * S0 + input[k + 1] * S1;
				output[i + h] = input[k] * W0 + input[k + 1] * W1;
			}
			for (int i = 0; i < usableLength; i++)
			{
				input[i] = output[i];
			}
		}

		private static void IWTiteration(double[] input, int usableLength)
		{
			Debug.Assert(input != null);
			Debug.Assert(usableLength <= input.Length);
			double[] output = new double[usableLength];
			int h = usableLength >> 1; // TODO: handle cases when the length of the input is not a power of two
			for (int i = 0; i < h; i++)
			{
				int k = (i << 1);
				output[k] = (input[i] * S0 + input[i + h] * W0) / W0;
				output[k + 1] = (input[i] * S1 + input[i + h] * W1) / S0;
			}
			for (int i = 0; i < usableLength; i++)
			{
				input[i] = output[i];
			}
		}

		private static int findClosestNumberDivisibleBy(int number, int divider)
		{
			return (number / divider + 1) * divider;
		}

		private static int getNearestPowerOfTwo(int number)
		{
			Debug.Assert(number >= 0);
			int n = number - 1;
			n |= n >> 1;
			n |= n >> 2;
			n |= n >> 4;
			n |= n >> 8;
			n |= n >> 16;
			return n + 1;
		}

		private static double scale(double fromMin, double fromMax, double toMin, double toMax, double x)
		{
			if (fromMax - fromMin == 0) return 0;
			double value = (toMax - toMin) * (x - fromMin) / (fromMax - fromMin) + toMin;
			if (value > toMax)
			{
				value = toMax;
			}
			if (value < toMin)
			{
				value = toMin;
			}
			return value;
		}

		private static double[] enlarge(double[] array, int newLength)
		{
			if (newLength == array.Length)
				return array.ToArray(); // create copy
			double[] result = array;
			Array.Resize(ref result, newLength); // enlarge
			Debug.Assert(!ReferenceEquals(result, array));
			Debug.Assert(result.Length == newLength);
			return result;
		}

		private static double[] shrink(double[] array, int newLength)
		{
			double[] result = array;
			Array.Resize(ref result, newLength);
			Debug.Assert(result.Length == newLength);
			return result;
		}

		#endregion
	}
}
