using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.DataVisualizer
{
	public class DataSelection
	{
		public DataSelection(string fieldName, string componentName, double timeStep, int dataIndex, int meshIndex)
		{
			FieldName = fieldName;
			ComponentName = componentName;
			TimeStep = timeStep;
			DataIndex = dataIndex;
			MeshIndex = meshIndex;
		}

		public string FieldName { get; }
		public string ComponentName { get; }
		public double TimeStep { get; }

		public int DataIndex { get; }

		public int MeshIndex { get; }
	}
}
