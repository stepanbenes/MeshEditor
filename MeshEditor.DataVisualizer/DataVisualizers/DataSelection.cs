using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.DataVisualizer
{
	public class DataSelection
	{
		public DataSelection(int meshIndex, int? elementPropertyAttributeIndex)
			: this(null, null, 0.0, null, meshIndex, elementPropertyAttributeIndex)
		{ }

		public DataSelection(string fieldName, string componentName, double timeStep, int? dataIndex, int meshIndex, int? elementPropertyAttributeIndex)
		{
			FieldName = fieldName;
			ComponentName = componentName;
			TimeStep = timeStep;
			DataIndex = dataIndex;
			MeshIndex = meshIndex;
			ElementPropertyAttributeIndex = elementPropertyAttributeIndex;
		}

		public string FieldName { get; }
		public string ComponentName { get; }
		public double TimeStep { get; }

		public int? DataIndex { get; }

		public int MeshIndex { get; }

		public int? ElementPropertyAttributeIndex { get; }
	}
}
