using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using MeshEditor.LayerManager.Data;
using MeshEditor.LayerManager.Filters;

namespace MeshEditor.LayerManager.MeshFiltering
{
	internal class MeshSurfaceCreator : IMeshFilterCreator
	{
		#region Static members

		private struct TriangleFace : IEquatable<TriangleFace>
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

			public bool Equals(TriangleFace other)
			{
				TriangleFace thisSorted = this.createSorted();
				TriangleFace otherSorted = other.createSorted();
				return thisSorted.Point1Id == otherSorted.Point1Id && thisSorted.Point2Id == otherSorted.Point2Id && thisSorted.Point3Id == otherSorted.Point3Id;
			}

			private TriangleFace createSorted()
			{
				int p1 = Point1Id;
				int p2 = Point2Id;
				int p3 = Point3Id;
				// sort indices using bubble-sort
				if (p1 > p2)
					swap(ref p1, ref p2);
				if (p1 > p3)
					swap(ref p1, ref p3);
				if (p2 > p3)
					swap(ref p2, ref p3);
				return new TriangleFace(p1, p2, p3);
			}

			private static void swap(ref int a, ref int b)
			{
				int temp = a;
				a = b;
				b = temp;
			}

			public override bool Equals(object obj)
			{
				if (obj is TriangleFace)
					return this.Equals((TriangleFace)obj);
				return false;
			}

			public override int GetHashCode()
			{
				TriangleFace thisSorted = createSorted();
				unchecked // Overflow is fine, just wrap
				{
					int hash = 17;
					hash = hash * thisSorted.Point1Id.GetHashCode();
					hash = hash * thisSorted.Point2Id.GetHashCode();
					hash = hash * thisSorted.Point3Id.GetHashCode();
					return hash;
				}
			}

			public override string ToString() => $"{nameof(TriangleFace)} [{Point1Id}, {Point2Id}, {Point3Id}]";

			public static void SplitRectangleToTriangles(int p1, int p2, int p3, int p4, out TriangleFace firstTriangle, out TriangleFace secondTriangle)
			{
				if (p1 < p2 && p1 < p3 && p1 < p4) // first is smallest
				{
					firstTriangle = new TriangleFace(p1, p2, p3);
					secondTriangle = new TriangleFace(p1, p3, p4);
				}
				else if (p2 < p1 && p2 < p3 && p2 < p4) // second is smallest
				{
					firstTriangle = new TriangleFace(p2, p3, p4);
					secondTriangle = new TriangleFace(p2, p4, p1);
				}
				else if (p3 < p1 && p3 < p2 && p3 < p4) // third is smallest
				{
					firstTriangle = new TriangleFace(p3, p4, p1);
					secondTriangle = new TriangleFace(p3, p1, p2);
				}
				else // fourth is smallest
				{
					Debug.Assert(p4 < p1 && p4 < p2 && p4 < p3);
					firstTriangle = new TriangleFace(p4, p1, p2);
					secondTriangle = new TriangleFace(p4, p2, p3);
				}
			}

			public bool IsCollapsed() => Point1Id == Point2Id || Point1Id == Point3Id || Point2Id == Point3Id;
		}

		#endregion

		#region Fields, constructor

		readonly SurfaceFilter surfaceFilter;

		public MeshSurfaceCreator(SurfaceFilter surfaceFilter)
		{
			this.surfaceFilter = surfaceFilter;
		}

		#endregion

		#region Public methods

		public IEnumerable<(GeometryDescription geometry, List<double> timeSteps)> Create(GeometryDescription geometry, IEnumerable<double> timeSteps)
		{
			Dictionary<TriangleFace, int> surfaceTriangles = new Dictionary<TriangleFace, int>();
			for (int cellIndex = 0; cellIndex < geometry.NumberOfCells; cellIndex++)
			{
				foreach (TriangleFace cellFace in getFacesOfCell(geometry, cellIndex))
				{
					if (cellFace.IsCollapsed())
						continue;

					if (surfaceTriangles.ContainsKey(cellFace))
					{
						surfaceTriangles.Remove(cellFace);
					}
					else
					{
						surfaceTriangles.Add(cellFace, cellIndex);
					}
				}
			}

			HashSet<int> pointSet = new HashSet<int>();
			foreach (TriangleFace surfaceTriangle in surfaceTriangles.Keys)
			{
				pointSet.Add(surfaceTriangle.Point1Id);
				pointSet.Add(surfaceTriangle.Point2Id);
				pointSet.Add(surfaceTriangle.Point3Id);
			}

			Dictionary<int, int> oldToNewPointMap = new Dictionary<int, int>();

			GeometryBuilder geometryBuilder = new GeometryBuilder(geometry.NumberOfCoordinateComponents);
			int newPointId = 0;
			foreach (int oldPointId in pointSet)
			{
				float x = (geometry.NumberOfCoordinateComponents > 0) ? geometry.PointCoordinates[oldPointId * geometry.NumberOfCoordinateComponents + 0] : 0f;
				float y = (geometry.NumberOfCoordinateComponents > 1) ? geometry.PointCoordinates[oldPointId * geometry.NumberOfCoordinateComponents + 1] : 0f;
				float z = (geometry.NumberOfCoordinateComponents > 2) ? geometry.PointCoordinates[oldPointId * geometry.NumberOfCoordinateComponents + 2] : 0f;
				geometryBuilder.AddPoint(new Vector3(x, y, z), oldPointId);
				oldToNewPointMap.Add(oldPointId, newPointId);
				newPointId += 1;
			}

			foreach (var pair in surfaceTriangles)
			{
				TriangleFace surfaceTriangle = pair.Key;
				int oldCellId = pair.Value;
				geometryBuilder.AddCell(
					CellType.TriangleLinear,
					oldCellId,
					connectivity: new[] { oldToNewPointMap[surfaceTriangle.Point1Id], oldToNewPointMap[surfaceTriangle.Point2Id], oldToNewPointMap[surfaceTriangle.Point3Id] },
					oldCellPointIds: new[] { surfaceTriangle.Point1Id, surfaceTriangle.Point2Id, surfaceTriangle.Point3Id }
				);
			}

			var surface = geometryBuilder.Build();

			return new[] { (surface, timeSteps.ToList()) };
		}

