using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.LayerManager.Import;
using MeshEditor.LayerManager.Storage;

namespace MeshEditor.FormatConverter.Import
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

		IStorageService storageService;
		Uri uri;

		public GiDGeometryFormatParser(IStorageService storageService, Uri uri)
		{
			this.storageService = storageService;
			this.uri = uri;
		}

		#endregion

		#region Public methods

		public GeometryDescription ReadGeometry()
		{
			List<float> pointCoordinates = new List<float>();
			List<int> cellConnectivity = new List<int>();
			List<int> cellOffsets = new List<int>();
			List<CellType> cellTypes = new List<CellType>();

			int dimension = 0;
			int currentLineNumber = 0;
			Dictionary<int, int> nodeIdIndexMap = new Dictionary<int, int>();
			Dictionary<int, int> elementIdIndexMap = new Dictionary<int, int>();

			using (Stream fileStream = storageService.Load(uri))
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

									nodeIdIndexMap[nodeId] = nodeIdIndexMap.Count;

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

									cellOffsets.Add(cellOffsets.LastOrDefault() + mapCellTypeToNumberOfPoints(elementType));
									cellTypes.Add(elementType);

									int elementId = ParseInt32(parts[0]);
									elementIdIndexMap[elementId] = elementIdIndexMap.Count;

									for (int i = 0; i < nnode; i++)
									{
										int nodeId = ParseInt32(parts[i + 1]);
										int nodeIndex = nodeIdIndexMap[nodeId];
										cellConnectivity.Add(nodeIndex);
									}
									//if (parts.Length > nnode + 1)
									//{
									//	int elementProperty = ParseInt32(parts[nnode + 1]); // read optional material number
									//}
								}
								break;
						}
					}
				}
			}

			if (dimension != 2 && dimension != 3)
				throw new NotSupportedException($"This dimension is not supported ({dimension}).");

			GeometryDescription geometry = new GeometryDescription
			{
				NumberOfCoordinateComponents = dimension,
				PointCoordinates = pointCoordinates.ToArray(),
				CellConnectivity = cellConnectivity.ToArray(),
				CellOffsets = cellOffsets.ToArray(),
				CellTypes = cellTypes.ToArray(),
				PointIdIndexMap = nodeIdIndexMap,
				CellIdIndexMap = elementIdIndexMap
			};
			return geometry;
		}

		#endregion

		#region Private methods

		private static int mapCellTypeToNumberOfPoints(CellType cellType)
		{
			switch (cellType)
			{
				case CellType.Point:
					return 1;
				case CellType.LineLinear:
					return 2;
				case CellType.LineQuadratic:
					return 3;
				case CellType.TriangleLinear:
					return 3;
				case CellType.TriangleQuadratic:
					return 6;
				case CellType.QuadLinear:
					return 4;
				case CellType.QuadQuadratic:
					return 8;
				case CellType.TetraLinear:
					return 4;
				case CellType.TetraQuadratic:
					return 10;
				case CellType.WedgeLinear:
					return 6;
				case CellType.WedgeQuadratic:
					return 15;
				case CellType.HexaLinear:
					return 8;
				case CellType.HexaQuadratic:
					return 20;
				default:
					throw new NotSupportedException();
			}
		}

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
