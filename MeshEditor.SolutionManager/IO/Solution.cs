using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MeshEditor.LayerManager.Filters;
using Newtonsoft.Json;

namespace MeshEditor.SolutionManager.IO
{
	class Solution : SolutionBase
	{
		public class Layer : ILayerInfo
		{
			public Guid Id { get; set; }
			public string Name { get; set; }
			public Filter Filter { get; set; }
			[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
			public Layer[] Children { get; set; }

			string ILayerInfo.FilterType => Filter?.Type.ToString() ?? "null";
			IEnumerable<ILayerInfo> ILayerInfo.Children => Children ?? Enumerable.Empty<ILayerInfo>();
		}

		public Layer[] Layers { get; set; }
	}
}
