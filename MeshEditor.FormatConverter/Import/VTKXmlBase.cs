using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using MeshEditor.IO;

namespace MeshEditor.FormatConverter.Import
{
	abstract class VTKXmlBase
	{
		protected enum DataArrayFormat
		{
			Ascii,
			Binary
		}

		protected enum DataArrayType
		{
			Float64,
			Float32,
			Int32,
			UInt8
		}

		#region Static members

		public static readonly char[] DataArrayValueDelimiters = new[] { ' ', '\t', '\n', '\r' };

		protected static DataArrayFormat? TryParseDataArrayFormat(string text)
		{
			switch (text?.ToLower())
			{
				case "ascii":
					return DataArrayFormat.Ascii;
				case "binary":
					return DataArrayFormat.Binary;
				default:
					return null;
			}
		}

		protected static DataArrayType? TryParseDataArrayType(string text)
		{
			switch (text?.ToLower())
			{
				case "float64":
					return DataArrayType.Float64;
				case "float32":
					return DataArrayType.Float32;
				case "int32":
					return DataArrayType.Int32;
				case "uint8":
					return DataArrayType.UInt8;
				default:
					return null;
			}
		}

		#endregion

		#region Private methods

		protected static void ThrowElementIsMissing(string elementName)
		{
			throw new FormatException($"{elementName} element is missing.");
		}

		private static void validateVTKFileType(XmlReader input, out string fileType)
		{
			fileType = null;

			if (!input.ReadToDescendant("VTKFile"))
			{
				ThrowElementIsMissing("VTKFile");
			}

			try
			{
				while (input.MoveToNextAttribute())
				{
					if (input.Name.ToLower() == "type")
					{
						fileType = input.Value;
					}
				}
			}
			finally
			{
				if (!input.MoveToElement())
				{
					ThrowElementIsMissing("VTKFile");
				}
			}
		}

		private static double[] convertAsciiDataArrayToFloat64Array(string[] data)
		{
			Debug.Assert(data != null);
			double[] result = new double[data.Length];
			for (int i = 0; i < data.Length; i++)
			{
				result[i] = ParseFloat64(data[i]);
			}
			return result;
		}

		private static float[] convertAsciiDataArrayToFloat32Array(string[] data)
		{
			Debug.Assert(data != null);
			float[] result = new float[data.Length];
			for (int i = 0; i < data.Length; i++)
			{
				result[i] = ParseFloat32(data[i]);
			}
			return result;
		}

		private static int[] convertAsciiDataArrayToInt32Array(string[] data)
		{
			Debug.Assert(data != null);
			int[] result = new int[data.Length];
			for (int i = 0; i < data.Length; i++)
			{
				result[i] = ParseInt32(data[i]);
			}
			return result;
		}

		private static byte[] convertAsciiDataArrayToUInt8Array(string[] data)
		{
			Debug.Assert(data != null);
			byte[] result = new byte[data.Length];
			for (int i = 0; i < data.Length; i++)
			{
				result[i] = ParseUInt8(data[i]);
			}
			return result;
		}

		private static double[] convertByteArrayToFloat64Array(byte[] bytes)
		{
			Debug.Assert(bytes != null);
			const int doubleByteCount = 8;
			Debug.Assert(bytes.Length % doubleByteCount == 0);
			double[] values = new double[bytes.Length / doubleByteCount];
			for (int i = 0; i < values.Length; i++)
			{
				values[i] = BitConverter.ToDouble(bytes, i * doubleByteCount);
			}
			return values;
		}

		private static float[] convertByteArrayToFloat32Array(byte[] bytes)
		{
			Debug.Assert(bytes != null);
			const int singleByteCount = 4;
			Debug.Assert(bytes.Length % singleByteCount == 0);
			float[] values = new float[bytes.Length / singleByteCount];
			for (int i = 0; i < values.Length; i++)
			{
				values[i] = BitConverter.ToSingle(bytes, i * singleByteCount);
			}
			return values;
		}

		private static int[] convertByteArrayToInt32Array(byte[] bytes)
		{
			Debug.Assert(bytes != null);
			const int int32ByteCount = 4;
			Debug.Assert(bytes.Length % int32ByteCount == 0);
			int[] values = new int[bytes.Length / int32ByteCount];
			for (int i = 0; i < values.Length; i++)
			{
				values[i] = BitConverter.ToInt32(bytes, i * int32ByteCount);
			}
			return values;
		}

		#endregion

		#region Protected methods

		protected static XmlReader InitInput(Stream inputStream, out string fileType)
		{
			var xmlReader = XmlReader.Create(inputStream);
			validateVTKFileType(xmlReader, out fileType);
			return xmlReader;
		}

