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
	internal class MeshSliceCreator : MeshSectionCreatorBase, IMeshFilterCreator
	{
		readonly SliceFilter sliceFilter;

		public MeshSliceCreator(SliceFilter sliceFilter)
		{
			Debug.Assert(sliceFilter != null);
			this.sliceFilter = sliceFilter;
		}

		public IEnumerable<(GeometryDescription geometry, IReadOnlyList<decimal> timeSteps)> Create(GeometryDescription source, IEnumerable<decimal> timeSteps)
		{
			GeometryDescription slice = CreateForTimeStep(source, timeStep: 0m /*ignore timeSteps*/);
			return Enumerable.Repeat<(GeometryDescription geometry, IReadOnlyList<decimal> timeSteps)>((geometry: slice, timeSteps: timeSteps.ToList()), 1);
		}

		#region Overrides

		protected override IEnumerable<EdgeIntersection> GetAllIntersectionsOfCellEdgesWithPlane(GeometryDescription geometry, int cellIndex, decimal timeStep, out Vector3? planeNormal)
		{
			float planeOffset = sliceFilter.Offset;
			Vector3 normal = new Vector3(sliceFilter.NormalX, sliceFilter.NormalY, sliceFilter.NormalZ);
			normal.Normalize();
			planeNormal = normal;

			float minDistance = float.MaxValue, maxDistance = float.MinValue;
			foreach (Vector3 cellPoint in EnumerateCellPoints(geometry, cellIndex))
			{
				float distance = Vector3.Dot(cellPoint, normal);
				minDistance = Math.Min(minDistance, distance);
				maxDistance = Math.Max(maxDistance, distance);
			}

			if (planeOffset < minDistance || planeOffset > maxDistance)
			{
				return Enumerable.Empty<EdgeIntersection>();
			}

			return getAllIntersectionsOfCellEdgesWithPlane(normal, planeOffset);

			IEnumerable<EdgeIntersection> getAllIntersectionsOfCellEdgesWithPlane(Vector3 n, float offset)
			{
				var processedEdges = new HashSet<EdgeMark>();
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
					if (ComputationalGeometryMath.LinePlaneIntersection(firstPoint, secondPoint, ref n, offset, out float intersection))
					{
						yield return new EdgeIntersection(firstIndex, secondIndex, intersection);
					}
				}
			}
		}

		#endregion
	}
}
