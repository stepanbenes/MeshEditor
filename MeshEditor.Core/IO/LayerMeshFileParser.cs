using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.Common.Extensions;
using MeshEditor.Data;
using MeshEditor.IO;
using MeshEditor.LayerManager.Data;
using OpenTK;

namespace MeshEditor.IO
{
	public class LayerMeshFileParser : IMeshFileParser, INameProvider
	{
		readonly GeometryDescription geometry;
		readonly AttributeDescription elementPropertyAttribute;
		readonly IGeometryEntityMapping mappingFromGeometryEntityIndicesToIds;

		public LayerMeshFileParser(string layerName, GeometryDescription geometry, AttributeDescription elementPropertyAttribute, IGeometryEntityMapping mappingFromGeometryEntityIndicesToIds)
		{
			Debug.Assert(geometry != null);
			Debug.Assert(elementPropertyAttribute == null || elementPropertyAttribute.Location == DataLocationType.Cells);
			Name = layerName;
			this.geometry = geometry;
			this.elementPropertyAttribute = elementPropertyAttribute;
			this.mappingFromGeometryEntityIndicesToIds = mappingFromGeometryEntityIndicesToIds;
		}

		public string Name { get; }
		public string Filename => null;
		public int CurrentLineNumber => 0;
		public int NodeCount => geometry?.NumberOfPoints ?? 0;
		public int ElementCount => geometry?.NumberOfCells ?? 0;

		public IEnumerable<Node> ReadNodes()
		{
			for (int index = 0; index < geometry.NumberOfPoints; index++)
			{
				float x = (geometry.NumberOfCoordinateComponents > 0) ? geometry.PointCoordinates[index * geometry.NumberOfCoordinateComponents + 0] : 0f;
				float y = (geometry.NumberOfCoordinateComponents > 1) ? geometry.PointCoordinates[index * geometry.NumberOfCoordinateComponents + 1] : 0f;
				float z = (geometry.NumberOfCoordinateComponents > 2) ? geometry.PointCoordinates[index * geometry.NumberOfCoordinateComponents + 2] : 0f;

				if (mappingFromGeometryEntityIndicesToIds == null || !mappingFromGeometryEntityIndicesToIds.TryMapPoint(index, out int nodeId))
					nodeId = index;

				Node node = new Node(id: nodeId, position: new Vector3(x, y, z), properties: null);
				yield return node;
			}
		}

		public IEnumerable<ElementDraft> ReadElements()
		{
			int offset = 0;
			for (int index = 0; index < geometry.NumberOfCells; index++)
			{
				int nextOffset = geometry.CellOffsets[index];

				int[] nodeIDs = geometry.CellConnectivity.CreateSlice(offset, nextOffset - offset);

				if (mappingFromGeometryEntityIndicesToIds != null)
				{
					for (int i = 0; i < nodeIDs.Length; i++)
					{
						mappingFromGeometryEntityIndicesToIds.TryMapPoint(nodeIDs[i], out nodeIDs[i]);
					}
				}

				var cellType = geometry.CellTypes[index];

				if (cellType == CellType.HexaQuadratic) // numbering is differs between VTK file format and GiD file format, change it
				{
					nodeIDs.SwapSegments(firstIndex: 12, secondIndex: 16, length: 4);
				}

				if (mappingFromGeometryEntityIndicesToIds == null || !mappingFromGeometryEntityIndicesToIds.TryMapCell(index, out int elementId))
					elementId = index;

				ElementDraft element = new ElementDraft
				{
					ID = elementId,
					NodeIDs = nodeIDs,
					Type = mapCellTypeToElementType(cellType)
				};

				if (elementPropertyAttribute != null)
				{
					element.Property = new Property(elementPropertyAttribute.Values[index]);
				}

				yield return element;

				offset = nextOffset;
			}
		}

		public void Dispose()
		{ }

		#region Private methods

		private static ElementType mapCellTypeToElementType(CellType cellType)
		{
			switch (cellType)
			{
				case CellType.LineLinear:
					return ElementType.BeamLinear;
				case CellType.LineQuadratic:
					return ElementType.BeamQuadratic;
				case CellType.TriangleLinear:
					return ElementType.TriangleLinear;
				case CellType.TriangleQuadratic:
					return ElementType.TriangleQuadratic;
				case CellType.QuadLinear:
					return ElementType.QuadLinear;
				case CellType.QuadQuadratic:
					return ElementType.QuadQuadratic;
				case CellType.TetraLinear:
					return ElementType.TetrahedronLinear;
				case CellType.TetraQuadratic:
					return ElementType.TetrahedronQuadratic;
				case CellType.WedgeLinear:
					return ElementType.TriangularPrismLinear;
				case CellType.WedgeQuadratic:
					return ElementType.TriangularPrismQuadratic;
				case CellType.HexaLinear:
					return ElementType.HexahedronLinear;
				case CellType.HexaQuadratic:
					return ElementType.HexahedronQuadratic;
				case CellType.Undefined:
				case CellType.Point:
				default:
					throw new NotSupportedException();
			}
		}

		#endregion
	}
}
