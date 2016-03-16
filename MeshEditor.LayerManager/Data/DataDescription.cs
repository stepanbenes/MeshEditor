using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.LayerManager.Data
{
	public class DataDescription
	{
		public string Name { get; set; }
		public double? TimeStep { get; set; }
		public string[] ComponentNames { get; set; }

		public FieldType FieldType { get; set; }
		public DataLocationType Location { get; set; }
		
		public int NumberOfComponents { get; set; }

		public double[] Data { get; set; } // or float ?
	}
}
