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
		double[] TimeSteps { get; set; }
		string Location { get; set; }
	}

	public class ResultDescriptor : IResultDescription
	{
		public ResultDescriptor()
		{ }

		internal static ResultDescriptor CreateFrom(IResultDescription source)
		{
			return new ResultDescriptor
			{
				FieldName = source.FieldName,
				ComponentName = source.ComponentName,
				Index = source.Index,
				TimeSteps = source.TimeSteps.ToArray(),
				Location = source.Location
			};
		}

		public string FieldName { get; set; }
		public string ComponentName { get; set; }
		public int Index { get; set; }
		public double[] TimeSteps { get; set; }
		public string Location { get; set; }
	}
}
