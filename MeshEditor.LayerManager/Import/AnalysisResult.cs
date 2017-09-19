using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MeshEditor.LayerManager.Import
{
	public class AnalysisResult
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public decimal? TimeStep { get; set; }
		public IEnumerable<string> MeshRecordNames { get; set; }
		public IEnumerable<string> DataRecordNames { get; set; }
	}
}
