using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MeshEditor.SolutionManager.IO
{
	interface ISolutionController
	{
		IEnumerable<ISolutionInfo> GetAll();
		Solution Get(int solutionId);
		void AddLayer(Solution solution, Solution.Layer parentLayer, Solution.Layer newLayer);
	}
}
