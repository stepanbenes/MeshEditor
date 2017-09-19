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
		public DataSelection(decimal timeStep, IMeshFileDescriptor mesh)
			: this(null, null, timeStep, null, null, null, mesh)
		{ }

		public DataSelection(string fieldName, string componentName, decimal timeStep, int? scalarDataIndex, string vectorFieldName, VectorIndex? vectorDataIndex, IMeshFileDescriptor mesh)
		{
			FieldName = fieldName;
			ComponentName = componentName;
			TimeStep = timeStep;
			ScalarDataIndex = scalarDataIndex;
			Mesh = mesh;
			VectorDataIndex = vectorDataIndex;
			VectorFieldName = vectorFieldName;
		}

		public string FieldName { get; }
		public string ComponentName { get; }
		public decimal TimeStep { get; }

		public int? ScalarDataIndex { get; }

		public string VectorFieldName { get; }

		public VectorIndex? VectorDataIndex { get; }

		public IMeshFileDescriptor Mesh { get; }
	}
}
