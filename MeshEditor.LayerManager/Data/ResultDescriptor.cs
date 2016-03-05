using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.LayerManager.Data
{
	internal interface IResultDescription
	{
		string FieldName { get; set; }
		string ComponentName { get; set; }
		int Index { get; set; }
		double? TimeStep { get; set; }
	}

	public class ResultDescriptor : IResultDescription
	{
		public string FieldName { get; set; }
		public string ComponentName { get; set; }
		public int Index { get; set; }
		public double? TimeStep { get; set; }
	}
}
