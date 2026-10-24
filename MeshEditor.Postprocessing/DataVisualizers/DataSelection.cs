using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.LayerManager.Data;

namespace MeshEditor.DataVisualizer
{
	public class DataSelection
	{
		public DataSelection(decimal timeStep, IMeshFileDescriptor mesh)
			: this(timeStep, null, null, null, mesh)
		{ }

		public DataSelection(decimal timeStep, string fieldName, string componentName, string vectorFieldName, IMeshFileDescriptor mesh)
		{
			FieldName = fieldName;
			ComponentName = componentName;
			TimeStep = timeStep;
			Mesh = mesh;
			VectorFieldName = vectorFieldName;
		}

		public string FieldName { get; }
		public string ComponentName { get; }
		public decimal TimeStep { get; }

		public string VectorFieldName { get; }

		public IMeshFileDescriptor Mesh { get; }

		public bool HasScalarSelection => ComponentName != null;

		public bool HasVectorSelection => VectorFieldName != null;

		public bool HasDifferentScalarSelectionThan(DataSelection other)
		{
			if (other == null)
				return HasScalarSelection;
			return TimeStep != other.TimeStep || FieldName != other.FieldName || ComponentName != other.ComponentName;
		}

		public bool HasDifferentVectorSelectionThan(DataSelection other)
		{
			if (other == null)
				return HasVectorSelection;
			return TimeStep != other.TimeStep || VectorFieldName != other.VectorFieldName;
		}
	}
}
