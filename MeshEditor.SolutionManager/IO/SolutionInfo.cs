using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace MeshEditor.SolutionManager.IO
{
	class SolutionInfo : ISolutionInfo
	{
		public int Id { get; set; }

		public string ProjectName { get; set; }

		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public string Location { get; set; }

		/// <summary>
		/// User who owns the remote solution (null for local solutions).
		/// </summary>
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public RemoteUser Owner { get; set; }
	}
}
