using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.SolutionManager.IO
{
	class RestApiSolutionProvider : ISolutionProvider
	{
		public IEnumerable<ISolutionInfo> GetAll()
		{
			throw new NotImplementedException();
		}

		public Solution Get(Uri uri)
		{
			throw new NotImplementedException();
		}

		public void Create(Solution solution)
		{
			throw new NotImplementedException();
		}

		public void Update(Solution solution)
		{
			throw new NotImplementedException();
		}
	}
}
