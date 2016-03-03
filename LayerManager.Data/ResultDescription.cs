using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LayerManager.Types
{
	public interface IResultDescription
	{
		string ResultName { get; set; }
		string ComponentName { get; set; }
		double TimeStep { get; set; }
	}

	public class ResultDescriptor : IResultDescription
	{
		public string ResultName { get; set; }
		public string ComponentName { get; set; }
		public double TimeStep { get; set; }
		public string FileName { get; set; }
	}
}
