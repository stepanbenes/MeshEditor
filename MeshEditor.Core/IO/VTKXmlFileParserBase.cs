using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace MeshEditor.IO
{
	public abstract class VTKXmlFileParserBase : IDisposable
	{

		#region Static members

		public static readonly char[] DataArrayValueDelimiters = new[] { ' ', '\t', '\n', '\r' };

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
					return -1;
				}
				return xmlInfo.LineNumber;
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

		#endregion

		#region Protected methods

		protected void InitInput(out string fileType)
		{
			if (isInputInitialized)
			{
				throw new InvalidOperationException("Input was already initialized.");
			}

			if (!File.Exists(filename))
			{
				throw new MeshLoadingException($"Mesh file can't be found. ({filename})");
			}

			streamReader = new StreamReader(filename);
			input = XmlReader.Create(streamReader);

			ValidateVTKFileType(out fileType);

			isInputInitialized = true;
		}

		protected void ThrowElementIsMissing(string elementName)
		{
			throw new MeshLoadingException($"{elementName} element was not found.");
		}

		protected double[] ParseFloat64AsciiDataArray()
		{
			string content = input.ReadElementContentAsString();
			// TODO: for binary format use: input.ReadElementContentAsBase64(...)
			string[] parts = content.Split(DataArrayValueDelimiters, StringSplitOptions.RemoveEmptyEntries);
			double[] result = new double[parts.Length];
			for (int i = 0; i < parts.Length; i++)
			{
				result[i] = ParseFloat64(parts[i]);
			}
			return result;
		}

		protected float[] ParseFloat32AsciiDataArray()
		{
			string content = input.ReadElementContentAsString();
			string[] parts = content.Split(DataArrayValueDelimiters, StringSplitOptions.RemoveEmptyEntries);
			float[] result = new float[parts.Length];
			for (int i = 0; i < parts.Length; i++)
			{
				result[i] = ParseFloat32(parts[i]);
			}
			return result;
		}

		protected int[] ParseInt32AsciiDataArray()
		{
			string content = input.ReadElementContentAsString();
			string[] parts = content.Split(DataArrayValueDelimiters, StringSplitOptions.RemoveEmptyEntries);
			int[] result = new int[parts.Length];
			for (int i = 0; i < parts.Length; i++)
			{
				result[i] = ParseInt32(parts[i]);
			}
			return result;
		}

		protected byte[] ParseUInt8AsciiDataArray()
		{
			string content = input.ReadElementContentAsString();
			string[] parts = content.Split(DataArrayValueDelimiters, StringSplitOptions.RemoveEmptyEntries);
			byte[] result = new byte[parts.Length];
			for (int i = 0; i < parts.Length; i++)
			{
				result[i] = ParseUInt8(parts[i]);
			}
			return result;
		}

		protected int ParseInt32(string text)
		{
			int result;
			if (!int.TryParse(text, NumberStyles.Integer, CultureProvider.EnglishCulture.NumberFormat, out result))
			{
				throw new MeshLoadingException($"32bit integer expected instead of '{text}'", CurrentLineNumber);
			}
			return result;
		}

		protected byte ParseUInt8(string text)
		{
			byte result;
			if (!byte.TryParse(text, NumberStyles.Integer, CultureProvider.EnglishCulture.NumberFormat, out result))
			{
				throw new MeshLoadingException($"Unsigned 8bit integer expected instead of '{text}'", CurrentLineNumber);
			}
			return result;
		}

		protected double ParseFloat64(string text)
		{
			double result;
			if (!double.TryParse(text, NumberStyles.Float, CultureProvider.EnglishCulture.NumberFormat, out result))
			{
				throw new MeshLoadingException($"Floating-point number expected instead of '{text}'", CurrentLineNumber);
			}
			return result;
		}

		protected float ParseFloat32(string text)
		{
			float result;
			if (!float.TryParse(text, NumberStyles.Float, CultureProvider.EnglishCulture.NumberFormat, out result))
			{
				throw new MeshLoadingException($"Floating-point number expected instead of '{text}'", CurrentLineNumber);
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
