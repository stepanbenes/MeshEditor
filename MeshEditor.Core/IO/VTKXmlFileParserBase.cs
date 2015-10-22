using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using MeshEditor.Utilities;

namespace MeshEditor.IO
{
	public abstract class VTKXmlFileParserBase : IDisposable
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

		#region Fields, constructor

		private readonly string filename;

		private bool isInputInitialized;

		private StreamReader streamReader;
		private XmlReader input;

		public VTKXmlFileParserBase(string filename)
		{
			this.filename = filename;
		}

		#endregion

		#region Properties

		public string Filename => filename;

		public int CurrentLineNumber
		{
			get
			{
				IXmlLineInfo xmlInfo = input as IXmlLineInfo;
				if (xmlInfo == null)
				{
					return 0;
				}
				return xmlInfo.LineNumber;
			}
		}

		public int CurrentLinePosition
		{
			get
			{
				IXmlLineInfo xmlInfo = input as IXmlLineInfo;
				if (xmlInfo == null)
				{
					return 0;
				}
				return xmlInfo.LinePosition;
			}
		}

		public double PercentageRead
		{
			get
			{
				// NOTE: this is too coarse measure, underlying parser is ignored
				return ((double)streamReader.BaseStream.Position / (double)streamReader.BaseStream.Length) * 100.0;
			}
		}

		protected XmlReader Input => input;

		protected bool IsInputInitialized => isInputInitialized;

		#endregion

		#region Private methods

		private void ValidateVTKFileType(out string fileType)
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

		private double[] ConvertAsciiDataArrayToFloat64Array(string[] data)
		{
			Debug.Assert(data != null);
			double[] result = new double[data.Length];
			for (int i = 0; i < data.Length; i++)
			{
				result[i] = ParseFloat64(data[i]);
			}
			return result;
		}

		private float[] ConvertAsciiDataArrayToFloat32Array(string[] data)
		{
			Debug.Assert(data != null);
			float[] result = new float[data.Length];
			for (int i = 0; i < data.Length; i++)
			{
				result[i] = ParseFloat32(data[i]);
			}
			return result;
		}

		private int[] ConvertAsciiDataArrayToInt32Array(string[] data)
		{
			Debug.Assert(data != null);
			int[] result = new int[data.Length];
			for (int i = 0; i < data.Length; i++)
			{
				result[i] = ParseInt32(data[i]);
			}
			return result;
		}

		private byte[] ConvertAsciiDataArrayToUInt8Array(string[] data)
		{
			Debug.Assert(data != null);
			byte[] result = new byte[data.Length];
			for (int i = 0; i < data.Length; i++)
			{
				result[i] = ParseUInt8(data[i]);
			}
			return result;
		}

		private double[] ConvertByteArrayToFloat64Array(byte[] bytes)
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

		private float[] ConvertByteArrayToFloat32Array(byte[] bytes)
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

		private int[] ConvertByteArrayToInt32Array(byte[] bytes)
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

		protected void InitInput(out string fileType)
		{
			if (isInputInitialized)
			{
				throw new FileParserException("Input was already initialized.", Filename, CurrentLineNumber, CurrentLinePosition);
			}

			if (!File.Exists(filename))
			{
				throw new FileParserException($"Mesh file can't be found. ({filename})", Filename, CurrentLineNumber, CurrentLinePosition);
			}

			streamReader = new StreamReader(filename);
			input = XmlReader.Create(streamReader);

			ValidateVTKFileType(out fileType);

			isInputInitialized = true;
		}

		protected void ThrowElementIsMissing(string elementName)
		{
			throw new FileParserException($"{elementName} element was not found.", Filename, CurrentLineNumber, CurrentLinePosition);
		}

		protected double[] ParseFloat64DataArray(DataArrayFormat format, DataArrayType actualType)
		{
			string content = input.ReadElementContentAsString();
			switch (format)
			{
				case DataArrayFormat.Ascii:
					{
						string[] data = content.Split(DataArrayValueDelimiters, StringSplitOptions.RemoveEmptyEntries);
						return ConvertAsciiDataArrayToFloat64Array(data);
					}
				//case DataArrayFormat.Binary: // In front of every binary blob, base64 or raw-binary, appended or not, there is an UInt32 length indicator. see: http://mathema.tician.de/what-they-dont-tell-you-about-vtk-xml-binary-formats/
				//	{
				//		byte[] data = Convert.FromBase64String(content);						
				//		return ConvertByteArrayToFloat64Array(data);
				//	}
				default:
					throw new FileParserException($"{format.ToString()} data format is not supported.", Filename, CurrentLineNumber, CurrentLinePosition);
			}
		}

