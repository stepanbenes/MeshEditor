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
	class VTKXmlGeometryImportService : VTKXmlImportServiceBase, IGeometryImportService
	{
		IStorageService storageService;
		string filename;

		public VTKXmlGeometryImportService(IStorageService storageService, string filename)
		{
			this.storageService = storageService;
			this.filename = filename;
		}

		public GeometryDescription ReadGeometry()
		{
			string fileType;
			using (Stream fileStream = storageService.Load(filename))
			using (XmlReader input = InitInput(fileStream, out fileType))
			{
				Debug.Assert(input != null);
				if (fileType?.ToLower() != "unstructuredgrid")
				{
					throw new FormatException($"VTK file type '{fileType}' is not supported. Only 'UnstructuredGrid' type is supported.");
				}

				int numberOfPoints, numberOfCells;

				readToPieceElement(input, out numberOfPoints, out numberOfCells);

				// ---------------------

				if (!input.ReadToFollowing("Points"))
				{
					ThrowElementIsMissing("Points");
				}

				if (!input.ReadToDescendant("DataArray"))
				{
					ThrowElementIsMissing("DataArray");
				}

				int numberOfComponents = 1; // one component is default in case of missing attribute
				DataArrayFormat? format = null;
				DataArrayType? type = null;
				while (input.MoveToNextAttribute())
				{
					switch (input.Name.ToLower())
					{
						case "type":
							type = TryParseDataArrayType(input.Value);
							break;
						case "numberofcomponents":
							numberOfComponents = ParseInt32(input.Value);
							break;
						case "format":
							format = TryParseDataArrayFormat(input.Value);
							break;
					}
				}

				if (numberOfComponents < 2 || numberOfComponents > 3)
				{
					throw new FormatException($"Unsupported number of components ({numberOfComponents}).");
				}

				if (!type.HasValue)
				{
					throw new FormatException("Unknown data type");
				}

				if (!format.HasValue)
				{
					throw new FormatException("Unknown data format.");
				}

				if (format != DataArrayFormat.Ascii)
				{
					throw new FormatException("Only Ascii data array format is supported.");
				}

				if (!input.MoveToElement())
				{
					ThrowElementIsMissing("DataArray");
				}

				float[] coordinates = ParseFloat32DataArray(input, format.Value, type.Value); // can't handle 64 precission anyway
				int expectedDataArrayLength = numberOfPoints * numberOfComponents;
				if (coordinates.Length != expectedDataArrayLength)
				{
					throw new FormatException($"Unexpected length of coordinates data array ({coordinates.Length} instead of {expectedDataArrayLength}).");
				}

				// -----------------

				if (!input.ReadToFollowing("Cells"))
				{
					ThrowElementIsMissing("Cells");
				}

				int[] connectivity = readConnectivityArray(input);
				//int[] offsets = readOffsetsArray(input, numberOfCells);
				input.Skip(); // skip offsets data array
				CellType[] types = readTypesArray(input, numberOfCells);

				GeometryDescription geometry = new GeometryDescription
				{
					NumberOfCoordinateComponents = numberOfComponents,
					PointCoordinates = coordinates,
					CellConnectivity = connectivity,
					CellTypes = types
				};

				// TODO: catch all exceptions and convert them to FileParserException

				return geometry;
			}
		}

		#region Private methods

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

			if (!input.MoveToElement())
			{
				ThrowElementIsMissing("Piece");
			}
		}

		private static int[] readConnectivityArray(XmlReader input)
		{
			if (!input.ReadToDescendant("DataArray"))
			{
				ThrowElementIsMissing("DataArray");
			}

			DataArrayFormat? connectivityArrayFormat = null;
			DataArrayType? connectivityArrayType = null;
			while (input.MoveToNextAttribute())
			{
				switch (input.Name.ToLower())
				{
					case "type":
						connectivityArrayType = TryParseDataArrayType(input.Value);
						break;
					case "name":
						if (input.Value.ToLower() != "connectivity")
						{
							throw new FormatException($"Connectivity data array was expected instead of '{input.Value}'.");
						}
						break;
					case "format":
						connectivityArrayFormat = TryParseDataArrayFormat(input.Value);
						break;
				}
			}

			if (!connectivityArrayType.HasValue)
			{
				throw new FormatException("Unknown data type");
			}

			if (!connectivityArrayFormat.HasValue)
			{
				throw new FormatException("Unknown data format.");
			}

			if (connectivityArrayFormat != DataArrayFormat.Ascii)
			{
				throw new FormatException("Only Ascii data array format is supported.");
			}

			if (!input.MoveToElement())
			{
				ThrowElementIsMissing("DataArray");
			}

			return ParseInt32DataArray(input, connectivityArrayFormat.Value, connectivityArrayType.Value);
		}

		private static int[] readOffsetsArray(XmlReader input, int numberOfCells)
		{
			if (!input.ReadToNextSibling("DataArray"))
			{
				ThrowElementIsMissing("DataArray");
			}

			DataArrayFormat? offsetsArrayFormat = null;
			DataArrayType? offsetsArrayType = null;
			while (input.MoveToNextAttribute())
			{
				switch (input.Name.ToLower())
				{
					case "type":
						offsetsArrayType = TryParseDataArrayType(input.Value);
						break;
					case "name":
						if (input.Value.ToLower() != "offsets")
						{
							throw new FormatException($"Offsets data array was expected instead of '{input.Value}'.");
						}
						break;
					case "format":
						offsetsArrayFormat = TryParseDataArrayFormat(input.Value);
						break;
				}
			}

			if (!offsetsArrayType.HasValue)
			{
				throw new FormatException("Unknown data type");
			}

			if (!offsetsArrayFormat.HasValue)
			{
				throw new FormatException("Unknown data format.");
			}

			if (offsetsArrayFormat != DataArrayFormat.Ascii)
			{
				throw new FormatException("Only Ascii data array format is supported.");
			}

			if (!input.MoveToElement())
			{
				ThrowElementIsMissing("DataArray");
			}

			int[] offsets = ParseInt32DataArray(input, offsetsArrayFormat.Value, offsetsArrayType.Value);

			if (offsets.Length != numberOfCells)
			{
				throw new FormatException($"Unexpected length of offsets data array ({offsets.Length} instead of {numberOfCells}).");
			}

			return offsets;
		}

		private static CellType[] readTypesArray(XmlReader input, int numberOfCells)
		{
			if (!input.ReadToNextSibling("DataArray"))
			{
				ThrowElementIsMissing("DataArray");
			}

			DataArrayFormat? typesArrayFormat = null;
			DataArrayType? typesArrayType = null;
			while (input.MoveToNextAttribute())
			{
				switch (input.Name.ToLower())
				{
					case "type":
						typesArrayType = TryParseDataArrayType(input.Value);
						break;
					case "name":
						if (input.Value.ToLower() != "types")
						{
							throw new FormatException($"Types data array was expected instead of '{input.Value}'.");
						}
						break;
					case "format":
						typesArrayFormat = TryParseDataArrayFormat(input.Value);
						break;
				}
			}

			if (!typesArrayType.HasValue)
			{
				throw new FormatException("Unknown data type");
			}

			if (!typesArrayFormat.HasValue)
			{
				throw new FormatException("Unknown data format.");
			}

			if (typesArrayFormat != DataArrayFormat.Ascii)
			{
				throw new FormatException("Only Ascii data array format is supported.");
			}

			if (!input.MoveToElement())
			{
				ThrowElementIsMissing("DataArray");
			}

			byte[] types = ParseUInt8DataArray(input, typesArrayFormat.Value, typesArrayType.Value);

			if (types.Length != numberOfCells)
			{
				throw new FormatException($"Unexpected length of types data array ({types.Length} instead of {numberOfCells}).");
			}

			return (CellType[])(object)types;
		}

		#endregion

	}
}
