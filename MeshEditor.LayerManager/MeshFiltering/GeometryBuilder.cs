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
		readonly List<float> pointCoordinates;
		readonly List<int> cellConnectivity;
		readonly List<int> cellOffsets;
		readonly List<CellType> cellTypes;
		readonly int numberOfCoordinateComponents;
		readonly FilterGeometryEntityMapping mapping;

		private int pointCounter;

		readonly bool mergeOverlappingPoints;
		readonly Dictionary<EdgeIntersection, int> intersectionCache;

		public GeometryBuilder(int numberOfCoordinateComponents, bool mergeOverlappingPoints)
		{
			if (numberOfCoordinateComponents < 1)
				throw new ArgumentOutOfRangeException(nameof(numberOfCoordinateComponents));

			pointCoordinates = new List<float>();
			cellConnectivity = new List<int>();
			cellOffsets = new List<int>();
			cellTypes = new List<CellType>();
			this.numberOfCoordinateComponents = numberOfCoordinateComponents;
			mapping = new FilterGeometryEntityMapping();

			this.mergeOverlappingPoints = mergeOverlappingPoints;
			if (this.mergeOverlappingPoints)
			{
				intersectionCache = new Dictionary<EdgeIntersection, int>();
			}
		}

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
			if (mergeOverlappingPoints)
			{
				if (intersectionCache.TryGetValue(edgeIntersection, out int pointIndex))
				{
					return pointIndex;
				}
				intersectionCache.Add(edgeIntersection, pointCounter);
			}

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

			if (checkIsDegeneratedTriangle())
			{
				return; // ignore degenerated triangles
			}

			// check for other degenerated shapes is not necessary. Maybe quadrilateral degenerated into single line?

			mapping.AddCellMapping(cellTypes.Count, oldCellId);
			for (int i = 0; i < connectivity.Length; i++)
			{
				mapping.AddCellPointEdgeMapping(cellConnectivity.Count + i, cellPointEdgeIntersections[i]);
			}

			cellTypes.Add(cellType);
			cellOffsets.Add(numberOfPoints);
			cellConnectivity.AddRange(connectivity);

			bool checkIsDegeneratedTriangle()
			{
				if (cellType == CellType.TriangleLinear)
				{
					if (connectivity[0] == connectivity[1] || connectivity[1] == connectivity[2] || connectivity[2] == connectivity[0])
						return true;
				}
				return false;
			}
		}

		public void AddEdge(int point1, int point2, float faceAngle)
		{
			if (point1 == point2)
				return; // ignore degenerated edges

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
