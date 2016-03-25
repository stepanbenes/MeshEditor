using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.LayerManager.Filters;
using Newtonsoft.Json;

namespace MeshEditor.LayerManager.Infrastructure
{
	public class Solution
	{
		public class LayerRecord
		{
			public Guid Id { get; set; }
			public string Name { get; set; }
			public FilterBase Filter { get; set; }
			[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
			public LayerRecord[] Children { get; set; }
		}

		public string ProjectName { get; set; }
		public LayerRecord[] Layers { get; set; }
	}
}
