using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MeshEditor.SolutionManager.IO
{
	class SolutionInfo : ISolutionInfo
	{
		public int Id { get; set; }

		public string ProjectName { get; set; }

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string Location { get; set; }
	}
}
