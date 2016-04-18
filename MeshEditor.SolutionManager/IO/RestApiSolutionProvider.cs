using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.SolutionManager.IO
{
	class RestApiSolutionProvider : ISolutionProvider
	{
		public SolutionFile Get(int id)
		{
			throw new NotImplementedException();
		}

		public IEnumerable<SolutionFile> List()
		{
			throw new NotImplementedException();
		}

		public void Update(SolutionFile solution)
		{
			throw new NotImplementedException();
		}
	}
}