		#endregion

		#region Private methods

		private static IEnumerable<TriangleFace> getFacesOfCell(GeometryDescription geometry, int cellIndex)
		{
			CellType cellType = geometry.CellTypes[cellIndex];
			int[] connectivity = geometry.CellConnectivity;
			int startIndex = (cellIndex > 0) ? geometry.CellOffsets[cellIndex - 1] : 0;
			TriangleFace t1, t2;

			// depends on face ordering! see: http://www.vtk.org/wp-content/uploads/2015/04/file-formats.pdf
			switch (cellType)
			{
				case CellType.Point:
				case CellType.LineLinear:
				case CellType.LineQuadratic:

					yield break; // point and beam have no face

				case CellType.TriangleLinear: // just make a copy
					yield return new TriangleFace(
						connectivity[startIndex + 0],
						connectivity[startIndex + 1],
						connectivity[startIndex + 2]);
					break;
				case CellType.TriangleQuadratic:
					foreach (var qt in getFacesOfQuadraticTriangle(connectivity, startIndex, 0, 1, 2, 3, 4, 5))
						yield return qt;
					break;
				case CellType.QuadLinear: // just split to two triangles
					TriangleFace.SplitRectangleToTriangles(
						connectivity[startIndex + 0],
						connectivity[startIndex + 1],
						connectivity[startIndex + 2],
						connectivity[startIndex + 3],
						out t1, out t2);
					yield return t1;
					yield return t2;
					break;
				case CellType.QuadQuadratic:
					foreach (var qq in getFacesOfQuadraticQuad(connectivity, startIndex, 0, 1, 2, 3, 4, 5, 6, 7))
						yield return qq;
					break;
				case CellType.TetraLinear:
					yield return new TriangleFace(
						connectivity[startIndex + 2],
						connectivity[startIndex + 1],
						connectivity[startIndex + 3]);
					yield return new TriangleFace(
						connectivity[startIndex + 0],
						connectivity[startIndex + 2],
						connectivity[startIndex + 3]);
					yield return new TriangleFace(
						connectivity[startIndex + 1],
						connectivity[startIndex + 0],
						connectivity[startIndex + 3]);
					yield return new TriangleFace(
						connectivity[startIndex + 0],
						connectivity[startIndex + 1],
						connectivity[startIndex + 2]);
					break;
				case CellType.TetraQuadratic:
					foreach (var qt in getFacesOfQuadraticTriangle(connectivity, startIndex, 0, 1, 3, 4, 8, 7))
						yield return qt;
					foreach (var qt in getFacesOfQuadraticTriangle(connectivity, startIndex, 0, 3, 2, 7, 9, 6))
						yield return qt;
					foreach (var qt in getFacesOfQuadraticTriangle(connectivity, startIndex, 0, 2, 1, 6, 5, 4))
						yield return qt;
					foreach (var qt in getFacesOfQuadraticTriangle(connectivity, startIndex, 1, 2, 3, 5, 9, 8))
						yield return qt;
					break;
				case CellType.WedgeLinear:
					yield return new TriangleFace( // bottom triangle
						connectivity[startIndex + 0],
						connectivity[startIndex + 1],
						connectivity[startIndex + 2]);
					yield return new TriangleFace( // top triangle
						connectivity[startIndex + 3],
						connectivity[startIndex + 5],
						connectivity[startIndex + 4]);

					TriangleFace.SplitRectangleToTriangles(
						connectivity[startIndex + 1],
						connectivity[startIndex + 0],
						connectivity[startIndex + 3],
						connectivity[startIndex + 4],
						out t1, out t2);
					yield return t1;
					yield return t2;

					TriangleFace.SplitRectangleToTriangles(
						connectivity[startIndex + 2],
						connectivity[startIndex + 1],
						connectivity[startIndex + 4],
						connectivity[startIndex + 5],
						out t1, out t2);
					yield return t1;
					yield return t2;

					TriangleFace.SplitRectangleToTriangles(
						connectivity[startIndex + 0],
						connectivity[startIndex + 2],
						connectivity[startIndex + 5],
						connectivity[startIndex + 3],
						out t1, out t2);
					yield return t1;
					yield return t2;

					break;
				case CellType.WedgeQuadratic:

					throw new NotImplementedException();

				case CellType.HexaLinear:

					TriangleFace.SplitRectangleToTriangles(
						connectivity[startIndex + 0],
						connectivity[startIndex + 3],
						connectivity[startIndex + 7],
						connectivity[startIndex + 4],
						out t1, out t2);
					yield return t1;
					yield return t2;

					TriangleFace.SplitRectangleToTriangles(
						connectivity[startIndex + 1],
						connectivity[startIndex + 0],
						connectivity[startIndex + 4],
						connectivity[startIndex + 5],
						out t1, out t2);
					yield return t1;
					yield return t2;

					TriangleFace.SplitRectangleToTriangles(
						connectivity[startIndex + 2],
						connectivity[startIndex + 1],
						connectivity[startIndex + 5],
						connectivity[startIndex + 6],
						out t1, out t2);
					yield return t1;
					yield return t2;

					TriangleFace.SplitRectangleToTriangles(
						connectivity[startIndex + 3],
						connectivity[startIndex + 2],
						connectivity[startIndex + 6],
						connectivity[startIndex + 7],
						out t1, out t2);
					yield return t1;
					yield return t2;

					TriangleFace.SplitRectangleToTriangles(
						connectivity[startIndex + 0],
						connectivity[startIndex + 1],
						connectivity[startIndex + 2],
						connectivity[startIndex + 3],
						out t1, out t2);
					yield return t1;
					yield return t2;

					TriangleFace.SplitRectangleToTriangles(
						connectivity[startIndex + 4],
						connectivity[startIndex + 5],
						connectivity[startIndex + 6],
						connectivity[startIndex + 7],
						out t1, out t2);
					yield return t1;
					yield return t2;

					break;
				case CellType.HexaQuadratic:
					foreach (var qq in getFacesOfQuadraticQuad(connectivity, startIndex, 0, 1, 2, 3, 8, 9, 10, 11))
						yield return qq;
					foreach (var qq in getFacesOfQuadraticQuad(connectivity, startIndex, 4, 5, 6, 7, 12, 13, 14, 15))
						yield return qq;
					foreach (var qq in getFacesOfQuadraticQuad(connectivity, startIndex, 0, 1, 5, 4, 8, 17, 12, 16))
						yield return qq;
					foreach (var qq in getFacesOfQuadraticQuad(connectivity, startIndex, 1, 2, 6, 5, 9, 18, 13, 17))
						yield return qq;
					foreach (var qq in getFacesOfQuadraticQuad(connectivity, startIndex, 2, 3, 7, 6, 10, 19, 14, 18))
						yield return qq;
					foreach (var qq in getFacesOfQuadraticQuad(connectivity, startIndex, 3, 0, 4, 7, 11, 16, 15, 19))
						yield return qq;
					break;
				default:

					throw new NotSupportedException();
			}
		}

