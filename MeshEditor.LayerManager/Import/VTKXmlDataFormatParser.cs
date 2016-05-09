using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using MeshEditor.LayerManager.Data;
using MeshEditor.LayerManager.Storage;

namespace MeshEditor.LayerManager.Import
{
	class VTKXmlDataFormatParser : VTKXmlFormatParserBase, IDataImportService
	{
		IReadStorageService storageService;
		IEnumerable<string> recordNames;

		public VTKXmlDataFormatParser(IReadStorageService storageService, IEnumerable<string> recordNames)
		{
			this.storageService = storageService;
			this.recordNames = recordNames;
		}

		public IEnumerable<FieldDataDescription> ReadData(GeometryDescription ignored)
		{
			foreach (string recordName in recordNames)
			{
				double timeStep = tryGetOrdinalFromFileName(recordName) ?? 0.0;
				using (Stream fileStream = storageService.Load(recordName))
				{
					string fileType;
					using (XmlReader input = InitInput(fileStream, out fileType))
					{
						Debug.Assert(input != null);
						if (fileType?.ToLower() != "unstructuredgrid")
						{
							throw new FormatException($"VTK file type '{fileType}' is not supported. Only 'UnstructuredGrid' type is supported.");
						}

						int numberOfPoints, numberOfCells;
						readToPieceElement(input, out numberOfPoints, out numberOfCells);

						foreach (var dataDescription in parseDataArraysInLocation(input, DataLocationType.Points, timeStep))
						{
							Debug.Assert(dataDescription.Values.Length == numberOfPoints * dataDescription.NumberOfComponents);
							yield return dataDescription;
						}

						foreach (var dataDescription in parseDataArraysInLocation(input, DataLocationType.Cells, timeStep))
						{
							Debug.Assert(dataDescription.Values.Length == numberOfCells * dataDescription.NumberOfComponents);
							yield return dataDescription;
						}
					}
				}
			}

			// TODO: catch all exceptions and convert them to FileParserException
		}

		#region Private methods

		private static IEnumerable<FieldDataDescription> parseDataArraysInLocation(XmlReader input, DataLocationType location, double timeStep)
		{
			Dictionary<string, FieldType> fieldNameTypeMap;
			readToDataElement(input, location, out fieldNameTypeMap);

			while (true)
			{
				if (!input.ReadToDescendant("DataArray") && !input.ReadToNextSibling("DataArray"))
				{
					break; // end of PointData, no DataArray found in this element
				}

				string dataArrayName = null;
				int numberOfComponents = 1; // one component is default in case of missing attribute
				DataArrayFormat? currentDataArrayFormat = null;
				DataArrayType? currentDataArrayType = null;
				while (input.MoveToNextAttribute())
				{
					switch (input.Name.ToLower())
					{
						case "type":
							currentDataArrayType = TryParseDataArrayType(input.Value);
							break;
						case "name":
							dataArrayName = input.Value;
							break;
						case "numberofcomponents":
							numberOfComponents = ParseInt32(input.Value);
							break;
						case "format":
							currentDataArrayFormat = TryParseDataArrayFormat(input.Value);
							break;
					}
				}

				if (!currentDataArrayType.HasValue)
				{
					throw new FormatException("Unknown data type.");
				}

				if (!currentDataArrayFormat.HasValue)
				{
					throw new FormatException("Unknown data format.");
				}

				if (currentDataArrayFormat != DataArrayFormat.Ascii)
				{
					throw new FormatException("Only Ascii data array format is supported.");
				}

				input.MoveToElement(); // move attributes back to beginning of the DataArray element

				FieldType fieldType;
				if (!fieldNameTypeMap.TryGetValue(dataArrayName, out fieldType))
				{
					throw new FormatException($"Data array with name '{dataArrayName}' was not found.");
				}

				// -----------------------

				double[] values = ParseFloat64DataArray(input, currentDataArrayFormat.Value, currentDataArrayType.Value);

				FieldDataDescription dataDescription = new FieldDataDescription
				{
					FieldName = dataArrayName,
					TimeStep = timeStep,
					ComponentNames = new string[numberOfComponents],
					FieldType = fieldType,
					Location = location,
					Values = values
				};

				yield return dataDescription;
			}
		}

		private static int? tryGetOrdinalFromFileName(string filename)
		{
			string extension = Path.GetExtension(Path.GetFileNameWithoutExtension(filename)).TrimStart('.');
			int ordinal;
			if (int.TryParse(extension, out ordinal))
				return ordinal;
			return null;
		}

		private static void readToPieceElement(XmlReader input, out int numberOfPoints, out int numberOfCells)
		{
			if (!input.ReadToDescendant("UnstructuredGrid"))
			{
				ThrowElementIsMissing("UnstructuredGrid");
			}

			if (!input.ReadToDescendant("Piece"))
			{
				ThrowElementIsMissing("Piece");
			}

			numberOfPoints = numberOfCells = 0;

			while (input.MoveToNextAttribute())
			{
				switch (input.Name.ToLower())
				{
					case "numberofpoints":
						numberOfPoints = ParseInt32(input.Value);
						break;
					case "numberofcells":
						numberOfCells = ParseInt32(input.Value);
						break;
				}
			}

			if (!input.MoveToElement()) // move to start of Piece element
			{
				ThrowElementIsMissing("Piece");
			}
		}

		private static void readToDataElement(XmlReader input, DataLocationType location, out Dictionary<string, FieldType> fieldNameTypeMap)
		{
			string elementName;
			switch (location)
			{
				case DataLocationType.Points:
					elementName = "PointData";
					break;
				case DataLocationType.Cells:
					elementName = "CellData";
					break;
				case DataLocationType.CellPoints:
				default:
					throw new NotSupportedException();
			}

			if (!input.ReadToFollowing(elementName))
			{
				ThrowElementIsMissing(elementName);
			}

			fieldNameTypeMap = new Dictionary<string, FieldType>();

			while (input.MoveToNextAttribute())
			{
				switch (input.Name.ToLower())
				{
					case "scalars":
						foreach (string scalarName in input.Value.Split(DataArrayValueDelimiters, StringSplitOptions.RemoveEmptyEntries))
						{
							fieldNameTypeMap.Add(scalarName, FieldType.Scalar);
						}
						break;
					case "vectors":
						foreach (string vectorName in input.Value.Split(DataArrayValueDelimiters, StringSplitOptions.RemoveEmptyEntries))
						{
							fieldNameTypeMap.Add(vectorName, FieldType.Vector);
						}
						break;
					case "tensors":
						foreach (string tensorName in input.Value.Split(DataArrayValueDelimiters, StringSplitOptions.RemoveEmptyEntries))
						{
							fieldNameTypeMap.Add(tensorName, FieldType.Tensor);
						}
						break;
				}
			}

			input.MoveToElement(); // move from attributes back to element
		}

		#endregion

	}
}
