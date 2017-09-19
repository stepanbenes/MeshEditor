using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.LayerManager.Data
{
	public abstract class DataDescription
	{
		public string FieldName { get; set; }
		public decimal TimeStep { get; set; }
		public DataLocationType Location { get; set; }
		public double[] Values { get; set; }

		public abstract int NumberOfComponents { get; }
		public abstract string GetComponentName(int index);
	}

	public class FieldDataDescription : DataDescription
	{
		public override int NumberOfComponents => ComponentNames.Length;

		public FieldType FieldType { get; set; }
		public string[] ComponentNames { get; set; }

		public override string GetComponentName(int index)
		{
			Debug.Assert(ComponentNames != null);
			if (index < 0 || index >= NumberOfComponents)
			{
				throw new ArgumentOutOfRangeException(nameof(index), "Index must be non-negative and less then number of components.");
			}
			return ComponentNames[index];
		}
	}

	public class ComponentDataDescription : DataDescription
	{
		public override int NumberOfComponents => 1;

		public string ComponentName { get; set; }

		public override string GetComponentName(int index)
		{
			if (index != 0)
			{
				throw new ArgumentOutOfRangeException(nameof(index), "There is only one component, index has to be zero.");
			}
			return ComponentName;
		}
	}
}
