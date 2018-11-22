using MeshEditor.Common.Extensions;
using MeshEditor.LayerManager.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.LayerManager.Encoding
{
	internal class Base64EncodingService : IEncodingService
	{
		#region Public methods

		public string Encode<T>(T[] values, TrimOptions trimOptions, out EncodingParameters encodingParameters) where T : struct
		{
			encodingParameters = new EncodingParameters
			{
				OriginalLength = values.Length,
				DataType = convertTypeToDataArrayType(typeof(T)),
			};

			T[] valuesToConvert;
			switch (trimOptions)
			{
				case TrimOptions.None:
					valuesToConvert = values;
					break;
				case TrimOptions.End:
					valuesToConvert = trimEnd(values);
					break;
				case TrimOptions.BeginEnd:
					int offset;
					T? defaultValue;
					valuesToConvert = trim(values, out offset, out defaultValue);
					encodingParameters.Offset = offset;
					encodingParameters.DefaultValue = defaultValue.HasValue ? Convert.ToString(defaultValue.Value, CultureInfo.InvariantCulture) : null;
					break;
				default:
					throw new NotSupportedException();
			}
			encodingParameters.Length = valuesToConvert.Length;
			byte[] bytes = scatterArrayToBytes(valuesToConvert);
			return Convert.ToBase64String(bytes);
		}

		public T[] Decode<T>(string data, TrimOptions trimOptions, EncodingParameters encodingParameters) where T : struct
		{
			Debug.Assert(encodingParameters != null);
			byte[] bytes = Convert.FromBase64String(data);
			T[] values = gatherArrayOfBytes<T>(bytes, encodingParameters.DataType);
			T[] result;
			switch (trimOptions)
			{
				case TrimOptions.None:
					result = values;
					break;
				case TrimOptions.End:
					result = expandEnd(values, encodingParameters.OriginalLength);
					break;
				case TrimOptions.BeginEnd:
					T defaultValue = string.IsNullOrEmpty(encodingParameters.DefaultValue) ? default : (T)Convert.ChangeType(encodingParameters.DefaultValue, typeof(T), CultureInfo.InvariantCulture);
					result = expand(values, encodingParameters.OriginalLength, encodingParameters.Offset, defaultValue);
					break;
				default:
					throw new NotSupportedException();
			}
			return result;
		}

		#endregion

		#region Private methods

		private DataArrayType convertTypeToDataArrayType(Type type)
		{
			if (type == typeof(double))
				return DataArrayType.Float64;
			if (type == typeof(float))
				return DataArrayType.Float32;
			if (type == typeof(int))
				return DataArrayType.Int32;
			if (type == typeof(byte))
				return DataArrayType.UInt8;
			throw new NotSupportedException();
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

		private static T[] gatherArrayOfBytes<T>(byte[] bytes, DataArrayType sourceDataType) where T : struct
		{
			switch (sourceDataType)
			{
				case DataArrayType.Default: // source type is destination type
					{
						Type itemType = typeof(T);
						if (itemType == typeof(byte))
						{
							return (T[])(object)bytes;
						}
						var array = new T[bytes.Length / System.Runtime.InteropServices.Marshal.SizeOf<T>()];
						Buffer.BlockCopy(bytes, 0, array, 0, bytes.Length);
						return array;
					}
				case DataArrayType.Float64:
					{
						var array = new double[bytes.Length / sizeof(double)];
						Buffer.BlockCopy(bytes, 0, array, 0, bytes.Length);
						if (typeof(T) == typeof(double))
						{
							return (T[])(object)array;
						}
						else if (typeof(T) == typeof(float))
						{
							return (T[])(object)Array.ConvertAll(array, value => (float)value);
						}
						else if (typeof(T) == typeof(int))
						{
							return (T[])(object)Array.ConvertAll(array, value => (int)value);
						}
						else if (typeof(T) == typeof(byte))
						{
							return (T[])(object)Array.ConvertAll(array, value => (byte)value);
						}
						throw new NotSupportedException();
					}
				case DataArrayType.Float32:
					{
						var array = new float[bytes.Length / sizeof(float)];
						Buffer.BlockCopy(bytes, 0, array, 0, bytes.Length);
						if (typeof(T) == typeof(double))
						{
							return (T[])(object)Array.ConvertAll(array, value => (double)value);
						}
						else if (typeof(T) == typeof(float))
						{
							return (T[])(object)array;
						}
						else if (typeof(T) == typeof(int))
						{
							return (T[])(object)Array.ConvertAll(array, value => (int)value);
						}
						else if (typeof(T) == typeof(byte))
						{
							return (T[])(object)Array.ConvertAll(array, value => (byte)value);
						}
						throw new NotSupportedException();
					}
				case DataArrayType.Int32:
					{
						var array = new int[bytes.Length / sizeof(int)];
						Buffer.BlockCopy(bytes, 0, array, 0, bytes.Length);
						if (typeof(T) == typeof(double))
						{
							return (T[])(object)Array.ConvertAll(array, value => (double)value);
						}
						else if (typeof(T) == typeof(float))
						{
							return (T[])(object)Array.ConvertAll(array, value => (float)value);
						}
						else if (typeof(T) == typeof(int))
						{
							return (T[])(object)array;
						}
						else if (typeof(T) == typeof(byte))
						{
							return (T[])(object)Array.ConvertAll(array, value => (byte)value);
						}
						throw new NotSupportedException();
					}
				case DataArrayType.UInt8:
					{
						var array = bytes;
						if (typeof(T) == typeof(double))
						{
							return (T[])(object)Array.ConvertAll(array, value => (double)value);
						}
						else if (typeof(T) == typeof(float))
						{
							return (T[])(object)Array.ConvertAll(array, value => (float)value);
						}
						else if (typeof(T) == typeof(int))
						{
							return (T[])(object)Array.ConvertAll(array, value => (int)value);
						}
						else if (typeof(T) == typeof(byte))
						{
							return (T[])(object)array;
						}
						throw new NotSupportedException();
					}
				default:
					throw new NotSupportedException();
			}
		}

		private static T[] trimEnd<T>(T[] values) where T : struct
		{
			if (values.Length == 0)
			{
				return values;
			}
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
			{
				return values;
			}
			T[] trimmedValues = new T[values.Length - endOffset]; // trimmed value is last value in trimmed array
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

		private static T[] trim<T>(T[] values, out int offset, out T? defaultValue) where T : struct
		{
			if (values.Length == 0)
			{
				offset = 0;
				defaultValue = null;
				return values;
			}
			T testValue = values[0];
			int beginOffset = 0;

			for (int index = 1; index < values.Length; index++)
			{
				if (testValue.Equals(values[index]))
				{
					beginOffset = index + 1;
				}
				else
				{
					break;
				}
			}

			if (beginOffset == 0)
			{
				testValue = values[values.Length - 1];
			}

			int endOffset = 0;
			for (int index = values.Length - ((beginOffset == 0) ? 2 : 1); index >= beginOffset; index--)
			{
				if (testValue.Equals(values[index]))
				{
					endOffset = values.Length - index;
				}
				else
				{
					break;
				}
			}

			Debug.Assert(beginOffset + endOffset <= values.Length);
			if (beginOffset + endOffset == 0)
			{
				offset = 0;
				defaultValue = null;
				return values;
			}

			T[] trimmedValues = new T[values.Length - beginOffset - endOffset];
			Array.Copy(values, beginOffset, trimmedValues, 0, trimmedValues.Length);
			offset = beginOffset;
			defaultValue = testValue;
			return trimmedValues;
		}

		private T[] expand<T>(T[] values, int length, int offset, T defaultValue) where T : struct
		{
			if (length == values.Length)
				return values;
			if (length < values.Length)
				throw new ArgumentOutOfRangeException(nameof(length));

			T[] expandedValues = new T[length];
			if (values.Length == 0)
			{
				if (!defaultValue.Equals(default(T)))
				{
					expandedValues.Fill(defaultValue);
				}
				return expandedValues;
			}

			// copy beginning
			for (int i = 0; i < offset; i++)
			{
				expandedValues[i] = defaultValue;
			}

			// copy middle - valuable data
			for (int i = 0; i < values.Length; i++)
			{
				expandedValues[offset + i] = values[i];
			}

			// copy end
			for (int i = values.Length + offset; i < length; i++)
			{
				expandedValues[i] = defaultValue;
			}

			return expandedValues;
		}

		#endregion
	}
}
