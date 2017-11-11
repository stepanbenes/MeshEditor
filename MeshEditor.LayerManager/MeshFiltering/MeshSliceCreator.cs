using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.LayerManager.Data;
using MeshEditor.LayerManager.Filters;
using MeshEditor.Common.GeometryMarkers;

namespace MeshEditor.LayerManager.MeshFiltering
{
	/// <summary>
	/// Planar cross-section generator
	/// Based on implementation in MeshEditor.DataVisualizer.Layers.CrossSection.updateGeometry()
	/// </summary>
	internal class MeshSliceCreator : MeshSectionCreatorBase
	{
		readonly SliceFilter sliceFilter;

		public MeshSliceCreator(SliceFilter sliceFilter)
		{
			Debug.Assert(sliceFilter != null);
			this.sliceFilter = sliceFilter;
		}

		#region Overrides

		protected override IEnumerable<EdgeIntersection> GetAllIntersectionsOfCellEdgesWithPlane(GeometryDescription geometry, int cellIndex, out Vector3 intersectionPlaneNormal)
		{
			float planeOffset = sliceFilter.Offset;
			Vector3 planeNormal = new Vector3(sliceFilter.NormalX, sliceFilter.NormalY, sliceFilter.NormalZ);
			planeNormal.Normalize();
			intersectionPlaneNormal = planeNormal;

			float minDistance = float.MaxValue, maxDistance = float.MinValue;
			foreach (Vector3 cellPoint in EnumerateCellPoints(geometry, cellIndex))
			{
				float distance = Vector3.Dot(cellPoint, planeNormal);
				minDistance = Math.Min(minDistance, distance);
				maxDistance = Math.Max(maxDistance, distance);
			}

			if (planeOffset < minDistance || planeOffset > maxDistance)
			{
				return Enumerable.Empty<EdgeIntersection>();
			}

			return getAllIntersectionsOfCellEdgesWithPlane(planeNormal, planeOffset);

			IEnumerable<EdgeIntersection> getAllIntersectionsOfCellEdgesWithPlane(Vector3 normal, float offset)
			{
				var processedEdges = new HashSet<EdgeMark>(); // TODO: use EdgeMark struct in Core assembly
				int[] edgePointIndexArray = EdgePointIndexMap[geometry.CellTypes[cellIndex]];
				int baseOffset = (cellIndex > 0) ? geometry.CellOffsets[cellIndex - 1] : 0;
				for (int i = 0; i < edgePointIndexArray.Length; i += 2)
				{
					int firstIndex = baseOffset + edgePointIndexArray[i];
					int secondIndex = baseOffset + edgePointIndexArray[i + 1];
					int firstPointId = geometry.CellConnectivity[firstIndex];
					int secondPointId = geometry.CellConnectivity[secondIndex];
					var edgeMark = new EdgeMark(firstPointId, secondPointId);
					if (processedEdges.Contains(edgeMark))
					{
						continue;
					}
					else
					{
						processedEdges.Add(edgeMark);
					}
					Vector3 firstPoint = MeshFilterCreatorHelper.GetPointCoordinates(geometry, firstPointId);
					Vector3 secondPoint = MeshFilterCreatorHelper.GetPointCoordinates(geometry, secondPointId);
					if (ComputationalGeometryMath.LinePlaneIntersection(firstPoint, secondPoint, ref normal, offset, out float intersection))
					{
						yield return new EdgeIntersection(firstIndex, secondIndex, intersection);
					}
				}
			}
		}

		#endregion
	}
}
