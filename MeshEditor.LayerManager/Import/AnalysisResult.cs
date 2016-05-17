using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.LayerManager.Import
{
	public class AnalysisResult
	{
		public AnalysisResult(IReadOnlyList<string> meshRecordNames, IReadOnlyList<string> dataRecordNames)
		{
			MeshRecordNames = meshRecordNames;
			DataRecordNames = dataRecordNames;
		}

		public IReadOnlyList<string> MeshRecordNames { get; }
		public IReadOnlyList<string> DataRecordNames { get; }
	}
}
