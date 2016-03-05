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

	public class DataDescription
	{
		// <PointData Scalars="PressureVector IST_VOFFraction " Vectors="DisplacementVector " Tensors="" >
		// <CellData Scalars="" Vectors="" Tensors="" >

		public int NumberOfDataComponents { get; set; }
		public double[] Data { get; set; }
		public string Name { get; set; }
		public FieldType FieldType { get; set; }

		public int NumberOfDataBlocks => Data.Length / NumberOfDataComponents;
	}
}
