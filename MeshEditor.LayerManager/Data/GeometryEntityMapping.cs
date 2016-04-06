using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.LayerManager.Data
{
	public abstract class GeometryEntityMapping
	{

	}

	public sealed class ImportGeometryEntityMapping : GeometryEntityMapping
	{
		private readonly Dictionary<int, int> oldToNewPointIdMap;
		private readonly Dictionary<int, int> oldToNewCellIdMap;

		public ImportGeometryEntityMapping()
		{
			oldToNewPointIdMap = new Dictionary<int, int>();
			oldToNewCellIdMap = new Dictionary<int, int>();
		}

		public void AddPointMapping(int oldPointId, int newPointId)
		{
			oldToNewPointIdMap.Add(oldPointId, newPointId);
		}

		public void AddCellMapping(int oldCellId, int newCellId)
		{
			oldToNewCellIdMap.Add(oldCellId, newCellId);
		}

		public bool TryGetNewPointId(int oldPointId, out int newPointId)
		{
			return oldToNewPointIdMap.TryGetValue(oldPointId, out newPointId);
		}

		public bool TryGetNewCellId(int oldCellId, out int newCellId)
		{
			return oldToNewCellIdMap.TryGetValue(oldCellId, out newCellId);
		}
	}

	internal sealed class FilterGeometryEntityMapping : GeometryEntityMapping
	{
		private readonly Dictionary<int, int> newToOldPointIdMap;
		private readonly Dictionary<int, int> newToOldCellIdMap;
		private readonly Dictionary<int, EdgeIntersection> newPointIdToOldEdgeIntersectionMap;

		// private readonly Dictionary<int, int> newToOldCellPointIdMap;
		// private readonly Dictionary<int, EdgeIntersection> newCellPointIdToOldEdgeIntersectionMap; // edge has two cell point indexes
		// TODO: add methods for filling dictionaries, do not pass it through ctor, e.g., newToOldPointIdMap and newCellPointIdToOldEdgeIntersectionMap has to be entangled

		public FilterGeometryEntityMapping()
		{
			newToOldPointIdMap = new Dictionary<int, int>();
			newToOldCellIdMap = new Dictionary<int, int>();
			newPointIdToOldEdgeIntersectionMap = new Dictionary<int, EdgeIntersection>();
		}

		public void AddPointMapping(int newPointId, int oldPointId)
		{
			newToOldPointIdMap.Add(newPointId, oldPointId);
		}

		public void AddCellMapping(int newCellId, int oldCellId)
		{
			newToOldCellIdMap.Add(newCellId, oldCellId);
		}

		public void AddPointEdgeMapping(int newPointId, EdgeIntersection edgeIntersection)
		{
			newPointIdToOldEdgeIntersectionMap.Add(newPointId, edgeIntersection);
		}

		public bool TryGetOldPointId(int newPointId, out int oldPointId)
		{
			return newToOldPointIdMap.TryGetValue(newPointId, out oldPointId);
		}

		public bool TryGetOldCellId(int newCellId, out int oldCellId)
		{
			return newToOldCellIdMap.TryGetValue(newCellId, out oldCellId);
		}

		public bool TryGetOldEdgeIntersection(int newPointId, out EdgeIntersection edgeIntersection)
		{
			return newPointIdToOldEdgeIntersectionMap.TryGetValue(newPointId, out edgeIntersection);
		}
	}
}
