using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.SolutionManager.IO;

namespace MeshEditor.DataVisualizer
{
	public class DataSelection
	{
		public DataSelection(Guid layerId, int meshIndex, int? elementPropertyAttributeIndex)
			: this(layerId, null, null, 0.0, null, meshIndex, elementPropertyAttributeIndex)
		{ }

		public DataSelection(Guid layerId, string fieldName, string componentName, double timeStep, int? dataIndex, int meshIndex, int? elementPropertyAttributeIndex)
		{
			LayerId = layerId;
			FieldName = fieldName;
			ComponentName = componentName;
			TimeStep = timeStep;
			DataIndex = dataIndex;
			MeshIndex = meshIndex;
			ElementPropertyAttributeIndex = elementPropertyAttributeIndex;
		}

		public Guid LayerId { get; }
		public string FieldName { get; }
		public string ComponentName { get; }
		public double TimeStep { get; }

		public int? DataIndex { get; }

		public int MeshIndex { get; }

		public int? ElementPropertyAttributeIndex { get; }
	}
}
