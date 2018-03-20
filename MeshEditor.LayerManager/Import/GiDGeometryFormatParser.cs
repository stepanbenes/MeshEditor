using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.Common.Extensions;
using MeshEditor.LayerManager.Data;
using MeshEditor.LayerManager.Storage;

namespace MeshEditor.LayerManager.Import
{
	class GiDGeometryFormatParser : FormatParserBase, IGeometryImportService
	{
		#region Static members

		public const string COMMENT_PATTERN = "#";
		public const string MESH_PATTERN = "MESH";
		public const string DIMENSION_PATTERN = "dimension";
		public const string ELEMTYPE_PATTERN = "Elemtype";
		public const string NNODE_PATTERN = "Nnode";
		public const string COORDINATES_PATTERN = "coordinates";
		public const string ELEMENTS_PATTERN = "elements";
		public const string END_PATTERN = "end";

		private static readonly char[] whiteSpaceSeparators = { ' ', '\t' };

		private enum ParserMode
		{
			Init,
			Coordinates,
			Elements
		}

		#endregion

		#region Fields, constructor

		IReadStorageService storageService;
		string recordName;

		public GiDGeometryFormatParser(IReadStorageService storageService, string recordName)
		{
			this.storageService = storageService;
			this.recordName = recordName;
		}

		#endregion

		#region Public methods

		public GeometryDescription ReadGeometry(out IReadOnlyList<AttributeDescription> attributes)
		{
			List<float> pointCoordinates = new List<float>();
			List<int> cellConnectivity = new List<int>();
			List<int> cellOffsets = new List<int>();
			List<CellType> cellTypes = new List<CellType>();
			List<int> elementProperties = new List<int>();
			List<int> elementNumbers = new List<int>();
			List<int> nodeNumbers = new List<int>();

			int dimension = 0;
			int currentLineNumber = 0;
			int numberOfNodes = 0;
			int numberOfElements = 0;
			GeometryEntityMapping mapping = new GeometryEntityMapping();

			using (Stream fileStream = storageService.Load(recordName))
			using (TextReader reader = new StreamReader(fileStream))
			{
				string line;
				string[] parts;
				string meshName;

				int nnode = 0;
				ParserMode parserMode = ParserMode.Init;
				CellType elementType = CellType.Undefined;
				while ((line = reader.ReadLine()) != null)
				{
					++currentLineNumber;
					line = line.Trim();
					if (line.StartsWith(COMMENT_PATTERN))
						continue;
					parts = line.Split(whiteSpaceSeparators, StringSplitOptions.RemoveEmptyEntries);
					if (parts.Length == 0)
						continue;

					if (parts[0].Equals(MESH_PATTERN, StringComparison.InvariantCultureIgnoreCase)) // MESH
					{
						parserMode = ParserMode.Init;
						int index = 1;
						Debug.Assert(parts.Length >= 7);
						if (!parts[index].Equals(DIMENSION_PATTERN, StringComparison.InvariantCultureIgnoreCase)) // if dimension keyword is not following, current part is mesh_name
							meshName = parts[index++]; // optional mesh name
						Debug.Assert(parts[index].Equals(DIMENSION_PATTERN, StringComparison.InvariantCultureIgnoreCase));
						++index;
						dimension = ParseInt32(parts[index++]); // dimension
						Debug.Assert(parts[index].Equals(ELEMTYPE_PATTERN, StringComparison.InvariantCultureIgnoreCase));
						++index;
						string elementTypeName = parts[index++]; // element type
						Debug.Assert(parts[index].Equals(NNODE_PATTERN, StringComparison.InvariantCultureIgnoreCase));
						++index;
						nnode = ParseInt32(parts[index++]); // number of nodes in one element of type elementType

						// determine element type
						elementType = convertNameToCellType(elementTypeName, nnode);
					}
					else if (parts[0].Equals(COORDINATES_PATTERN, StringComparison.InvariantCultureIgnoreCase)) // COORDINATES
					{
						parserMode = ParserMode.Coordinates; // prepare to read node coordinates
					}
					else if (parts[0].Equals(ELEMENTS_PATTERN, StringComparison.InvariantCultureIgnoreCase)) // ELEMENTS
					{
						parserMode = ParserMode.Elements; // prepare to read elements
					}
					else
					{
						switch (parserMode)
						{
							case ParserMode.Coordinates:
								{
									if (parts[0].Equals(END_PATTERN, StringComparison.InvariantCultureIgnoreCase))
									{
										Debug.Assert(parts.Length > 1 && parts[1].Equals(COORDINATES_PATTERN, StringComparison.InvariantCultureIgnoreCase));
										parserMode = ParserMode.Init;
										break;
									}
									Debug.Assert(parts.Length >= 3);
									int nodeId = ParseInt32(parts[0]);

									mapping.AddPointMapping(from: nodeId, to: numberOfNodes);
									nodeNumbers.Add(nodeId);

									float positionX = (float)ParseFloat64(parts[1]); // WARNING: possible loss of precision
									float positionY = (float)ParseFloat64(parts[2]); // WARNING: possible loss of precision
									float positionZ = (parts.Length >= 4) ? (float)ParseFloat64(parts[3]) : 0f; // WARNING: possible loss of precision

									if (dimension == 2)
									{
										pointCoordinates.Add(positionX);
										pointCoordinates.Add(positionY);
									}
									else
									{
										pointCoordinates.Add(positionX);
										pointCoordinates.Add(positionY);
										pointCoordinates.Add(positionZ);
									}
									numberOfNodes += 1;
								}
								break;
							case ParserMode.Elements:
								{
									if (parts[0].Equals(END_PATTERN, StringComparison.InvariantCultureIgnoreCase))
									{
										Debug.Assert(parts.Length > 1 && parts[1].Equals(ELEMENTS_PATTERN, StringComparison.InvariantCultureIgnoreCase));
										parserMode = ParserMode.Init;
										break;
									}
									Debug.Assert(parts.Length >= nnode + 1);

									int lastOffset = cellOffsets.LastOrDefault();
									cellOffsets.Add(lastOffset + GeometryDescription.MapCellTypeToNumberOfPoints(elementType));
									cellTypes.Add(elementType);

									int elementId = ParseInt32(parts[0]);

									mapping.AddCellMapping(from: elementId, to: numberOfElements);
									elementNumbers.Add(elementId);

									for (int i = 0; i < nnode; i++)
									{
										int nodeId = ParseInt32(parts[i + 1]);
										int nodeIndex;
										if (!mapping.TryMapPoint(nodeId, out nodeIndex))
											throw new KeyNotFoundException($"node with id {nodeId} was not found");
										cellConnectivity.Add(nodeIndex);
									}

									if (elementType == CellType.HexaQuadratic) // numbering is differs between VTK file format and GiD file format, change it
									{
										cellConnectivity.SwapSegments(firstIndex: lastOffset + 12, secondIndex: lastOffset + 16, length: 4);
									}

									if (parts.Length > nnode + 1)
									{
										int elementProperty = ParseInt32(parts[nnode + 1]); // read optional material number
										elementProperties.Add(elementProperty);
									}
									else
									{
										elementProperties.Add(0);
									}
									numberOfElements += 1;
								}
								break;
						}
					}
				}
			}

			if (dimension != 2 && dimension != 3)
			{
				throw new NotSupportedException($"This dimension is not supported ({dimension}).");
			}

			GeometryDescription geometry = new GeometryDescription
			{
				NumberOfCoordinateComponents = dimension,
				PointCoordinates = pointCoordinates.ToArray(),
				CellConnectivity = cellConnectivity.ToArray(),
				CellOffsets = cellOffsets.ToArray(),
				CellTypes = cellTypes.ToArray(),
				Mapping = mapping
			};

			attributes = new[]
			{
				new AttributeDescription { Name = AttributeDescription.KnownAttributeNames.NodeNumber, Location = DataLocationType.Points, Values = nodeNumbers.ToArray() },
				new AttributeDescription { Name = AttributeDescription.KnownAttributeNames.ElementNumber, Location = DataLocationType.Cells, Values = elementNumbers.ToArray() },
				new AttributeDescription { Name = AttributeDescription.KnownAttributeNames.ElementProperty, Location = DataLocationType.Cells, Values = elementProperties.ToArray() },
			};

			return geometry;
		}

