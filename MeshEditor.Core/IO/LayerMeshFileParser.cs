using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.Data;
using MeshEditor.LayerManager;
using MeshEditor.LayerManager.Data;
using MeshEditor.LayerManager.Import;
using MeshEditor.LayerManager.Serialization;
using MeshEditor.LayerManager.Storage;
using OpenTK;

namespace MeshEditor.IO
{
	class LayerMeshFileParser : IMeshFileParser
	{
		GeometryDescription geometry;

		public LayerMeshFileParser(string filename)
		{
			Filename = filename;
		}

		public string Filename { get; }
		public int CurrentLineNumber => 0;
		public int NodeCount => geometry?.NumberOfPoints ?? 0;
		public int ElementCount => geometry?.NumberOfCells ?? 0;

		public IEnumerable<Node> ReadNodes()
		{
			if (geometry == null)
			{
				loadGeometry();
			}

			for (int i = 0; i < geometry.NumberOfPoints; i++)
			{
				float x = (geometry.NumberOfCoordinateComponents > 0) ? geometry.PointCoordinates[i * geometry.NumberOfCoordinateComponents + 0] : 0f;
				float y = (geometry.NumberOfCoordinateComponents > 1) ? geometry.PointCoordinates[i * geometry.NumberOfCoordinateComponents + 1] : 0f;
				float z = (geometry.NumberOfCoordinateComponents > 2) ? geometry.PointCoordinates[i * geometry.NumberOfCoordinateComponents + 2] : 0f;

				Node node = new Node(id: i, position: new Vector3(x, y, z), properties: null);
				yield return node;
			}
		}

		public IEnumerable<ElementDraft> ReadElements()
		{
			if (geometry == null)
			{
				loadGeometry();
			}

			int offset = 0;
			for (int i = 0; i < geometry.NumberOfCells; i++)
			{
				int[] nodeIDs = Utilities.Functions.GetSliceOfArray(geometry.CellConnectivity, offset, GeometryDescription.MapCellTypeToNumberOfPoints(geometry.CellTypes[i]));
				yield return new ElementDraft { ID = i, NodeIDs = nodeIDs, Type = mapCellTypeToElementType(geometry.CellTypes[i]) };
				offset = geometry.CellOffsets[i];
			}
		}

		public void Dispose()
		{
			geometry = null;
		}

		#region Private methods

		private void loadGeometry()
		{
			geometry = new LayerGenerator().LoadGeometry(new Uri(Filename));
		}

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
