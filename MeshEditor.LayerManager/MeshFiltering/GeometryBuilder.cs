using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.LayerManager.Data;

namespace MeshEditor.LayerManager.MeshFiltering
{
	internal class GeometryBuilder
	{
		private List<float> pointCoordinates;
		private List<int> cellConnectivity;
		private List<int> cellOffsets;
		private List<CellType> cellTypes;
		private int numberOfCoordinateComponents;
		private int pointCounter;

		public GeometryBuilder(int numberOfCoordinateComponents)
		{
			if (numberOfCoordinateComponents < 1)
				throw new ArgumentOutOfRangeException(nameof(numberOfCoordinateComponents));

			pointCoordinates = new List<float>();
			cellConnectivity = new List<int>();
			cellOffsets = new List<int>();
			cellTypes = new List<CellType>();
			this.numberOfCoordinateComponents = numberOfCoordinateComponents;
		}

		public int AddPoint(float xCoordinate)
		{
			if (numberOfCoordinateComponents != 1)
				throw new InvalidOperationException();
			pointCoordinates.Add(xCoordinate);
			return pointCounter++;
		}

		public int AddPoint(float xCoordinate, float yCoordinate)
		{
			if (numberOfCoordinateComponents != 2)
				throw new InvalidOperationException();
			pointCoordinates.Add(xCoordinate);
			pointCoordinates.Add(yCoordinate);
			return pointCounter++;
		}

		public int AddPoint(float xCoordinate, float yCoordinate, float zCoordinate)
		{
			if (numberOfCoordinateComponents != 3)
				throw new InvalidOperationException();
			pointCoordinates.Add(xCoordinate);
			pointCoordinates.Add(yCoordinate);
			pointCoordinates.Add(zCoordinate);
			return pointCounter++;
		}

		public int AddPoint(Vector3 coordinates)
		{
			pointCoordinates.Add(coordinates.X);
			if (numberOfCoordinateComponents > 1)
			{
				pointCoordinates.Add(coordinates.Y);
				if (numberOfCoordinateComponents > 2)
				{
					pointCoordinates.Add(coordinates.Z);
				}
			}
			return pointCounter++;
		}

		public void AddCell(CellType cellType, params int[] connectivity)
		{
			int numberOfPoints = GeometryDescription.MapCellTypeToNumberOfPoints(cellType);
			if (connectivity.Length != numberOfPoints)
				throw new ArgumentException(nameof(connectivity));
			cellTypes.Add(cellType);
			cellOffsets.Add(numberOfPoints);
			cellConnectivity.AddRange(connectivity);
		}

		public GeometryDescription Build()
		{
			GeometryDescription geometry = new GeometryDescription
			{
				NumberOfCoordinateComponents = numberOfCoordinateComponents,
				PointCoordinates = pointCoordinates.ToArray(),
				CellConnectivity = cellConnectivity.ToArray(),
				CellOffsets = cellOffsets.ToArray(),
				CellTypes = cellTypes.ToArray(),
			};
			return geometry;
		}
	}
}
