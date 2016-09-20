using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.LayerManager.Data
{
	public class GeometryEntityMapping
	{
		private readonly Dictionary<int, int> pointMap;
		private readonly Dictionary<int, int> cellMap;

		public GeometryEntityMapping()
		{
			pointMap = new Dictionary<int, int>();
			cellMap = new Dictionary<int, int>();
		}

		public void AddPointMapping(int from, int to) => pointMap.Add(from, to);

		public void AddCellMapping(int from, int to) => cellMap.Add(from, to);

		public bool TryMapPoint(int from, out int to) => pointMap.TryGetValue(from, out to);

		public bool TryMapCell(int from, out int to) => cellMap.TryGetValue(from, out to);
	}

	internal sealed class FilterGeometryEntityMapping : GeometryEntityMapping
	{
		private readonly Dictionary<int, int> cellPointMap;

		private readonly Dictionary<int, EdgeIntersection> pointEdgeIntersectionMap;
		private readonly Dictionary<int, EdgeIntersection> cellPointEdgeIntersectionMap;

		public FilterGeometryEntityMapping()
		{
			cellPointMap = new Dictionary<int, int>();
			pointEdgeIntersectionMap = new Dictionary<int, EdgeIntersection>();
			cellPointEdgeIntersectionMap = new Dictionary<int, EdgeIntersection>();
		}

		public void AddCellPointMapping(int from, int to) => cellPointMap.Add(from, to);

		public void AddPointEdgeMapping(int point, EdgeIntersection edgeIntersection) => pointEdgeIntersectionMap.Add(point, edgeIntersection);

		public void AddCellPointEdgeMapping(int cell, EdgeIntersection edgeIntersection) => cellPointEdgeIntersectionMap.Add(cell, edgeIntersection);

		public bool TryMapCellPoint(int from, out int to) => cellPointMap.TryGetValue(from, out to);

		public bool TryMapPointEdgeIntersection(int point, out EdgeIntersection edgeIntersection) => pointEdgeIntersectionMap.TryGetValue(point, out edgeIntersection);

		public bool TryMapCellPointEdgeIntersection(int cell, out EdgeIntersection edgeIntersection) => cellPointEdgeIntersectionMap.TryGetValue(cell, out edgeIntersection);
	}
}
