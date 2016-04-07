using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.LayerManager.Filters
{
	internal class SliceFilter : FilterBase
	{
		public override FilterType Type => FilterType.Slice;

		public float NormalX { get; set; }
		public float NormalY { get; set; }
		public float NormalZ { get; set; }

		public float Offset { get; set; }

		// int Count, float Step - or - float[] Offsets
	}

	//public class MultiSliceFilter : FilterBase
	//{
	//	SliceFilter[] slices;
	//}
}
