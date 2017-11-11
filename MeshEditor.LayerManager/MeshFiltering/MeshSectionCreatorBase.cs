using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.LayerManager.Data;

namespace MeshEditor.LayerManager.MeshFiltering
{
	internal abstract class MeshSectionCreatorBase : IMeshFilterCreator
	{
		public IEnumerable<(GeometryDescription geometry, List<decimal> timeSteps)> Create(GeometryDescription geometry, IEnumerable<decimal> timeSteps)
		{
			GeometryBuilder geometryBuilder = new GeometryBuilder(geometry.NumberOfCoordinateComponents, mergeOverlappingPoints: true);

			for (int cellIndex = 0; cellIndex < geometry.NumberOfCells; cellIndex++)
			{
				if (!getIntersectionsWithElement(geometry, cellIndex, out List<EdgeIntersection> intersectionInfoList, out Vector3[] intersections, out Vector3 intersectionPlaneNormal))
					continue;

				Debug.Assert(intersectionInfoList.Count == intersections.Length);

				if (intersections.Length == 2)
				{
					if (GeometryDescription.GetDimensionOfCellType(geometry.CellTypes[cellIndex]) == 2) // 2D cell
					{
						int point1 = geometryBuilder.AddPoint(intersections[0], convertCellPointsToPointsInEdgeIntersection(geometry, intersectionInfoList[0]));
						int point2 = geometryBuilder.AddPoint(intersections[1], convertCellPointsToPointsInEdgeIntersection(geometry, intersectionInfoList[1]));
						geometryBuilder.AddCell(CellType.LineLinear, cellIndex, new[] { point1, point2 }, new[] { intersectionInfoList[0], intersectionInfoList[1] });
					}
					continue;
				}

				Debug.Assert(intersections.Length > 2);

				Vector3 pivot = Vector3.Zero;
				for (int i = 0; i < intersections.Length; i++)
				{
					pivot += intersections[i];
				}
				pivot /= (float)intersections.Length;

				Vector3 firstVector = intersections[0] - pivot;

				if (firstVector == Vector3.Zero) // all intersections are same, do not cut element - section plane incides only with one point in element
					continue; // TODO: vyresit nulovy vektor nebo blizky nule

				firstVector.Normalize();
				float[] intersectionAngles = new float[intersections.Length];
				intersectionAngles[0] = 0f;
				for (int i = 1; i < intersections.Length; i++)
				{
					Vector3 secondVector = intersections[i] - pivot;
					if (secondVector == Vector3.Zero) // TODO: vyresit nulovy vektor nebo blizky nule
						continue;
					secondVector.Normalize();
					float intersectionAngle = ComputationalGeometryMath.GetAngleInDegreesBetweenUnitVectors_0_360(firstVector, secondVector, intersectionPlaneNormal);
					intersectionAngles[i] = intersectionAngle;
				}

				int[] intersectionIndices = new int[intersections.Length];
				for (int i = 0; i < intersectionIndices.Length; i++)
				{
					intersectionIndices[i] = i;
				}

				Comparison<int> compareAngles = (index1, index2) => intersectionAngles[index1].CompareTo(intersectionAngles[index2]);

				Array.Sort(intersectionIndices, compareAngles);

				int[] connectivityIndices = new int[intersections.Length];
				for (int i = 0; i < intersectionIndices.Length; i++)
				{
					connectivityIndices[i] = geometryBuilder.AddPoint(intersections[intersectionIndices[i]], convertCellPointsToPointsInEdgeIntersection(geometry, intersectionInfoList[intersectionIndices[i]]));
				}

				for (int i = 1; i < intersections.Length - 1; i++)
				{
					geometryBuilder.AddCell(CellType.TriangleLinear, cellIndex, new[] { connectivityIndices[0], connectivityIndices[i], connectivityIndices[i + 1] }, new[] { intersectionInfoList[intersectionIndices[0]], intersectionInfoList[intersectionIndices[i]], intersectionInfoList[intersectionIndices[i + 1]] });
					geometryBuilder.AddEdge(connectivityIndices[i], connectivityIndices[i + 1], faceAngle: 0f);
				}

				geometryBuilder.AddEdge(connectivityIndices[0], connectivityIndices[1], faceAngle: 0f);
				geometryBuilder.AddEdge(connectivityIndices[connectivityIndices.Length - 1], connectivityIndices[0], faceAngle: 0f);

			} // end of element loop

			GeometryDescription slice = geometryBuilder.Build();

			return new[] { (slice, timeSteps.ToList()) };
		}
		
