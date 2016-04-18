using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MeshEditor.LayerManager.Filters;
using Newtonsoft.Json;

namespace MeshEditor.SolutionManager.IO
{
	class SolutionFile
	{
		public class LayerRecord
		{
			public Guid Id { get; set; }
			public string Name { get; set; }
			public Filter Filter { get; set; }
			[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
			public LayerRecord[] Children { get; set; }
		}

		public int Id { get; set; }
		public string ProjectName { get; set; }
		public LayerRecord[] Layers { get; set; }
	}
}