		protected float[] ParseFloat32DataArray(DataArrayFormat format, DataArrayType actualType)
		{
			string content = input.ReadElementContentAsString();
			switch (format)
			{
				case DataArrayFormat.Ascii:
					{
						string[] data = content.Split(DataArrayValueDelimiters, StringSplitOptions.RemoveEmptyEntries);
						return ConvertAsciiDataArrayToFloat32Array(data);
					}
				//case DataArrayFormat.Binary: // In front of every binary blob, base64 or raw-binary, appended or not, there is an UInt32 length indicator. see: http://mathema.tician.de/what-they-dont-tell-you-about-vtk-xml-binary-formats/
				//	{
				//		byte[] data = Convert.FromBase64String(content);
				//		return ConvertByteArrayToFloat32Array(data);
				//	}
				default:
					throw new FileParserException($"{format.ToString()} data format is not supported.", Filename, CurrentLineNumber, CurrentLinePosition);
			}
		}

		protected int[] ParseInt32DataArray(DataArrayFormat format, DataArrayType actualType)
		{
			string content = input.ReadElementContentAsString();
			switch (format)
			{
				case DataArrayFormat.Ascii:
					{
						string[] data = content.Split(DataArrayValueDelimiters, StringSplitOptions.RemoveEmptyEntries);
						return ConvertAsciiDataArrayToInt32Array(data);
					}
				//case DataArrayFormat.Binary: // In front of every binary blob, base64 or raw-binary, appended or not, there is an UInt32 length indicator. see: http://mathema.tician.de/what-they-dont-tell-you-about-vtk-xml-binary-formats/
				//	{
				//		byte[] data = Convert.FromBase64String(content);
				//		return ConvertByteArrayToInt32Array(data);
				//	}
				default:
					throw new FileParserException($"{format.ToString()} data format is not supported.", Filename, CurrentLineNumber, CurrentLinePosition);
			}
		}

		protected byte[] ParseUInt8DataArray(DataArrayFormat format, DataArrayType actualType)
		{
			string content = input.ReadElementContentAsString();
			switch (format)
			{
				case DataArrayFormat.Ascii:
					{
						string[] data = content.Split(DataArrayValueDelimiters, StringSplitOptions.RemoveEmptyEntries);
						return ConvertAsciiDataArrayToUInt8Array(data);
					}
				//case DataArrayFormat.Binary: // In front of every binary blob, base64 or raw-binary, appended or not, there is an UInt32 length indicator. see: http://mathema.tician.de/what-they-dont-tell-you-about-vtk-xml-binary-formats/
				//	{
				//		return Convert.FromBase64String(content);
				//	}
				default:
					throw new FileParserException($"{format.ToString()} data format is not supported.", Filename, CurrentLineNumber, CurrentLinePosition);
			}
		}

		protected int ParseInt32(string text)
		{
			int result;
			if (!int.TryParse(text, NumberStyles.Integer, CultureProvider.EnglishCulture.NumberFormat, out result))
			{
				throw new FileParserException($"32bit integer expected instead of '{text}'", Filename, CurrentLineNumber, CurrentLinePosition);
			}
			return result;
		}

		protected byte ParseUInt8(string text)
		{
			byte result;
			if (!byte.TryParse(text, NumberStyles.Integer, CultureProvider.EnglishCulture.NumberFormat, out result))
			{
				throw new FileParserException($"Unsigned 8bit integer expected instead of '{text}'", Filename, CurrentLineNumber, CurrentLinePosition);
			}
			return result;
		}

		protected double ParseFloat64(string text)
		{
			double result;
			if (!double.TryParse(text, NumberStyles.Float, CultureProvider.EnglishCulture.NumberFormat, out result))
			{
				throw new FileParserException($"Floating-point number expected instead of '{text}'", Filename, CurrentLineNumber, CurrentLinePosition);
			}
			return result;
		}

		protected float ParseFloat32(string text)
		{
			float result;
			if (!float.TryParse(text, NumberStyles.Float, CultureProvider.EnglishCulture.NumberFormat, out result))
			{
				throw new FileParserException($"Floating-point number expected instead of '{text}'", Filename, CurrentLineNumber, CurrentLinePosition);
			}
			return result;
		}

		#endregion

		#region IDisposable Support

		public void Dispose()
		{
			if (streamReader != null)
			{
				streamReader.Dispose();
				streamReader = null;
			}
			if (input != null)
			{
				((IDisposable)input).Dispose();
				input = null;
			}
		}

		#endregion

	}
}
