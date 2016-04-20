using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MeshEditor.SolutionManager.IO
{
	interface ISolutionProvider
	{
		IEnumerable<ISolutionInfo> GetAll();
		Solution Get(ISolutionInfo solutionInfo);
		void CreateNew(SolutionBase solution);
		void AddLayer(Solution solution, Solution.Layer parentLayer, Solution.Layer newLayer);
	}
}