		protected static double[] ParseFloat64DataArray(XmlReader input, DataArrayFormat format, DataArrayType actualType)
		{
			string content = input.ReadElementContentAsString();
			switch (format)
			{
				case DataArrayFormat.Ascii:
					{
						string[] data = content.Split(DataArrayValueDelimiters, StringSplitOptions.RemoveEmptyEntries);
						return convertAsciiDataArrayToFloat64Array(data);
					}
				//case DataArrayFormat.Binary: // In front of every binary blob, base64 or raw-binary, appended or not, there is an UInt32 length indicator. see: http://mathema.tician.de/what-they-dont-tell-you-about-vtk-xml-binary-formats/
				//	{
				//		byte[] data = Convert.FromBase64String(content);						
				//		return ConvertByteArrayToFloat64Array(data);
				//	}
				default:
					throw new NotSupportedException($"{format.ToString()} data format is not supported.");
			}
		}

		protected static float[] ParseFloat32DataArray(XmlReader input, DataArrayFormat format, DataArrayType actualType)
		{
			string content = input.ReadElementContentAsString();
			switch (format)
			{
				case DataArrayFormat.Ascii:
					{
						string[] data = content.Split(DataArrayValueDelimiters, StringSplitOptions.RemoveEmptyEntries);
						return convertAsciiDataArrayToFloat32Array(data);
					}
				//case DataArrayFormat.Binary: // In front of every binary blob, base64 or raw-binary, appended or not, there is an UInt32 length indicator. see: http://mathema.tician.de/what-they-dont-tell-you-about-vtk-xml-binary-formats/
				//	{
				//		byte[] data = Convert.FromBase64String(content);
				//		return ConvertByteArrayToFloat32Array(data);
				//	}
				default:
					throw new NotSupportedException($"{format.ToString()} data format is not supported.");
			}
		}

		protected static int[] ParseInt32DataArray(XmlReader input, DataArrayFormat format, DataArrayType actualType)
		{
			string content = input.ReadElementContentAsString();
			switch (format)
			{
				case DataArrayFormat.Ascii:
					{
						string[] data = content.Split(DataArrayValueDelimiters, StringSplitOptions.RemoveEmptyEntries);
						return convertAsciiDataArrayToInt32Array(data);
					}
				//case DataArrayFormat.Binary: // In front of every binary blob, base64 or raw-binary, appended or not, there is an UInt32 length indicator. see: http://mathema.tician.de/what-they-dont-tell-you-about-vtk-xml-binary-formats/
				//	{
				//		byte[] data = Convert.FromBase64String(content);
				//		return ConvertByteArrayToInt32Array(data);
				//	}
				default:
					throw new NotSupportedException($"{format.ToString()} data format is not supported.");
			}
		}

		protected static byte[] ParseUInt8DataArray(XmlReader input, DataArrayFormat format, DataArrayType actualType)
		{
			string content = input.ReadElementContentAsString();
			switch (format)
			{
				case DataArrayFormat.Ascii:
					{
						string[] data = content.Split(DataArrayValueDelimiters, StringSplitOptions.RemoveEmptyEntries);
						return convertAsciiDataArrayToUInt8Array(data);
					}
				//case DataArrayFormat.Binary: // In front of every binary blob, base64 or raw-binary, appended or not, there is an UInt32 length indicator. see: http://mathema.tician.de/what-they-dont-tell-you-about-vtk-xml-binary-formats/
				//	{
				//		return Convert.FromBase64String(content);
				//	}
				default:
					throw new NotSupportedException($"{format.ToString()} data format is not supported.");
			}
		}

		protected static int ParseInt32(string text)
		{
			int result;
			if (!int.TryParse(text, NumberStyles.Integer, CultureProvider.EnglishCulture.NumberFormat, out result))
			{
				throw new FormatException($"32bit integer expected instead of '{text}'");
			}
			return result;
		}

		protected static byte ParseUInt8(string text)
		{
			byte result;
			if (!byte.TryParse(text, NumberStyles.Integer, CultureProvider.EnglishCulture.NumberFormat, out result))
			{
				throw new FormatException($"Unsigned 8bit integer expected instead of '{text}'");
			}
			return result;
		}

		protected static double ParseFloat64(string text)
		{
			double result;
			if (!double.TryParse(text, NumberStyles.Float, CultureProvider.EnglishCulture.NumberFormat, out result))
			{
				throw new FormatException($"Floating-point number expected instead of '{text}'");
			}
			return result;
		}

		protected static float ParseFloat32(string text)
		{
			float result;
			if (!float.TryParse(text, NumberStyles.Float, CultureProvider.EnglishCulture.NumberFormat, out result))
			{
				throw new FormatException($"Floating-point number expected instead of '{text}'");
			}
			return result;
		}

		#endregion

	}
}
