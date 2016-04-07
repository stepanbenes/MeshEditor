using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.LayerManager.Data;
using MeshEditor.LayerManager.Import;

namespace MeshEditor.LayerManager
{
	internal class MeshSurfaceGenerator
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
			for (int i = 0, offset = 0; i < geometry.NumberOfCells; i++)
			{
				triangleFaces.AddRange(GetSequenceOfFaces(geometry.CellTypes[i], geometry.CellConnectivity, offset));
				offset = geometry.CellOffsets[i];
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
