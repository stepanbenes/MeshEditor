using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.LayerManager.Data;
using MeshEditor.LayerManager.Filters;

namespace MeshEditor.LayerManager.MeshFiltering
{
	/// <summary>
	/// Planar cross-section generator
	/// Based on implementation in MeshEditor.DataVisualizer.Layers.CrossSection.updateGeometry()
	/// </summary>
	internal class MeshSliceGenerator
	{
		private GeometryDescription geometry;

		public MeshSliceGenerator(GeometryDescription geometry)
		{
			Debug.Assert(geometry != null);
			this.geometry = geometry;
		}

		public GeometryDescription CreateSlice(SliceFilter sliceFilter)
		{
			GeometryBuilder geometryBuilder = new GeometryBuilder(geometry.NumberOfCoordinateComponents);

			Vector3 planeNormal = new Vector3(sliceFilter.NormalX, sliceFilter.NormalY, sliceFilter.NormalZ);
			planeNormal.Normalize();

			for (int cellIndex = 0; cellIndex < geometry.NumberOfCells; cellIndex++)
			{
				List<EdgeIntersection> intersectionInfoList;
				Vector3[] intersections;
				if (!getIntersectionsWithElement(cellIndex, planeNormal, sliceFilter.Offset, out intersectionInfoList, out intersections))
					continue;

				int[] indices;

				//if (intersections.Length == 2)
				//{
				//	if (element is Element2D)
				//	{
				//		indices = new int[] { 0, 1 };

				//		lineVertexIndices.Add(vertices.Count);
				//		//vertices.Add(intersections[0]);
				//		addIntersectionVertex(vertices, intersections, intersectionInfoList, indices, 0);

				//		lineVertexIndices.Add(vertices.Count);
				//		//vertices.Add(intersections[1]);
				//		addIntersectionVertex(vertices, intersections, intersectionInfoList, indices, 1);
				//	}
				//	continue;
				//}

				Vector3 pivot = Vector3.Zero;
				for (int i = 0; i < intersections.Length; i++)
				{
					pivot += intersections[i];
				}
				pivot /= (float)intersections.Length;

				//float[] intersectionAngles = new float[intersections.Count];
				Vector3 firstVector = intersections[0] - pivot;

				if (firstVector == Vector3.Zero) // all intersections are same, do not cut element - section plane incides only with one point in element
					continue; // TODO: vyresit nulovy vektor nebo blizky nule

				firstVector.Normalize();
				float[] intersectionAngles = new float[intersections.Length];
				intersectionAngles[0] = 0f;
				for (int i = 1; i < intersections.Length; i++)
				{
					Vector3 secondVector = intersections[i] - pivot;
					//System.Diagnostics.Debug.Assert(secondVector != Vector3.Zero);
					if (secondVector == Vector3.Zero) // TODO: vyresit nulovy vektor nebo blizky nule
						continue;
					secondVector.Normalize();
					float intersectionAngle = ComputationalGeometry.GetAngleInDegreesBetweenUnitVectors_0_360(firstVector, secondVector, planeNormal);
					intersectionAngles[i] = intersectionAngle;
				}

				indices = new int[intersections.Length];
				for (int i = 0; i < indices.Length; i++)
				{
					indices[i] = i;
				}

				Comparison<int> compareAngles = (index1, index2) => intersectionAngles[index1].CompareTo(intersectionAngles[index2]);

				Array.Sort(indices, compareAngles);

				//int firstIndex = vertices.Count;

				// add vertexes to vertex list and add indexes of edge vertexes to edgeVertexIndices list
				for (int i = 1; i < intersections.Length - 1; i++)
				{
					int point1 = geometryBuilder.AddPoint(intersections[indices[0]]);
					int point2 = geometryBuilder.AddPoint(intersections[indices[i]]);
					int point3 = geometryBuilder.AddPoint(intersections[indices[i + 1]]);

					geometryBuilder.AddCell(CellType.TriangleLinear, point1, point2, point3);

					//triangleVertexIndices.Add(vertices.Count);
					//addIntersectionVertex(vertices, intersections, intersectionInfoList, indices, 0);

					//edgeVertexIndices.Add(vertices.Count);
					//triangleVertexIndices.Add(vertices.Count);
					//addIntersectionVertex(vertices, intersections, intersectionInfoList, indices, i);
					//edgeVertexIndices.Add(vertices.Count);
					//triangleVertexIndices.Add(vertices.Count);
					//addIntersectionVertex(vertices, intersections, intersectionInfoList, indices, i + 1);
				}

				//int lastIndex = vertices.Count - 1;

				//edgeVertexIndices.Add(lastIndex);
				//edgeVertexIndices.Add(firstIndex);
				//edgeVertexIndices.Add(firstIndex);
				//edgeVertexIndices.Add(firstIndex + 1);

				//crossedElements.Add(new ElementCountPair(element, lastIndex - firstIndex + 1));

			} // end of element loop

			GeometryDescription slice = geometryBuilder.Build();
			slice.Mapping = new FilterGeometryEntityMapping(); /**/
			return slice;
		}

