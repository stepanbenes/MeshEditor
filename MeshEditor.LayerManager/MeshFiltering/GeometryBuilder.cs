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

		private FilterGeometryEntityMapping mapping;

		public GeometryBuilder(int numberOfCoordinateComponents)
		{
			if (numberOfCoordinateComponents < 1)
				throw new ArgumentOutOfRangeException(nameof(numberOfCoordinateComponents));

			pointCoordinates = new List<float>();
			cellConnectivity = new List<int>();
			cellOffsets = new List<int>();
			cellTypes = new List<CellType>();
			this.numberOfCoordinateComponents = numberOfCoordinateComponents;
			mapping = new FilterGeometryEntityMapping();
		}

		//public int AddPoint(float xCoordinate)
		//{
		//	if (numberOfCoordinateComponents != 1)
		//		throw new InvalidOperationException();
		//	pointCoordinates.Add(xCoordinate);
		//	return pointCounter++;
		//}

		//public int AddPoint(float xCoordinate, float yCoordinate)
		//{
		//	if (numberOfCoordinateComponents != 2)
		//		throw new InvalidOperationException();
		//	pointCoordinates.Add(xCoordinate);
		//	pointCoordinates.Add(yCoordinate);
		//	return pointCounter++;
		//}

		//public int AddPoint(float xCoordinate, float yCoordinate, float zCoordinate)
		//{
		//	if (numberOfCoordinateComponents != 3)
		//		throw new InvalidOperationException();
		//	pointCoordinates.Add(xCoordinate);
		//	pointCoordinates.Add(yCoordinate);
		//	pointCoordinates.Add(zCoordinate);
		//	return pointCounter++;
		//}

		public int AddPoint(Vector3 coordinates, int oldPointId)
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
			mapping.AddPointMapping(pointCounter, oldPointId);
			return pointCounter++;
		}

		public int AddPoint(Vector3 coordinates, EdgeIntersection edgeIntersection)
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
			mapping.AddPointEdgeMapping(pointCounter, edgeIntersection);
			return pointCounter++;
		}

		public void AddCell(CellType cellType, int oldCellId, int[] connectivity, int[] oldCellPointIds)
		{
			int numberOfPoints = GeometryDescription.MapCellTypeToNumberOfPoints(cellType);
			if (connectivity.Length != numberOfPoints)
				throw new ArgumentException(nameof(connectivity));

			mapping.AddCellMapping(cellTypes.Count, oldCellId);
			for (int i = 0; i < connectivity.Length; i++)
			{
				mapping.AddCellPointMapping(cellConnectivity.Count + i, oldCellPointIds[i]);
			}

			cellTypes.Add(cellType);
			cellOffsets.Add(numberOfPoints);
			cellConnectivity.AddRange(connectivity);
		}

		public void AddCell(CellType cellType, int oldCellId, int[] connectivity, EdgeIntersection[] cellPointEdgeIntersections)
		{
			int numberOfPoints = GeometryDescription.MapCellTypeToNumberOfPoints(cellType);
			if (connectivity.Length != numberOfPoints)
				throw new ArgumentException(nameof(connectivity));

			mapping.AddCellMapping(cellTypes.Count, oldCellId);
			for (int i = 0; i < connectivity.Length; i++)
			{
				mapping.AddCellPointEdgeMapping(cellConnectivity.Count + i, cellPointEdgeIntersections[i]);
			}

			cellTypes.Add(cellType);
			cellOffsets.Add(numberOfPoints);
			cellConnectivity.AddRange(connectivity);
		}

		public void AddEdge(int point1, int point2, float faceAngle)
		{
			// TODO: add implementation
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
				Mapping = mapping
			};
			return geometry;
		}
	}
}
