using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.LayerManager.Data;
using MeshEditor.LayerManager.Filters;
using MeshEditor.Common.GeometryMarkers;

namespace MeshEditor.LayerManager.MeshFiltering
{
	internal class MeshIsoSurfaceCreator : MeshSectionCreatorBase
	{
		readonly IsoSurfaceFilter isoSurfaceFilter;
		readonly IDictionary<decimal, List<ComponentDataDescription>> data;

		public MeshIsoSurfaceCreator(IsoSurfaceFilter isoSurfaceFilter, IDictionary<decimal, List<ComponentDataDescription>> data)
		{
			this.isoSurfaceFilter = isoSurfaceFilter;
			this.data = data;
		}

		protected override IEnumerable<EdgeIntersection> GetAllIntersectionsOfCellEdgesWithPlane(GeometryDescription geometry, int cellIndex, out Vector3 intersectionPlaneNormal)
		{
			throw new NotImplementedException();
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
