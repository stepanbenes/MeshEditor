using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.LayerManager.Data;
using MeshEditor.LayerManager.Common;
using System.Diagnostics;

namespace MeshEditor.LayerManager.Compression
{
	internal class GenericCompressionService : ICompressionService
	{
		#region Public methods

		public string TrimAndEncode<T>(T[] values) where T : struct
		{
			T[] trimmedValues = trimEnd(values);
			return Encode(trimmedValues);
		}

		public T[] DecodeAndExpand<T>(string data, int requestedLength) where T : struct
		{
			T[] values = Decode<T>(data);
			return expandEnd(values, requestedLength);
		}

		public string Encode<T>(T[] values) where T : struct
		{
			byte[] bytes = scatterArrayToBytes(values);
			return Convert.ToBase64String(bytes);
		}

		public T[] Decode<T>(string data) where T : struct
		{
			byte[] bytes = Convert.FromBase64String(data);
			return gatherArrayOfBytes<T>(bytes);
		}

		public string CompressAndEncode(double[] values, out CompressionDescriptor compressionParameters)
		{
			int offset;
			double[] shrinkedValues = trim(values, out offset);
			double[] compressedData = compress(shrinkedValues, out compressionParameters);
			compressionParameters.Offset = offset;
			compressionParameters.Dimensions = new[] { /*original length:*/ values.Length, /*time steps:*/ 1 };
			return Encode(compressedData);
		}

		public double[] DecodeAndDecompress(string data, CompressionDescriptor compressionParameters)
		{
			if (compressionParameters.Dimensions?.Length != 2 || compressionParameters.Dimensions[0] < 0 || compressionParameters.Dimensions[1] < 1)
				throw new Exception("Unknown dimensions");
			if (compressionParameters.Dimensions[1] > 1)
				throw new NotImplementedException();
			if (compressionParameters.Level != 0)
				throw new NotImplementedException();
			if (compressionParameters.DataType != DataArrayType.Float64)
				throw new NotImplementedException();

			double[] compressedData = Decode<double>(data);
			double[] values = decompress(compressedData, compressionParameters);
			return expand(values, compressionParameters.Dimensions[0], compressionParameters.Offset);
		}

		#endregion

		#region Private methods

		private static double[] compress(double[] dataValues, out CompressionDescriptor compressionParameters)
		{
			compressionParameters = new CompressionDescriptor();

			compressionParameters.Level = 0; // no compression, only copying data to byte array
			compressionParameters.DataType = DataArrayType.Float64;

			return dataValues; // do nothing
		}

		private static double[] decompress(double[] compressedData, CompressionDescriptor compressionParameters)
		{
			return compressedData; // do nothing
		}

		private static byte[] scatterArrayToBytes<T>(T[] values) where T : struct
		{
			// determine the correct type
			Type itemType = typeof(T);
			Debug.Assert(!itemType.IsEnum);
			byte[] bytes;
			if (itemType != typeof(byte))
			{
				bytes = new byte[values.Length * System.Runtime.InteropServices.Marshal.SizeOf(itemType)];
				Buffer.BlockCopy(values, 0, bytes, 0, bytes.Length);
			}
			else
			{
				bytes = (byte[])(object)values; // evade C# array cast limitation
			}
			return bytes;
		}

		private static T[] gatherArrayOfBytes<T>(byte[] bytes) where T : struct
		{
			// determine the correct type
			Type itemType = typeof(T);
			Debug.Assert(!itemType.IsEnum);
			if (itemType != typeof(byte))
			{
				T[] values = new T[bytes.Length / System.Runtime.InteropServices.Marshal.SizeOf(itemType)];
				Buffer.BlockCopy(bytes, 0, values, 0, bytes.Length);
				return values;
			}
			return (T[])(object)bytes; // evade C# array cast limitation
		}

		private static T[] trimEnd<T>(T[] values) where T : struct
		{
			if (values.Length == 0)
				return values;
			int endOffset = values.Length - 1;
			for (int i = values.Length - 2; i >= 0; i--)
			{
				T a = values[values.Length - 1];
				T b = values[i];
				if (!a.Equals(b))
				{
					endOffset = values.Length - i - 2;
					break;
				}
			}
			if (endOffset == 0)
				return values;
			T[] trimmedValues = new T[values.Length - endOffset];
			Array.Copy(values, trimmedValues, trimmedValues.Length);
			return trimmedValues;
		}

		private T[] expandEnd<T>(T[] values, int requestedLength) where T : struct
		{
			if (requestedLength == values.Length)
				return values;
			if (requestedLength < values.Length)
				throw new ArgumentOutOfRangeException(nameof(requestedLength));

			T[] expandedValues = new T[requestedLength];
			if (values.Length == 0)
				return expandedValues;
			Array.Copy(values, expandedValues, values.Length);
			T defaultValue = values[values.Length - 1]; // copy last value to the rest of array
			for (int i = values.Length; i < requestedLength; i++)
			{
				expandedValues[i] = defaultValue;
			}
			return expandedValues;
		}

		private static T[] trim<T>(T[] values, out int offset) where T : struct
		{
			if (values.Length == 0)
			{
				offset = 0;
				return values;
			}
			int beginOffset = values.Length - 1;
			for (int i = 1; i < values.Length; i++)
			{
				if (!values[0].Equals(values[i]))
				{
					beginOffset = i - 1;
					break;
				}
			}
			int endOffset = 0;
			for (int i = values.Length - 2; i >= beginOffset; i--)
			{
				if (!values[values.Length - 1].Equals(values[i]))
				{
					endOffset = values.Length - i - 2;
					break;
				}
			}
			Debug.Assert(beginOffset + endOffset < values.Length);
			if (beginOffset + endOffset == 0)
			{
				offset = 0;
				return values;
			}
			T[] trimmedValues = new T[values.Length - beginOffset - endOffset];
			Array.Copy(values, beginOffset, trimmedValues, 0, trimmedValues.Length);
			offset = beginOffset;
			return trimmedValues;
		}

		private T[] expand<T>(T[] values, int length, int offset) where T : struct
		{
			if (length == values.Length)
				return values;
			if (length < values.Length)
				throw new ArgumentOutOfRangeException(nameof(length));

			T[] expandedValues = new T[length];
			if (values.Length == 0)
				return expandedValues;

			// copy beginning
			T firstValue = values[0];
			for (int i = 0; i < offset; i++)
			{
				expandedValues[i] = firstValue;
			}

			// copy middle - valuable data
			for (int i = 0; i < values.Length; i++)
			{
				expandedValues[offset + i] = values[i];
			}

			// copy end
			T lastValue = values[values.Length - 1];
			for (int i = values.Length + offset; i < length; i++)
			{
				expandedValues[i] = lastValue;
			}

			return expandedValues;
		}

		#endregion
	}
}
