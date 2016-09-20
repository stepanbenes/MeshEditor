using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.LayerManager.Data;
using MeshEditor.SolutionManager.IO;

namespace MeshEditor.DataVisualizer
{
	public class DataSelection
	{
		public DataSelection(IMeshFileDescriptor mesh)
			: this(null, null, 0.0, null, mesh)
		{ }

		public DataSelection(string fieldName, string componentName, double timeStep, int? dataIndex, IMeshFileDescriptor mesh)
		{
			FieldName = fieldName;
			ComponentName = componentName;
			TimeStep = timeStep;
			DataIndex = dataIndex;
			Mesh = mesh;
		}

		public string FieldName { get; }
		public string ComponentName { get; }
		public double TimeStep { get; }

		public int? DataIndex { get; }

		public IMeshFileDescriptor Mesh { get; }
	}
}
