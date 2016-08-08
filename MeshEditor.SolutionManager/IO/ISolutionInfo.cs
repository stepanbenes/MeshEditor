using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.SolutionManager.IO
{
	public interface ISolutionInfo
	{
		int Id { get; }
		string ProjectName { get; }
		string Location { get; }
	}
}