		protected static readonly Dictionary<CellType, int[]> EdgePointIndexMap = new Dictionary<CellType, int[]>
		{
			// TODO: handle better edges of quadratic elements
			[CellType.Point] = new int[] { },
			[CellType.LineLinear] = new int[] { 0, 1 },
			[CellType.LineQuadratic] = new int[] { 0, 1 },
			[CellType.TriangleLinear] = new int[] { 0, 1, 1, 2, 2, 0 },
			[CellType.TriangleQuadratic] = new int[] { 0, 1, 1, 2, 2, 0 },
			[CellType.QuadLinear] = new int[] { 0, 1, 1, 2, 2, 3, 3, 0 },
			[CellType.QuadQuadratic] = new int[] { 0, 1, 1, 2, 2, 3, 3, 0 },
			[CellType.TetraLinear] = new int[] { 0, 1, 1, 2, 2, 0, 0, 3, 1, 3, 2, 3 },
			[CellType.TetraQuadratic] = new int[] { 0, 1, 1, 2, 2, 0, 0, 3, 1, 3, 2, 3 },
			[CellType.WedgeLinear] = new int[] { 0, 1, 1, 2, 2, 0, 3, 4, 4, 5, 5, 3, 0, 3, 1, 4, 2, 5 },
			[CellType.WedgeQuadratic] = new int[] { 0, 1, 1, 2, 2, 0, 3, 4, 4, 5, 5, 3, 0, 3, 1, 4, 2, 5 },
			[CellType.HexaLinear] = new int[] { 0, 1, 1, 2, 2, 3, 3, 0, 4, 5, 5, 6, 6, 7, 7, 4, 0, 4, 1, 5, 2, 6, 3, 7 },
			[CellType.HexaQuadratic] = new int[] { 0, 1, 1, 2, 2, 3, 3, 0, 4, 5, 5, 6, 6, 7, 7, 4, 0, 4, 1, 5, 2, 6, 3, 7 },
		};

		protected abstract IEnumerable<EdgeIntersection> GetAllIntersectionsOfCellEdgesWithPlane(GeometryDescription geometry, int cellIndex, out Vector3 intersectionPlaneNormal);

		protected static IEnumerable<Vector3> EnumerateCellPoints(GeometryDescription geometry, int cellIndex)
		{
			int previousOffset = (cellIndex > 0) ? geometry.CellOffsets[cellIndex - 1] : 0;
			int currentOffset = geometry.CellOffsets[cellIndex];
			for (int offset = previousOffset; offset < currentOffset; offset++)
			{
				yield return MeshFilterCreatorHelper.GetPointCoordinates(geometry, geometry.CellConnectivity[offset]);
			}
		}

		private bool getIntersectionsWithElement(GeometryDescription geometry, int cellIndex, out List<EdgeIntersection> intersectionInfoList, out Vector3[] intersections, out Vector3 intersectionPlaneNormal)
		{
			intersectionInfoList = new List<EdgeIntersection>(GetAllIntersectionsOfCellEdgesWithPlane(geometry, cellIndex, out intersectionPlaneNormal));

			if (intersectionInfoList.Count < 2)
			{
				intersections = null;
				return false;
			}

			intersections = new Vector3[intersectionInfoList.Count];
			for (int i = 0; i < intersectionInfoList.Count; i++)
			{
				EdgeIntersection edgeIntersection = convertCellPointsToPointsInEdgeIntersection(geometry, intersectionInfoList[i]);
				intersections[i] = getIntersectionPoint(geometry, edgeIntersection);
			}

			if (intersections.Length < 2)
			{
				return false;
			}

			return true;
		}

		private static EdgeIntersection convertCellPointsToPointsInEdgeIntersection(GeometryDescription geometry, EdgeIntersection cellPointEdgeIntersection)
		{
			return new EdgeIntersection(
				geometry.CellConnectivity[cellPointEdgeIntersection.FirstPointId],
				geometry.CellConnectivity[cellPointEdgeIntersection.SecondPointId],
				cellPointEdgeIntersection.Coordinate);
		}

		private static Vector3 getIntersectionPoint(GeometryDescription geometry, EdgeIntersection edgeIntersection)
		{
			Vector3 v1 = MeshFilterCreatorHelper.GetPointCoordinates(geometry, edgeIntersection.FirstPointId);
			Vector3 v2 = MeshFilterCreatorHelper.GetPointCoordinates(geometry, edgeIntersection.SecondPointId);
			Vector3 result;
			Vector3.Subtract(ref v2, ref v1, out result);
			Vector3.Multiply(ref result, edgeIntersection.Coordinate, out result);
			Vector3.Add(ref v1, ref result, out result);
			return result;
		}
	}
}
