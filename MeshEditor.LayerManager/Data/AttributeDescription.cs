using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.LayerManager.Data
{
	public class AttributeDescription
	{
		public static class KnownAttributeNames
		{
			public static readonly string ElementProperty = "ElementProperty";
		}

		public string Name { get; set; }
		public DataLocationType Location { get; set; }
		public int[] Values { get; set; }
	}
}
