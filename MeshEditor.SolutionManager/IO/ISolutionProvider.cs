using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MeshEditor.SolutionManager.IO
{
	interface ISolutionProvider
	{
		IEnumerable<ISolutionInfo> GetAll();
		Solution Get(Uri uri);
		void Create(Solution solution);
		void Update(Solution solution);
	}
}
