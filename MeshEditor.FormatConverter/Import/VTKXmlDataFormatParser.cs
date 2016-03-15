using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using MeshEditor.LayerManager.Import;
using MeshEditor.LayerManager.Storage;

namespace MeshEditor.FormatConverter.Import
{
	class VTKXmlDataFormatParser : VTKXmlFormatParserBase, IDataImportService
	{
		IStorageService storageService;
		IEnumerable<Uri> uris;

		public VTKXmlDataFormatParser(IStorageService storageService, IEnumerable<Uri> uris)
		{
			this.storageService = storageService;
			this.uris = uris;
		}

		public IEnumerable<DataDescription> ReadData(GeometryDescription ignored)
		{
			foreach (Uri uri in uris)
			{
				string fileType;
				using (Stream fileStream = storageService.Load(uri))
				using (XmlReader input = InitInput(fileStream, out fileType))
				{
					Debug.Assert(input != null);
					if (fileType?.ToLower() != "unstructuredgrid")
					{
						throw new FormatException($"VTK file type '{fileType}' is not supported. Only 'UnstructuredGrid' type is supported.");
					}

					int numberOfPoints, numberOfCells;
					Dictionary<string, FieldType> fieldNameTypeMap;
					readToPointDataElement(input, out numberOfPoints, out numberOfCells, out fieldNameTypeMap);

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

						Debug.Assert(values.Length == numberOfPoints * numberOfComponents);
						
						DataDescription dataDescription = new DataDescription
						{
							Name = dataArrayName,
							TimeStep = tryGetOrdinalFromFileName(uri.LocalPath),
							NumberOfComponents = numberOfComponents,
							ComponentNames = null, // or new string[NumberOfDataComponents]
							FieldType = fieldType,
							LocationType = DataLocationType.Points, /**/
							Data = values
						};

						yield return dataDescription;
					}
				}
			}

			// TODO: catch all exceptions and convert them to FileParserException
		}

		#region Private methods

		private static int? tryGetOrdinalFromFileName(string filename)
		{
			string extension = Path.GetExtension(Path.GetFileNameWithoutExtension(filename)).TrimStart('.');
			int ordinal;
			if (int.TryParse(extension, out ordinal))
				return ordinal;
			return null;
		}

		private static void readToPointDataElement(XmlReader input, out int numberOfPoints, out int numberOfCells, out Dictionary<string, FieldType> fieldNameTypeMap)
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

			if (!input.ReadToDescendant("PointData"))
			{
				ThrowElementIsMissing("PointData");
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

			input.MoveToElement(); // move from attribute back to Piece element
		}

		#endregion

	}
}
