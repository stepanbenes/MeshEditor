using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.LayerManager.Data;
using MeshEditor.LayerManager.Filters;
using MeshEditor.Common.GeometryMarkers;
using System.Diagnostics;

namespace MeshEditor.LayerManager.MeshFiltering
{
	internal class MeshIsoSurfaceCreator : MeshSectionCreatorBase, IMeshFilterCreator
	{
		readonly IsoSurfaceFilter isoSurfaceFilter;
		readonly IDictionary<decimal, ComponentDataDescription> data;

		public MeshIsoSurfaceCreator(IsoSurfaceFilter isoSurfaceFilter, IDictionary<decimal, ComponentDataDescription> data)
		{
			// TODO: add check that all data have Points location
			this.isoSurfaceFilter = isoSurfaceFilter;
			this.data = data;
		}

		public IEnumerable<(GeometryDescription geometry, IReadOnlyList<decimal> timeSteps)> Create(GeometryDescription source, IEnumerable<decimal> timeSteps)
		{
			foreach(decimal timeStep in timeSteps)
			{
				var filteredGeometry = CreateForTimeStep(source, timeStep);
				yield return (geometry: filteredGeometry, timeSteps: new[] { timeStep });
			}
		}

		protected override IEnumerable<EdgeIntersection> GetAllIntersectionsOfCellEdgesWithPlane(GeometryDescription geometry, int cellIndex, decimal timeStep, out Vector3? planeNormal)
		{
			planeNormal = null;

			// iterate through all edges and interpolate values
			return getAllIntersectionsOfCellEdgesWithPlane();

			IEnumerable<EdgeIntersection> getAllIntersectionsOfCellEdgesWithPlane()
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

					double firstPointValue = getDataValue(timeStep, firstPointId);
					double secondPointValue = getDataValue(timeStep, secondPointId);

					if (valueIsInInterval(isoSurfaceFilter.Value, firstPointValue, secondPointValue, out float intersection))
					{
						yield return new EdgeIntersection(firstIndex, secondIndex, intersection);
					}
				}
			}
		}

		private double getDataValue(decimal timeStep, int pointId)
		{
			Debug.Assert(data.ContainsKey(timeStep));
			var component = data[timeStep];
			Debug.Assert(component.Location == DataLocationType.Points);
			return component.Values[pointId];
		}

		private static bool valueIsInInterval(double value, double a, double b, out float parameter)
		{
			double range = b - a;
			if (range == 0.0)
			{
				parameter = 0.0f;
				return false; /**/ // check whether value is equal to min?
			}
			parameter = (float)((value - a) / range);
			return value.CompareTo(a) != value.CompareTo(b);
		}

		private static bool valueIsInInterval(double value, double a, double b)
		{
			return value.CompareTo(a) != value.CompareTo(b);
		}
	}
}
