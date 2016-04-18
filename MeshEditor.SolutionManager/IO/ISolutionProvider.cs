using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MeshEditor.SolutionManager.IO
{
	interface ISolutionProvider
	{
		IEnumerable<SolutionFile> List();
		SolutionFile Get(int id);
		void Update(SolutionFile solution);
	}
}
