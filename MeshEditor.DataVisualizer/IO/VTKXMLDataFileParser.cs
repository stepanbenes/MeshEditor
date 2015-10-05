using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using MeshEditor.DataVisualizer.Data;
using MeshEditor.IO;

namespace MeshEditor.DataVisualizer.IO
{
	public class VTKXMLDataFileParser : IDataFileParser
	{

		#region Static members

		#endregion

		#region Fields, constructor

		private string filename;
		private StreamReader streamReader;
		private XmlReader input;
		private int numberOfPoints, numberOfCells;

		public VTKXMLDataFileParser(string filename)
		{
			this.filename = filename;
			input = null;
		}

		#endregion

		#region IDataFileParser members

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
				return ((double)streamReader.BaseStream.Position / (double)streamReader.BaseStream.Length) * 100.0;
			}
		}

		public DataInfo ReadNextResult()
		{
			throw new NotImplementedException();
		}

		public IEnumerable<DataValue> ReadResultBlock()
		{
			throw new NotImplementedException();
		}

		#endregion

		#region Private methods

		private void initInput()
		{
			if (!File.Exists(filename))
			{
				throw new DataLoadingException($"Data file can't be found. ({filename})");
			}

			streamReader = new StreamReader(filename);
			input = XmlReader.Create(streamReader);

			validateVTKFileType();

			if (!input.ReadToDescendant("UnstructuredGrid"))
			{
				throwElementIsMissing("UnstructuredGrid");
			}

			if (!input.ReadToDescendant("Piece"))
			{
				throwElementIsMissing("Piece");
			}

			while (input.MoveToNextAttribute())
			{
				switch (input.Name.ToLower())
				{
					case "numberofpoints":
						numberOfPoints = parseInt32(input.Value);
						break;
					case "numberofcells":
						numberOfCells = parseInt32(input.Value);
						break;
				}
			}

			if (!input.MoveToElement())
			{
				throwElementIsMissing("Piece");
			}
		}

		private void validateVTKFileType()
		{
			if (!input.ReadToDescendant("VTKFile"))
			{
				throwElementIsMissing("VTKFile");
			}

			while (input.MoveToNextAttribute())
			{
				switch (input.Name.ToLower())
				{
					case "type":
						if (input.Value.ToLower() != "unstructuredgrid")
						{
							throw new DataLoadingException($"Type '{input.Value}' is not supported. Only 'UnstructuredGrid' is supported.", Filename, CurrentLineNumber);
						}
						break;
				}
			}

			if (!input.MoveToElement())
			{
				throwElementIsMissing("VTKFile");
			}
		}

		private void throwElementIsMissing(string elementName)
		{
			throw new DataLoadingException($"{elementName} element was not found.", Filename, CurrentLineNumber);
		}

		private double[] parseFloat64AsciiDataArray()
		{
			string content = input.ReadElementContentAsString();
			// TODO: for binary format use: input.ReadElementContentAsBase64(...)
			string[] parts = content.Split(VTKXMLMeshParser.DataArrayValueDelimiters, StringSplitOptions.RemoveEmptyEntries);
			double[] result = new double[parts.Length];
			for (int i = 0; i < parts.Length; i++)
			{
				result[i] = parseFloat64(parts[i]);
			}
			return result;
		}

		private float[] parseFloat32AsciiDataArray()
		{
			string content = input.ReadElementContentAsString();
			string[] parts = content.Split(VTKXMLMeshParser.DataArrayValueDelimiters, StringSplitOptions.RemoveEmptyEntries);
			float[] result = new float[parts.Length];
			for (int i = 0; i < parts.Length; i++)
			{
				result[i] = parseFloat32(parts[i]);
			}
			return result;
		}

		private int[] parseInt32AsciiDataArray()
		{
			string content = input.ReadElementContentAsString();
			string[] parts = content.Split(VTKXMLMeshParser.DataArrayValueDelimiters, StringSplitOptions.RemoveEmptyEntries);
			int[] result = new int[parts.Length];
			for (int i = 0; i < parts.Length; i++)
			{
				result[i] = parseInt32(parts[i]);
			}
			return result;
		}

		private byte[] parseUInt8AsciiDataArray()
		{
			string content = input.ReadElementContentAsString();
			string[] parts = content.Split(VTKXMLMeshParser.DataArrayValueDelimiters, StringSplitOptions.RemoveEmptyEntries);
			byte[] result = new byte[parts.Length];
			for (int i = 0; i < parts.Length; i++)
			{
				result[i] = parseUInt8(parts[i]);
			}
			return result;
		}

		private int parseInt32(string text)
		{
			int result;
			if (!int.TryParse(text, NumberStyles.Integer, CultureProvider.EnglishCulture.NumberFormat, out result))
			{
				throw new DataLoadingException($"32bit integer expected instead of '{text}'", Filename, CurrentLineNumber);
			}
			return result;
		}

		private byte parseUInt8(string text)
		{
			byte result;
			if (!byte.TryParse(text, NumberStyles.Integer, CultureProvider.EnglishCulture.NumberFormat, out result))
			{
				throw new DataLoadingException($"Unsigned 8bit integer expected instead of '{text}'", Filename, CurrentLineNumber);
			}
			return result;
		}

		private double parseFloat64(string text)
		{
			double result;
			if (!double.TryParse(text, NumberStyles.Float, CultureProvider.EnglishCulture.NumberFormat, out result))
			{
				throw new DataLoadingException($"Floating-point number expected instead of '{text}'", Filename, CurrentLineNumber);
			}
			return result;
		}

		private float parseFloat32(string text)
		{
			float result;
			if (!float.TryParse(text, NumberStyles.Float, CultureProvider.EnglishCulture.NumberFormat, out result))
			{
				throw new DataLoadingException($"Floating-point number expected instead of '{text}'", Filename, CurrentLineNumber);
			}
			return result;
		}

		#endregion

		#region IDisposable Support

		// This code added to correctly implement the disposable pattern.
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
