using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.LayerManager.Data;
using MeshEditor.LayerManager.Filters;
using System.Diagnostics;
using MeshEditor.Common.Extensions;

namespace MeshEditor.LayerManager.MeshFiltering
{
	internal class DeformedMeshCreator : IMeshFilterCreator
	{
		readonly DeformationFilter deformationFilter;
		readonly IDictionary<double, List<ComponentDataDescription>> deformationData;

		public DeformedMeshCreator(DeformationFilter deformationFilter, IDictionary<double, List<ComponentDataDescription>> deformationData)
		{
			this.deformationFilter = deformationFilter;
			this.deformationData = deformationData;
		}

		public IEnumerable<(GeometryDescription geometry, List<double> timeSteps)> Create(GeometryDescription source, IEnumerable<double> timeSteps)
		{
			double[] maxValues = calculateMaxAbsoluteValues(source.NumberOfCoordinateComponents);
			int maxValueComponentIndex = maxValues.IndicesOfMaxElements().First();
			float[] maxDimensions = calculateMaxDimensions(source);
			double relativeScale = deformationFilter.RelativeScale ?? 1.0;
			double absoluteScale = (maxValues[maxValueComponentIndex] > 0.0) ? (maxDimensions[maxValueComponentIndex] / maxValues[maxValueComponentIndex]) * relativeScale : 0.0;

			foreach (double timeStep in timeSteps)
			{
				GeometryDescription geometry = buildDeformedGeometry(source, timeStep, absoluteScale);
				yield return (geometry, new List<double> { timeStep });
			}
		}

		private GeometryDescription buildDeformedGeometry(GeometryDescription sourceGeometry, double timeStep, double scale)
		{
			var dataComponents = deformationData[timeStep];

			Debug.Assert(dataComponents.Count == sourceGeometry.NumberOfCoordinateComponents);

			float[] deformedPointCoordinates = new float[sourceGeometry.PointCoordinates.Length];

			// deform PointCoordinates
			for (int componentIndex = 0; componentIndex < sourceGeometry.NumberOfCoordinateComponents; componentIndex++)
			{
				for (int pointIndex = 0; pointIndex < sourceGeometry.NumberOfPoints; pointIndex++)
				{
					int coordinateIndex = pointIndex * sourceGeometry.NumberOfCoordinateComponents + componentIndex;
					double value = dataComponents[componentIndex].Values[pointIndex];
					double displacement = double.IsNaN(value) ? 0.0 : scale * value;
					float deformedPointCoordinate = (float)(sourceGeometry.PointCoordinates[coordinateIndex] + displacement);
					deformedPointCoordinates[coordinateIndex] = deformedPointCoordinate;
				}
			}

			GeometryDescription deformedGeometry = new GeometryDescription
			{
				NumberOfCoordinateComponents = sourceGeometry.NumberOfCoordinateComponents,
				PointCoordinates = deformedPointCoordinates,
				CellConnectivity = sourceGeometry.CellConnectivity,
				CellOffsets = sourceGeometry.CellOffsets,
				CellTypes = sourceGeometry.CellTypes,
				Mapping = new IdentityGeometryEntityMapping()
			};
			return deformedGeometry;
		}

		private static float[] calculateMaxDimensions(GeometryDescription geometry)
		{
			float[] mins = Enumerable.Repeat(float.MaxValue, geometry.NumberOfCoordinateComponents).ToArray();
			float[] maxs = Enumerable.Repeat(float.MinValue, geometry.NumberOfCoordinateComponents).ToArray();
			for (int pointIndex = 0; pointIndex < geometry.NumberOfPoints; pointIndex++)
			{
				for (int componentIndex = 0; componentIndex < geometry.NumberOfCoordinateComponents; componentIndex++)
				{
					int coordinateIndex = pointIndex * geometry.NumberOfCoordinateComponents + componentIndex;
					mins[componentIndex] = Math.Min(mins[componentIndex], geometry.PointCoordinates[coordinateIndex]);
					maxs[componentIndex] = Math.Max(maxs[componentIndex], geometry.PointCoordinates[coordinateIndex]);
				}
			}
			return mins.Zip(maxs, (min, max) => (max - min)).ToArray();
		}

		private double[] calculateMaxAbsoluteValues(int numberOfCoordinateComponents)
		{
			double[] maxValues = new double[numberOfCoordinateComponents]; // array of zeroes
			foreach (var (timeStep, dataComponents) in deformationData)
			{
				for (int componentIndex = 0; componentIndex < numberOfCoordinateComponents; componentIndex++)
				{
					foreach (double value in dataComponents[componentIndex].Values)
					{
						if (!double.IsNaN(value))
						{
							maxValues[componentIndex] = Math.Max(maxValues[componentIndex], Math.Abs(value));
						}
					}
				}
			}
			return maxValues;
		}
	}
}
