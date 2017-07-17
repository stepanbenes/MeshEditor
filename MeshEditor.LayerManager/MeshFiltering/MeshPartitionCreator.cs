using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.LayerManager.Data;
using MeshEditor.LayerManager.Filters;

namespace MeshEditor.LayerManager.MeshFiltering
{
	internal class MeshPartitionCreator : IMeshFilterCreator
	{
		readonly AttributeSelectionFilter attributeSelectionFilter;
		readonly AttributeDescription attribute;

		public MeshPartitionCreator(AttributeSelectionFilter attributeSelectionFilter, AttributeDescription attribute)
		{
			Debug.Assert(attributeSelectionFilter != null);
			Debug.Assert(attribute != null);
			this.attributeSelectionFilter = attributeSelectionFilter;
			this.attribute = attribute;
		}

		public IList<(GeometryDescription geometry, List<double> timeSteps)> Create(GeometryDescription geometry, IEnumerable<double> timeSteps)
		{
			// TODO: use GeometryBuilder

			List<CellType> cellTypes = new List<CellType>();
			HashSet<int> remainingPointIndices = new HashSet<int>();
			List<int> cellConnectivity = new List<int>();
			List<int> cellOffsets = new List<int>();

			FilterGeometryEntityMapping mapping = new FilterGeometryEntityMapping();
			int[] selectionFilter = attributeSelectionFilter.AttributeSelection;

			for (int oldCellIndex = 0, newCellIndex = 0, previousOffset = 0; oldCellIndex < geometry.NumberOfCells; oldCellIndex++)
			{
				int currentOffset = geometry.CellOffsets[oldCellIndex];
				if (selectionFilter.Contains(attribute.Values[oldCellIndex]))
				{
					for (int offset = previousOffset; offset < currentOffset; offset++)
					{
						int pointIndex = geometry.CellConnectivity[offset];
						remainingPointIndices.Add(pointIndex);

						mapping.AddCellPointMapping(from: cellConnectivity.Count, to: offset);
						cellConnectivity.Add(pointIndex);
					}
					cellOffsets.Add(cellConnectivity.Count);
					cellTypes.Add(geometry.CellTypes[oldCellIndex]);
					mapping.AddCellMapping(newCellIndex, oldCellIndex);
					newCellIndex += 1;
				}
				previousOffset = currentOffset;
			}

			int numberOfCoordinates = geometry.NumberOfCoordinateComponents;
			List<float> pointCoordinates = new List<float>();
			Dictionary<int, int> oldNewPointIndexMap = new Dictionary<int, int>();

			{
				int newPointIndex = 0;
				foreach (int oldPointIndex in remainingPointIndices.OrderBy(p => p))
				{
					for (int coordinateIndex = 0; coordinateIndex < numberOfCoordinates; coordinateIndex++)
					{
						pointCoordinates.Add(geometry.PointCoordinates[oldPointIndex * numberOfCoordinates + coordinateIndex]);
					}
					mapping.AddPointMapping(newPointIndex, oldPointIndex);
					oldNewPointIndexMap[oldPointIndex] = newPointIndex;
					newPointIndex += 1;
				}
			}

			// update cell connectivity (from old point indices to new point indices)
			for (int i = 0; i < cellConnectivity.Count; i++)
			{
				int oldPointIndex = cellConnectivity[i];
				int newPointIndex = oldNewPointIndexMap[oldPointIndex];
				cellConnectivity[i] = newPointIndex;
			}

			GeometryDescription filteredGeometry = new GeometryDescription
			{
				NumberOfCoordinateComponents = numberOfCoordinates,
				PointCoordinates = pointCoordinates.ToArray(),
				CellConnectivity = cellConnectivity.ToArray(),
				CellOffsets = cellOffsets.ToArray(),
				CellTypes = cellTypes.ToArray(),
				Mapping = mapping
			};

			return new[] { (filteredGeometry, timeSteps.ToList()) };
		}
	}
}
