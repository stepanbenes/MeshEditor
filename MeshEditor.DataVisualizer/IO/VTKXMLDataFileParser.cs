using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using MeshEditor.DataVisualizer.Data;
using MeshEditor.IO;
using MeshEditor.Utilities;

namespace MeshEditor.DataVisualizer.IO
{
	public class VTKXMLDataFileParser : IDataFileParser
	{

		#region Static members

		private static int? tryGetOrdinalFromFileName(string filename)
		{
			string extension = Path.GetExtension(Path.GetFileNameWithoutExtension(filename)).TrimStart('.');
			int ordinal;
			if (int.TryParse(extension, out ordinal))
				return ordinal;
			return null;
		}

		#endregion

		#region Fields, constructor

		private readonly string filename;
		private readonly double time;

		private StreamReader streamReader;
		private XmlReader input;
		private int numberOfPoints, numberOfCells;
		private Dictionary<string, DataType.CompoundTypes> dataNameMap;
		private DataInfo currentDataInfo;

		public VTKXMLDataFileParser(string filename, double? time)
		{
			this.filename = filename;
			this.time = time ?? tryGetOrdinalFromFileName(filename) ?? 0;
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
			if (input == null)
			{
				initInput();
			}

			Debug.Assert(dataNameMap != null);

			if (!input.ReadToDescendant("DataArray") && !input.ReadToNextSibling("DataArray"))
			{
				return null; // end of PointData, no DataArray found in this element
			}

			string dataArrayName = null;
			int numberOfComponents = 0;
			while (input.MoveToNextAttribute())
			{
				switch (input.Name.ToLower())
				{
					case "type":
						// ignore type, parse coordinates as Float64 values
						break;
					case "name":
						dataArrayName = input.Value;
						break;
					case "numberofcomponents":
						numberOfComponents = parseInt32(input.Value);
						break;
					case "format":
						if (input.Value.ToLower() != "ascii")
						{
							throw new DataLoadingException($"Ascii data array format was expected instead of '{input.Value}'.", Filename, CurrentLineNumber);
						}
						break;
				}
			}

			input.MoveToElement(); // move attributes back to beginning of the DataArray element

			DataType.CompoundTypes compoundType;
			if (!dataNameMap.TryGetValue(dataArrayName, out compoundType))
			{
				throw new DataLoadingException($"Data array with name '{dataArrayName}' was not found.", Filename, CurrentLineNumber);
			}

			DataType dataType = new DataType(dataArrayName, Filename, 0 /*filePosition*/, compoundType, generateComponentNames(numberOfComponents));
			currentDataInfo = new DataInfo(dataType, Path.GetFileNameWithoutExtension(Filename), time, DataLocation.Nodes);
			return currentDataInfo;
		}

		public IEnumerable<DataValue> ReadResultBlock()
		{
			if (currentDataInfo == null)
			{
				throw new DataLoadingException("Can not read result block. Previous data was not processed entirely.", Filename, CurrentLineNumber);
			}

			double[] values = parseFloat64AsciiDataArray();
			int componentCount = currentDataInfo.DataType.ComponentCount;
			Debug.Assert(values.Length == numberOfPoints * componentCount);
			for (int i = 0; i < numberOfPoints; i++)
			{
				NodeValue nodeValue = new NodeValue(i, Functions.GetSliceOfArray(values, i * componentCount, componentCount));
				yield return nodeValue;
			}
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

			if (!input.MoveToElement()) // move to start of Piece element
			{
				throwElementIsMissing("Piece");
			}

			if (!input.ReadToDescendant("PointData"))
			{
				throwElementIsMissing("PointData");
			}

			dataNameMap = new Dictionary<string, DataType.CompoundTypes>();
			while (input.MoveToNextAttribute())
			{
				switch (input.Name.ToLower())
				{
					case "scalars":
						foreach (string scalarName in input.Value.Split(VTKXMLMeshParser.DataArrayValueDelimiters, StringSplitOptions.RemoveEmptyEntries))
						{
							dataNameMap.Add(scalarName, DataType.CompoundTypes.Scalar);
						}
						break;
					case "vectors":
						foreach (string vectorName in input.Value.Split(VTKXMLMeshParser.DataArrayValueDelimiters, StringSplitOptions.RemoveEmptyEntries))
						{
							dataNameMap.Add(vectorName, DataType.CompoundTypes.Vector);
						}
						break;
					case "tensors":
						foreach (string tensorName in input.Value.Split(VTKXMLMeshParser.DataArrayValueDelimiters, StringSplitOptions.RemoveEmptyEntries))
						{
							dataNameMap.Add(tensorName, DataType.CompoundTypes.Matrix);
						}
						break;
				}
			}

			input.MoveToElement(); // move from attribute back to Piece element
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

		private string[] generateComponentNames(int numberOfComponents)
		{
			const string genericComponentName = "value";
			return Enumerable.Range(1, numberOfComponents).Select(i => genericComponentName + i).ToArray();
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
