using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MeshEditor.LayerManager.Import;

namespace MeshEditor.SolutionManager.IO
{
	interface ISolutionController
	{
		Solution CreateNew(int solutionId, IEnumerable<AnalysisResult> analysisResults, string projectName = null);
		IEnumerable<ISolutionInfo> GetAll();
		Solution Get(int solutionId);
		void AddLayer(Solution solution, Solution.Layer parentLayer, Solution.Layer newLayer);
		void DeleteLayer(Solution solution, Solution.Layer layerToDelete);
	}
}
