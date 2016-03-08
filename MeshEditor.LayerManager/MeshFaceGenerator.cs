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
		#region Static members

		private struct TriangleFace
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

		#endregion

		#region Properties

		public int NumberOfTriangles => TriangleConnectivity.Length / 3;
		public int NumberOfEdges => EdgeConnectivity.Length / 2;

		public int[] TriangleConnectivity { get; private set; }
		public int[] EdgeConnectivity { get; private set; }

		#endregion

		#region Public methods

		public void ProcessGeometry(GeometryDescription geometry)
		{
			// TODO: pair faces and leave only one of two twin faces, also mark external faces

			List<TriangleFace> triangleFaces = new List<TriangleFace>();
			int pointIndex = 0;
			for (int i = 0; i < geometry.NumberOfCells; i++)
			{
				triangleFaces.AddRange(GetSequenceOfFaces(geometry.CellTypes[i], geometry.CellConnectivity, pointIndex));
				pointIndex += mapCellTypeToNumberOfPoints(geometry.CellTypes[i]);
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

		private static IEnumerable<TriangleFace> GetSequenceOfFaces(CellType cellType, int[] cellConnectivity, int startIndex)
		{
			// depends on face ordering!
			switch (cellType)
			{
				case CellType.Point:
				case CellType.LineLinear:
				case CellType.LineQuadratic:
					yield break; // point and beam has no face
				case CellType.TriangleLinear:
					yield return new TriangleFace(
						cellConnectivity[startIndex],
						cellConnectivity[startIndex + 1],
						cellConnectivity[startIndex + 2]);
					break;
				case CellType.TriangleQuadratic:

					throw new NotImplementedException();

				case CellType.QuadLinear:
					yield return new TriangleFace(
						cellConnectivity[startIndex + 0],
						cellConnectivity[startIndex + 1],
						cellConnectivity[startIndex + 2]);
					yield return new TriangleFace(
						cellConnectivity[startIndex + 0],
						cellConnectivity[startIndex + 2],
						cellConnectivity[startIndex + 3]);
					break;
				case CellType.QuadQuadratic:

					throw new NotImplementedException();

				case CellType.TetraLinear:
					yield return new TriangleFace(
						cellConnectivity[startIndex + 2],
						cellConnectivity[startIndex + 1],
						cellConnectivity[startIndex + 3]);
					yield return new TriangleFace(
						cellConnectivity[startIndex + 0],
						cellConnectivity[startIndex + 2],
						cellConnectivity[startIndex + 3]);
					yield return new TriangleFace(
						cellConnectivity[startIndex + 1],
						cellConnectivity[startIndex + 0],
						cellConnectivity[startIndex + 3]);
					yield return new TriangleFace(
						cellConnectivity[startIndex + 0],
						cellConnectivity[startIndex + 1],
						cellConnectivity[startIndex + 2]);
					break;
				case CellType.TetraQuadratic:

					throw new NotImplementedException();

				case CellType.WedgeLinear:
					yield return new TriangleFace(
						cellConnectivity[startIndex + 0],
						cellConnectivity[startIndex + 1],
						cellConnectivity[startIndex + 2]);
					yield return new TriangleFace(
						cellConnectivity[startIndex + 3],
						cellConnectivity[startIndex + 5],
						cellConnectivity[startIndex + 4]);

					yield return new TriangleFace(
						cellConnectivity[startIndex + 1],
						cellConnectivity[startIndex + 0],
						cellConnectivity[startIndex + 3]);
					yield return new TriangleFace(
						cellConnectivity[startIndex + 1],
						cellConnectivity[startIndex + 3],
						cellConnectivity[startIndex + 4]);

					yield return new TriangleFace(
						cellConnectivity[startIndex + 2],
						cellConnectivity[startIndex + 1],
						cellConnectivity[startIndex + 4]);
					yield return new TriangleFace(
						cellConnectivity[startIndex + 2],
						cellConnectivity[startIndex + 4],
						cellConnectivity[startIndex + 5]);

					yield return new TriangleFace(
						cellConnectivity[startIndex + 0],
						cellConnectivity[startIndex + 2],
						cellConnectivity[startIndex + 5]);
					yield return new TriangleFace(
						cellConnectivity[startIndex + 0],
						cellConnectivity[startIndex + 5],
						cellConnectivity[startIndex + 3]);
					break;
				case CellType.WedgeQuadratic:

					throw new NotImplementedException();

				case CellType.HexaLinear:
					yield return new TriangleFace(
						cellConnectivity[startIndex + 0],
						cellConnectivity[startIndex + 3],
						cellConnectivity[startIndex + 7]);
					yield return new TriangleFace(
						cellConnectivity[startIndex + 0],
						cellConnectivity[startIndex + 7],
						cellConnectivity[startIndex + 4]);

					yield return new TriangleFace(
						cellConnectivity[startIndex + 1],
						cellConnectivity[startIndex + 0],
						cellConnectivity[startIndex + 4]);
					yield return new TriangleFace(
						cellConnectivity[startIndex + 1],
						cellConnectivity[startIndex + 4],
						cellConnectivity[startIndex + 5]);

					yield return new TriangleFace(
						cellConnectivity[startIndex + 2],
						cellConnectivity[startIndex + 1],
						cellConnectivity[startIndex + 5]);
					yield return new TriangleFace(
						cellConnectivity[startIndex + 2],
						cellConnectivity[startIndex + 5],
						cellConnectivity[startIndex + 6]);

					yield return new TriangleFace(
						cellConnectivity[startIndex + 3],
						cellConnectivity[startIndex + 2],
						cellConnectivity[startIndex + 6]);
					yield return new TriangleFace(
						cellConnectivity[startIndex + 3],
						cellConnectivity[startIndex + 6],
						cellConnectivity[startIndex + 7]);

					yield return new TriangleFace(
						cellConnectivity[startIndex + 0],
						cellConnectivity[startIndex + 1],
						cellConnectivity[startIndex + 2]);
					yield return new TriangleFace(
						cellConnectivity[startIndex + 0],
						cellConnectivity[startIndex + 2],
						cellConnectivity[startIndex + 3]);

					yield return new TriangleFace(
						cellConnectivity[startIndex + 4],
						cellConnectivity[startIndex + 5],
						cellConnectivity[startIndex + 6]);
					yield return new TriangleFace(
						cellConnectivity[startIndex + 4],
						cellConnectivity[startIndex + 6],
						cellConnectivity[startIndex + 7]);
					break;
				case CellType.HexaQuadratic:

					throw new NotImplementedException();

				default:
					throw new NotSupportedException();
			}
		}

		#endregion
	}
}