		private static IEnumerable<TriangleFace> getFacesOfQuadraticTriangle(int[] connectivity, int startIndex, int n0, int n1, int n2, int n3, int n4, int n5)
		{
			yield return new TriangleFace(
				connectivity[startIndex + n0],
				connectivity[startIndex + n3],
				connectivity[startIndex + n5]);
			yield return new TriangleFace(
				connectivity[startIndex + n1],
				connectivity[startIndex + n4],
				connectivity[startIndex + n3]);
			yield return new TriangleFace(
				connectivity[startIndex + n2],
				connectivity[startIndex + n5],
				connectivity[startIndex + n4]);
			yield return new TriangleFace(
				connectivity[startIndex + n3],
				connectivity[startIndex + n4],
				connectivity[startIndex + n5]);
		}

		private static IEnumerable<TriangleFace> getFacesOfQuadraticQuad(int[] connectivity, int startIndex, int n0, int n1, int n2, int n3, int n4, int n5, int n6, int n7)
		{
			// four corner triangles
			yield return new TriangleFace(
				connectivity[startIndex + n0],
				connectivity[startIndex + n4],
				connectivity[startIndex + n7]);
			yield return new TriangleFace(
				connectivity[startIndex + n1],
				connectivity[startIndex + n5],
				connectivity[startIndex + n4]);
			yield return new TriangleFace(
				connectivity[startIndex + n2],
				connectivity[startIndex + n6],
				connectivity[startIndex + n5]);
			yield return new TriangleFace(
				connectivity[startIndex + n3],
				connectivity[startIndex + n7],
				connectivity[startIndex + n6]);
			// two inner triangles
			TriangleFace innerT1, innerT2;
			TriangleFace.SplitRectangleToTriangles(
						connectivity[startIndex + n4],
						connectivity[startIndex + n5],
						connectivity[startIndex + n6],
						connectivity[startIndex + n7],
						out innerT1, out innerT2);
			yield return innerT1;
			yield return innerT2;
		}

		#endregion
	}
}
