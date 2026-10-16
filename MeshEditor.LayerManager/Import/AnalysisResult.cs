using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace MeshEditor.LayerManager.Import
{
	public class AnalysisResult
	{
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public decimal? TimeStep { get; set; }
		public IEnumerable<string> MeshRecordNames { get; set; }
		public IEnumerable<string> DataRecordNames { get; set; }
	}
}
