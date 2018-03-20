using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.LayerManager.Data
{
	public interface IGeometryEntityMapping
	{
		void AddPointMapping(int from, int to);
		void AddCellMapping(int from, int to);
		bool TryMapPoint(int from, out int to);
		bool TryMapCell(int from, out int to);
	}

	public interface IFilterGeometryEntityMapping : IGeometryEntityMapping
	{
		void AddCellPointMapping(int from, int to);
		void AddPointEdgeMapping(int point, EdgeIntersection edgeIntersection);
		void AddCellPointEdgeMapping(int cell, EdgeIntersection edgeIntersection);
		bool TryMapCellPoint(int from, out int to);
		bool TryMapPointEdgeIntersection(int point, out EdgeIntersection edgeIntersection);
		bool TryMapCellPointEdgeIntersection(int cell, out EdgeIntersection edgeIntersection);
	}

	public class GeometryEntityMapping : IGeometryEntityMapping
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

		public Dictionary<int, int> GenerateReversedPointMapping()
		{
			var reversedPointMapping = new Dictionary<int, int>();
			foreach (var kvp in pointMap)
			{
				reversedPointMapping.Add(kvp.Value, kvp.Key);
			}
			return reversedPointMapping;
		}

		public Dictionary<int, int> GenerateReversedCellMapping()
		{
			var reversedCellMapping = new Dictionary<int, int>();
			foreach (var kvp in cellMap)
			{
				reversedCellMapping.Add(kvp.Value, kvp.Key);
			}
			return reversedCellMapping;
		}
	}

	sealed class FilterGeometryEntityMapping : GeometryEntityMapping, IFilterGeometryEntityMapping
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

	sealed class IdentityGeometryEntityMapping : IFilterGeometryEntityMapping
	{
		public void AddCellMapping(int from, int to) => throw new NotSupportedException();

		public void AddCellPointEdgeMapping(int cell, EdgeIntersection edgeIntersection) => throw new NotSupportedException();

		public void AddCellPointMapping(int from, int to) => throw new NotSupportedException();

		public void AddPointEdgeMapping(int point, EdgeIntersection edgeIntersection) => throw new NotSupportedException();

		public void AddPointMapping(int from, int to) => throw new NotSupportedException();

		public bool TryMapCell(int from, out int to)
		{
			to = from;
			return true;
		}

		public bool TryMapCellPoint(int from, out int to)
		{
			to = from;
			return true;
		}

		public bool TryMapCellPointEdgeIntersection(int cell, out EdgeIntersection edgeIntersection) => throw new NotSupportedException();

		public bool TryMapPoint(int from, out int to)
		{
			to = from;
			return true;
		}

		public bool TryMapPointEdgeIntersection(int point, out EdgeIntersection edgeIntersection) => throw new NotSupportedException();
	}
}
