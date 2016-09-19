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

		public GeometryEntityMapping Mapping { get; set; }

		public int NumberOfPoints => IsEmpty ? 0 : PointCoordinates.Length / NumberOfCoordinateComponents;
		public int NumberOfCells => CellTypes.Length;

		public bool IsEmpty => PointCoordinates.Length == 0;

		#region Public methods

		public void CalculateCenterAndRadius(out float[] center, out float radius)
		{
			if (PointCoordinates.Length == 0)
			{
				center = Enumerable.Repeat(0f, NumberOfCoordinateComponents).ToArray();
				radius = 1f;
				return;
			}

			float[] mins = Enumerable.Repeat(float.MaxValue, NumberOfCoordinateComponents).ToArray();
			float[] maxs = Enumerable.Repeat(float.MinValue, NumberOfCoordinateComponents).ToArray();
			for (int i = 0; i < PointCoordinates.Length; i++)
			{
				mins[i % mins.Length] = Math.Min(mins[i % mins.Length], PointCoordinates[i]);
				maxs[i % maxs.Length] = Math.Max(maxs[i % maxs.Length], PointCoordinates[i]);
			}
			center = mins.Zip(maxs, (min, max) => (max + min) * 0.5f).ToArray();
			radius = (float)(Math.Sqrt(mins.Zip(maxs, (min, max) => (max - min) * (max - min)).Sum()) * 0.5);
		}

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

		public static int GetDimensionOfCellType(CellType cellType)
		{
			switch (cellType)
			{
				case CellType.Point:
					return 0;
				case CellType.LineLinear:
				case CellType.LineQuadratic:
					return 1;
				case CellType.TriangleLinear:
				case CellType.TriangleQuadratic:
				case CellType.QuadLinear:
				case CellType.QuadQuadratic:
					return 2;
				case CellType.TetraLinear:
				case CellType.TetraQuadratic:
				case CellType.WedgeLinear:
				case CellType.WedgeQuadratic:
				case CellType.HexaLinear:
				case CellType.HexaQuadratic:
					return 3;
				default:
					throw new NotSupportedException();
			}
		}

		#endregion
	}
}
