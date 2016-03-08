using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.LayerManager.Import
{
	public enum FieldType
	{
		Scalar = 1,
		Vector = 2,
		Tensor = 3,
	}

	public enum DataLocationType
	{
		Points = 1,
		CellPoints = 2,
		Cells = 3,
	}

	public class DataDescription
	{
		public string Name { get; set; }
		public double? TimeStep { get; set; }
		public string[] ComponentNames { get; set; }

		public FieldType FieldType { get; set; }
		public DataLocationType LocationType { get; set; }
		
		public int NumberOfComponents { get; set; }

		public double[] Data { get; set; } // or float ?
	}
}
