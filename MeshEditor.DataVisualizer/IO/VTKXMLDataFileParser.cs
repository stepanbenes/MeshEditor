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
	public class VTKXmlDataFileParser : VTKXmlFileParserBase, IDataFileParser
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

		private readonly double time;

		private int numberOfPoints, numberOfCells;
		private Dictionary<string, DataType.CompoundTypes> dataNameMap;
		private DataInfo currentDataInfo;

		public VTKXmlDataFileParser(string filename, double? time)
			: base(filename)
		{
			this.time = time ?? tryGetOrdinalFromFileName(filename) ?? 0;
		}

		#endregion

		#region IDataFileParser members

		public DataInfo ReadNextResult()
		{
			EnsureInputIsInitialized();

			if (!Input.ReadToDescendant("DataArray") && !Input.ReadToNextSibling("DataArray"))
			{
				return null; // end of PointData, no DataArray found in this element
			}

			string dataArrayName = null;
			int numberOfComponents = 0;
			while (Input.MoveToNextAttribute())
			{
				switch (Input.Name.ToLower())
				{
					case "type":
						// ignore type, parse coordinates as Float64 values
						break;
					case "name":
						dataArrayName = Input.Value;
						break;
					case "numberofcomponents":
						numberOfComponents = ParseInt32(Input.Value);
						break;
					case "format":
						if (Input.Value.ToLower() != "ascii")
						{
							throw new DataLoadingException($"Ascii data array format was expected instead of '{Input.Value}'.", Filename, CurrentLineNumber);
						}
						break;
				}
			}

			Input.MoveToElement(); // move attributes back to beginning of the DataArray element

			DataType.CompoundTypes compoundType;
			if (!dataNameMap.TryGetValue(dataArrayName, out compoundType))
			{
				throw new DataLoadingException($"Data array with name '{dataArrayName}' was not found.", Filename, CurrentLineNumber);
			}

			DataType dataType = new DataType(dataArrayName, Filename, 0 /*filePosition*/, compoundType, GenerateComponentNames(numberOfComponents));
			currentDataInfo = new DataInfo(dataType, Path.GetFileNameWithoutExtension(Filename), time, DataLocation.Nodes);
			return currentDataInfo;
		}

		public IEnumerable<DataValue> ReadResultBlock()
		{
			if (currentDataInfo == null)
			{
				throw new DataLoadingException("Can not read result block. Previous data was not processed entirely.", Filename, CurrentLineNumber);
			}

			double[] values = ParseFloat64AsciiDataArray();
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

		private void EnsureInputIsInitialized()
		{
			if (!IsInputInitialized)
			{
				string fileType;
				InitInput(out fileType);
				if (fileType?.ToLower() != "unstructuredgrid")
				{
					throw new MeshLoadingException($"VTK file type '{fileType}' is not supported. Only 'UnstructuredGrid' type is supported.", CurrentLineNumber);
				}
				ReadToUnstructuredGridElement();
			}
			Debug.Assert(IsInputInitialized);
			Debug.Assert(Input != null);
			Debug.Assert(dataNameMap != null);
		}

		private void ReadToUnstructuredGridElement()
		{
			if (!Input.ReadToDescendant("UnstructuredGrid"))
			{
				ThrowElementIsMissing("UnstructuredGrid");
			}

			if (!Input.ReadToDescendant("Piece"))
			{
				ThrowElementIsMissing("Piece");
			}

			while (Input.MoveToNextAttribute())
			{
				switch (Input.Name.ToLower())
				{
					case "numberofpoints":
						numberOfPoints = ParseInt32(Input.Value);
						break;
					case "numberofcells":
						numberOfCells = ParseInt32(Input.Value);
						break;
				}
			}

			if (!Input.MoveToElement()) // move to start of Piece element
			{
				ThrowElementIsMissing("Piece");
			}

			if (!Input.ReadToDescendant("PointData"))
			{
				ThrowElementIsMissing("PointData");
			}

			dataNameMap = new Dictionary<string, DataType.CompoundTypes>();

			while (Input.MoveToNextAttribute())
			{
				switch (Input.Name.ToLower())
				{
					case "scalars":
						foreach (string scalarName in Input.Value.Split(VTKXmlMeshParser.DataArrayValueDelimiters, StringSplitOptions.RemoveEmptyEntries))
						{
							dataNameMap.Add(scalarName, DataType.CompoundTypes.Scalar);
						}
						break;
					case "vectors":
						foreach (string vectorName in Input.Value.Split(VTKXmlMeshParser.DataArrayValueDelimiters, StringSplitOptions.RemoveEmptyEntries))
						{
							dataNameMap.Add(vectorName, DataType.CompoundTypes.Vector);
						}
						break;
					case "tensors":
						foreach (string tensorName in Input.Value.Split(VTKXmlMeshParser.DataArrayValueDelimiters, StringSplitOptions.RemoveEmptyEntries))
						{
							dataNameMap.Add(tensorName, DataType.CompoundTypes.Matrix);
						}
						break;
				}
			}

			Input.MoveToElement(); // move from attribute back to Piece element
		}

		private string[] GenerateComponentNames(int numberOfComponents)
		{
			const string genericComponentName = "value";
			return Enumerable.Range(1, numberOfComponents).Select(i => genericComponentName + i).ToArray();
		}

		#endregion

	}
}
