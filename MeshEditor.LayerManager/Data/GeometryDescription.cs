using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.LayerManager.Data
{
	public class GeometryDescription
	{
		public int NumberOfCoordinateComponents { get; set; }
		public float[] PointCoordinates { get; set; }
		public int[] CellConnectivity { get; set; }
		public int[] CellOffsets { get; set; }
		public CellType[] CellTypes { get; set; }
		public int[] CellAttributes { get; set; }


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
