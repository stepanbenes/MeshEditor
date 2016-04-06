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
		public abstract bool TryGetNewPointId(int oldPointId, out int newPointId);
		public abstract bool TryGetNewCellId(int oldCellId, out int newCellId);

		public abstract bool TryGetOldPointId(int newPointId, out int oldPointId);
		public abstract bool TryGetOldCellId(int newCellId, out int oldCellId);
		public abstract void AssignNewPointIdToOldEdgeIntersection(int newPointId, EdgeIntersection edgeIntersection);
		public abstract bool TryGetOldEdgeIntersection(int newPointId, out EdgeIntersection edgeIntersection);
	}

	internal sealed class ImportGeometryEntityMapping : GeometryEntityMapping
	{
		private readonly Dictionary<int, int> oldToNewPointIdMap;
		private readonly Dictionary<int, int> oldToNewCellIdMap;

		public ImportGeometryEntityMapping(Dictionary<int, int> oldToNewPointIdMap, Dictionary<int, int> oldToNewCellIdMap)
		{
			Debug.Assert(oldToNewPointIdMap != null);
			Debug.Assert(oldToNewCellIdMap != null);
			this.oldToNewPointIdMap = oldToNewPointIdMap;
			this.oldToNewCellIdMap = oldToNewCellIdMap;
		}

		public override bool TryGetNewPointId(int oldPointId, out int newPointId)
		{
			return oldToNewPointIdMap.TryGetValue(oldPointId, out newPointId);
		}

		public override bool TryGetNewCellId(int oldCellId, out int newCellId)
		{
			return oldToNewCellIdMap.TryGetValue(oldCellId, out newCellId);
		}

		public override bool TryGetOldPointId(int newPointId, out int oldPointId)
		{
			throw new NotSupportedException();
		}

		public override bool TryGetOldCellId(int newCellId, out int oldCellId)
		{
			throw new NotSupportedException();
		}

		public override void AssignNewPointIdToOldEdgeIntersection(int newPointId, EdgeIntersection edgeIntersection)
		{
			throw new NotSupportedException();
		}

		public override bool TryGetOldEdgeIntersection(int newPointId, out EdgeIntersection edgeIntersection)
		{
			throw new NotSupportedException();
		}
	}

	internal sealed class FilterGeometryEntityMapping : GeometryEntityMapping
	{
		private readonly Dictionary<int, int> newToOldPointIdMap;
		private readonly Dictionary<int, int> newToOldCellIdMap;
		private readonly Dictionary<int, EdgeIntersection> newPointIdToOldEdgeIntersectionMap;

		public FilterGeometryEntityMapping(Dictionary<int, int> newToOldPointIdMap, Dictionary<int, int> newToOldCellIdMap)
		{
			Debug.Assert(newToOldPointIdMap != null);
			Debug.Assert(newToOldCellIdMap != null);
			this.newToOldPointIdMap = newToOldPointIdMap;
			this.newToOldCellIdMap = newToOldCellIdMap;
			newPointIdToOldEdgeIntersectionMap = new Dictionary<int, EdgeIntersection>();
		}

		public override bool TryGetNewCellId(int oldCellId, out int newCellId)
		{
			throw new NotSupportedException();
		}

		public override bool TryGetNewPointId(int oldPointId, out int newPointId)
		{
			throw new NotSupportedException();
		}

		public override bool TryGetOldPointId(int newPointId, out int oldPointId)
		{
			return newToOldPointIdMap.TryGetValue(newPointId, out oldPointId);
		}

		public override bool TryGetOldCellId(int newCellId, out int oldCellId)
		{
			return newToOldCellIdMap.TryGetValue(newCellId, out oldCellId);
		}

		public override void AssignNewPointIdToOldEdgeIntersection(int newPointId, EdgeIntersection edgeIntersection)
		{
			newPointIdToOldEdgeIntersectionMap.Add(newPointId, edgeIntersection);
		}

		public override bool TryGetOldEdgeIntersection(int newPointId, out EdgeIntersection edgeIntersection)
		{
			return newPointIdToOldEdgeIntersectionMap.TryGetValue(newPointId, out edgeIntersection);
		}
	}
}