		#region Private methods

		private IEnumerable<Vector3> enumerateCellPoints(int cellIndex)
		{
			int previousOffset = (cellIndex > 0) ? geometry.CellOffsets[cellIndex - 1] : 0;
			int currentOffset = geometry.CellOffsets[cellIndex];
			for (int offset = previousOffset; offset < currentOffset; offset++)
			{
				yield return getPointCoordinates(geometry.CellConnectivity[offset]);
			}
		}

		private Vector3 getPointCoordinates(int pointIndex)
		{
			float x = geometry.PointCoordinates[pointIndex * geometry.NumberOfCoordinateComponents + 0];
			float y = (geometry.NumberOfCoordinateComponents > 1) ? geometry.PointCoordinates[pointIndex * geometry.NumberOfCoordinateComponents + 1] : 0f;
			float z = (geometry.NumberOfCoordinateComponents > 2) ? geometry.PointCoordinates[pointIndex * geometry.NumberOfCoordinateComponents + 2] : 0f;
			return new Vector3(x, y, z);
		}

		private Vector3 getIntersectionPoint(EdgeIntersection edgeIntersection)
		{
			Vector3 v1 = getPointCoordinates(edgeIntersection.FirstPointId);
			Vector3 v2 = getPointCoordinates(edgeIntersection.SecondPointId);
			Vector3 result;
			Vector3.Subtract(ref v2, ref v1, out result);
			Vector3.Multiply(ref result, edgeIntersection.Coordinate, out result);
			Vector3.Add(ref v1, ref result, out result);
			return result;
		}

		private static readonly Dictionary<CellType, int[]> edgePointIndexMap = new Dictionary<CellType, int[]>
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

		private IEnumerable<EdgeIntersection> getAllIntersectionsOfCellEdgesWithPlane(int cellIndex, Vector3 planeNormal, float planeOffset)
		{
			int[] edgePointIndexArray = edgePointIndexMap[geometry.CellTypes[cellIndex]];
			int baseOffset = (cellIndex > 0) ? geometry.CellOffsets[cellIndex - 1] : 0;
			for (int i = 0; i < edgePointIndexArray.Length; i += 2)
			{
				int firstIndex = geometry.CellConnectivity[baseOffset + edgePointIndexArray[i]];
				int secondIndex = geometry.CellConnectivity[baseOffset + edgePointIndexArray[i + 1]];
				Vector3 firstPoint = getPointCoordinates(firstIndex);
				Vector3 secondPoint = getPointCoordinates(secondIndex);
				float intersection;
				if (ComputationalGeometry.LinePlaneIntersection(firstPoint, secondPoint, ref planeNormal, planeOffset, out intersection))
				{
					yield return new EdgeIntersection(firstIndex, secondIndex, intersection);
				}
			}
		}

		private bool getIntersectionsWithElement(int cellIndex, Vector3 planeNormal, float planeOffset, out List<EdgeIntersection> intersectionInfoList, out Vector3[] intersections)
		{
			float minDistance = float.MaxValue, maxDistance = float.MinValue;
			foreach (Vector3 cellPoint in enumerateCellPoints(cellIndex))
			{
				float distance = Vector3.Dot(cellPoint, planeNormal);
				minDistance = Math.Min(minDistance, distance);
				maxDistance = Math.Max(maxDistance, distance);
			}

			if (planeOffset < minDistance || planeOffset > maxDistance)
			{
				intersectionInfoList = null;
				intersections = null;
				return false;
			}

			intersectionInfoList = new List<EdgeIntersection>(getAllIntersectionsOfCellEdgesWithPlane(cellIndex, planeNormal, planeOffset /* + Common.EpsilonF */));

			if (intersectionInfoList.Count < 2)
			{
				intersections = null;
				return false;
			}

			HashSet<Vector3> hashTest = new HashSet<Vector3>();
			intersections = new Vector3[intersectionInfoList.Count];
			int uniqueCount = 0;
			for (int i = 0; i < intersectionInfoList.Count; i++)
			{
				Vector3 temp = getIntersectionPoint(intersectionInfoList[i]);
				if (hashTest.Add(temp)) // check if already exists
				{
					intersections[uniqueCount++] = temp;
				}
				else // if exists, remove duplicate
				{
					intersectionInfoList.RemoveAt(i--);
				}
			}

			if (uniqueCount != intersections.Length)
			{
				Array.Resize(ref intersections, uniqueCount); // trim excess
			}

			if (intersections.Length < 2)
			{
				return false;
			}

			return true;
		}

		#endregion
	}
}
