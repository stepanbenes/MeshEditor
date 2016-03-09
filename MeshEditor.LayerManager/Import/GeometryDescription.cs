using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.LayerManager.Import
{
	public enum CellType : byte
	{
		Undefined = 0,
		// 0D
		Point = 1,
		// 1D
		LineLinear = 3,
		LineQuadratic = 21,
		// 2D
		TriangleLinear = 5,
		TriangleQuadratic = 22,
		QuadLinear = 9,
		QuadQuadratic = 23,
		// 3D
		TetraLinear = 10,
		TetraQuadratic = 24,
		WedgeLinear = 13,
		WedgeQuadratic = 26,
		HexaLinear = 12,
		HexaQuadratic = 25,
	}

	public class GeometryDescription
	{
		public int NumberOfCoordinateComponents { get; set; }
		public float[] PointCoordinates { get; set; }
		public int[] CellConnectivity { get; set; }
		public CellType[] CellTypes { get; set; }

		public Dictionary<int, int> PointIdIndexMap { get; set; }
		public Dictionary<int, int> CellIdIndexMap { get; set; }

		public int NumberOfPoints => PointCoordinates.Length / NumberOfCoordinateComponents;
		public int NumberOfCells => CellTypes.Length;
	}
}
