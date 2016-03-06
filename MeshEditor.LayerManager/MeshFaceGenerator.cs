using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.LayerManager.Import;

namespace MeshEditor.LayerManager
{
	class MeshFaceGenerator
	{
		public struct TriangleFace
		{
			public int Point1Id { get; }
			public int Point2Id { get; }
			public int Point3Id { get; }
			public TriangleFace(int point1Id, int point2Id, int point3Id)
			{
				Point1Id = point1Id;
				Point2Id = point2Id;
				Point3Id = point3Id;
			}
		}

		public int NumberOfTriangles => TriangleConnectivity.Length / 3;
		public int NumberOfEdges => EdgeConnectivity.Length / 2;

		public int[] TriangleConnectivity { get; private set; }
		public int[] EdgeConnectivity { get; private set; }

		public void ProcessGeometry(GeometryDescription geometry)
		{
			List<TriangleFace> triangleFaces = new List<TriangleFace>();
			int pointIndex = 0;
			for (int i = 0; i < geometry.NumberOfCells; i++)
			{
				switch (geometry.CellTypes[i])
				{
					case CellType.Point:
						continue;
					case CellType.LineLinear:
						throw new NotImplementedException();
					case CellType.LineQuadratic:
						throw new NotImplementedException();
					case CellType.TriangleLinear:
						triangleFaces.Add(new TriangleFace(geometry.CellConnectivity[pointIndex] + 1, geometry.CellConnectivity[pointIndex + 1] + 1, geometry.CellConnectivity[pointIndex + 2] + 1));
						pointIndex += 3;
						break;
					case CellType.TriangleQuadratic:
						throw new NotImplementedException();
					case CellType.QuadLinear:
						throw new NotImplementedException();
					case CellType.QuadQuadratic:
						throw new NotImplementedException();
					case CellType.TetraLinear:
						throw new NotImplementedException();
					case CellType.TetraQuadratic:
						throw new NotImplementedException();
					case CellType.WedgeLinear:
						throw new NotImplementedException();
					case CellType.WedgeQuadratic:
						throw new NotImplementedException();
					case CellType.HexaLinear:
						throw new NotImplementedException();
					case CellType.HexaQuadratic:
						throw new NotImplementedException();
					default:
						throw new NotSupportedException();
				}
			}

			TriangleConnectivity = new int[triangleFaces.Count * 3];
			for (int i = 0; i < triangleFaces.Count; i++)
			{
				TriangleFace triangle = triangleFaces[i];
				TriangleConnectivity[i * 3 + 0] = triangle.Point1Id;
				TriangleConnectivity[i * 3 + 1] = triangle.Point2Id;
				TriangleConnectivity[i * 3 + 2] = triangle.Point3Id;
			}

			EdgeConnectivity = new int[0];
		}
	}
}
