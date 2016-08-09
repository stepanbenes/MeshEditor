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
		Task<IEnumerable<ISolutionInfo>> GetAllAsync(CancellationToken cancellationToken);

		Solution Get(object solutionLocator);
		Task<Solution> GetAsync(object solutionLocator, CancellationToken cancellationToken);

		void Delete(object solutionLocator);
		Task DeleteAsync(object solutionLocator, CancellationToken cancellationToken);

		Solution AddLayer(Solution solution, Solution.Layer parentLayer, Solution.Layer newLayer);
		Solution DeleteLayer(Solution solution, Solution.Layer layerToDelete);
		Task<Solution> DeleteLayerAsync(Solution solution, Solution.Layer layerToDelete, CancellationToken cancellationToken);
	}
}
