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
		public int[] CellOffsets { get; set; }
		public CellType[] CellTypes { get; set; }

		public Dictionary<int, int> PointIdIndexMap { get; set; } = null;
		public Dictionary<int, int> CellIdIndexMap { get; set; } = null;

		public int NumberOfPoints => PointCoordinates.Length / NumberOfCoordinateComponents;
		public int NumberOfCells => CellTypes.Length;

		#region Public methods

		public static int MapCellTypeToNumberOfPoints(CellType cellType)
		{
			switch (cellType)
			{
				case CellType.Point:
					return 1;
				case CellType.LineLinear:
					return 2;
				case CellType.LineQuadratic:
					return 3;
				case CellType.TriangleLinear:
					return 3;
				case CellType.TriangleQuadratic:
					return 6;
				case CellType.QuadLinear:
					return 4;
				case CellType.QuadQuadratic:
					return 8;
				case CellType.TetraLinear:
					return 4;
				case CellType.TetraQuadratic:
					return 10;
				case CellType.WedgeLinear:
					return 6;
				case CellType.WedgeQuadratic:
					return 15;
				case CellType.HexaLinear:
					return 8;
				case CellType.HexaQuadratic:
					return 20;
				default:
					throw new NotSupportedException();
			}
		}

		#endregion
	}
}