		#endregion

		#region Private methods

		private static CellType convertNameToCellType(string elementTypeName, int numberOfNodes)
		{
			Debug.Assert(elementTypeName != null);
			switch (elementTypeName.ToLower())
			{
				case "linear":
					if (numberOfNodes == 2)
						return CellType.LineLinear;
					if (numberOfNodes == 3)
						return CellType.LineQuadratic;
					break;
				case "triangle":
					if (numberOfNodes == 3)
						return CellType.TriangleLinear;
					if (numberOfNodes == 6)
						return CellType.TriangleQuadratic;
					break;
				case "quadrilateral":
					if (numberOfNodes == 4)
						return CellType.QuadLinear;
					if (numberOfNodes == 8)
						return CellType.QuadQuadratic;
					break;
				case "tetrahedra":
					if (numberOfNodes == 4)
						return CellType.TetraLinear;
					if (numberOfNodes == 10)
						return CellType.TetraQuadratic;
					break;
				case "hexahedra":
					if (numberOfNodes == 8)
						return CellType.HexaLinear;
					if (numberOfNodes == 20)
						return CellType.HexaQuadratic;
					break;
				case "prism":
					if (numberOfNodes == 6)
						return CellType.WedgeLinear;
					if (numberOfNodes == 15)
						return CellType.WedgeQuadratic;
					break;
			}
			throw new NotSupportedException($"This combination of element type name and number of nodes is not supported ({elementTypeName}, {numberOfNodes}).");
		}

		#endregion
	}
}
