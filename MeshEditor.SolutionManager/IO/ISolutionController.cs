using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MeshEditor.LayerManager.Import;

namespace MeshEditor.SolutionManager.IO
{
	interface ISolutionController
	{
		Solution CreateNew(object solutionLocator, IEnumerable<AnalysisResult> analysisResults, string projectName = null);
		IEnumerable<ISolutionInfo> GetAll();
		Solution Get(object solutionLocator);
		Solution AddLayer(Solution solution, Solution.Layer parentLayer, Solution.Layer newLayer);
		Solution DeleteLayer(Solution solution, Solution.Layer layerToDelete);

		Task<IEnumerable<ISolutionInfo>> GetAllAsync(CancellationToken cancellationToken);
		Task<Solution> GetAsync(object solutionLocator, CancellationToken cancellationToken);
	}
}
