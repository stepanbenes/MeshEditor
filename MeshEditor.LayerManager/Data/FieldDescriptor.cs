using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.LayerManager.Data
{
	public class FieldDescriptor
	{
		public Dictionary<string, ComponentDescriptor> Components { get; set; }
	}

	public class ComponentDescriptor
	{
		public Dictionary<decimal, TimeStepDescriptor> TimeSteps { get; set; }
	}

	public class TimeStepDescriptor
	{
		public int MeshIndex { get; set; }
		public int DataIndex { get; set; }
	}
}
