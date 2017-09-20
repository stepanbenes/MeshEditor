using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.Data;
using MeshEditor.LayerManager.Data;
using MeshEditor.Common.Extensions;
using MeshEditor.DataVisualizer.Data;
using MeshEditor.DataVisualizer.Graphics;
using OpenTK;

namespace MeshEditor.DataVisualizer
{
	internal class LayerDataVisualizer : DataVisualizerBase
	{
		#region Fields, constructor

		readonly GeometryDescription geometry;

		DataSelection dataSelection;
		ComponentDataDescription currentDataComponent;

		public LayerDataVisualizer(GeometryDescription geometry, IVisualizerSettings settings)
			: base(settings)
		{
			Debug.Assert(geometry != null);
			this.geometry = geometry;
		}

		#endregion

		#region Properties

		public DataSelection DataSelection
		{
			get => dataSelection;
			set
			{
				if (dataSelection != value)
				{
					dataSelection = value;
					buildDataDescription();
				}
			}
		}

		public override bool DisplayColors => base.DisplayColors && currentDataComponent != null;

		#endregion

		#region Public methods

		public void UpdateScalarData(ComponentDataDescription scalarData)
		{
			currentDataComponent = scalarData;
			setupColorScale();
		}

		public void UpdateVectorData(IReadOnlyList<ComponentDataDescription> vectorComponents, Mesh mesh)
		{
			Debug.Assert(vectorComponents == null || vectorComponents.Count == 3);

			if (vectorComponents != null)
			{
				// build vector arrows vbo from vectorComponents
				setVectorField(createVectorField(vectorComponents, mesh));
			}
		}

		public override double GetDataValue(Node node)
		{
			if (currentDataComponent == null)
				return double.NaN;

			if (currentDataComponent.Location == DataLocationType.Points)
			{
				return currentDataComponent.Values[node.ID];
			}
			Debug.Assert(currentDataComponent.Location == DataLocationType.CellPoints || currentDataComponent.Location == DataLocationType.Cells);
			return double.NaN;
		}

		public override double GetDataValue(Node node, Element element)
		{
			if (currentDataComponent == null)
				return double.NaN;

			switch (currentDataComponent.Location)
			{
				case DataLocationType.Points:
					return currentDataComponent.Values[node.ID];
				case DataLocationType.CellPoints:

					Debug.Assert(geometry != null);
					int cellOffset = (element.ID > 0) ? geometry.CellOffsets[element.ID - 1] : 0;
					int? nodeIndex = element.GetIndexOfNode_IncludingMiddleNodes(node);
					Debug.Assert(nodeIndex.HasValue); // node has to be contained in element
					double value = currentDataComponent.Values[cellOffset + nodeIndex.Value]; // WARNING: correct node ordering is supposed
					return value;

				case DataLocationType.Cells:
					return currentDataComponent.Values[element.ID];
				default:
					throw new NotSupportedException();
			}
		}

		public override int[] GetIDsOfNodesWithMaximumDataValue()
		{
			if (currentDataComponent == null)
				return new int[0]; //Array.Empty<int>();

			switch (currentDataComponent.Location)
			{
				case DataLocationType.Points:
					return currentDataComponent.Values.IndicesOfMaxElements().ToArray();
				case DataLocationType.CellPoints:
					return currentDataComponent.Values.IndicesOfMaxElements().Select(cellPointIndex => geometry.CellConnectivity[cellPointIndex]).ToArray();
				case DataLocationType.Cells:
					return currentDataComponent.Values.IndicesOfMaxElements().SelectMany(cellIndex => getCellPointIndicesForCell(cellIndex).Select(cellPointIndex => geometry.CellConnectivity[cellPointIndex])).ToArray();
				default:
					throw new NotSupportedException();
			}
		}

		public override int[] GetIDsOfNodesWithMinimumDataValue()
		{
			if (currentDataComponent == null)
				return new int[0]; //Array.Empty<int>();

			switch (currentDataComponent.Location)
			{
				case DataLocationType.Points:
					return currentDataComponent.Values.IndicesOfMinElements().ToArray();
				case DataLocationType.CellPoints:
					return currentDataComponent.Values.IndicesOfMinElements().Select(cellPointIndex => geometry.CellConnectivity[cellPointIndex]).ToArray();
				case DataLocationType.Cells:
					return currentDataComponent.Values.IndicesOfMinElements().SelectMany(cellIndex => getCellPointIndicesForCell(cellIndex).Select(cellPointIndex => geometry.CellConnectivity[cellPointIndex])).ToArray();
				default:
					throw new NotSupportedException();
			}
		}

		public override double GetMaximumDataValue() => currentDataComponent?.Values.Max(ignore: double.NaN) ?? double.NaN;

		public override double GetMinimumDataValue() => currentDataComponent?.Values.Min(ignore: double.NaN) ?? double.NaN;

		#endregion

		#region Private methods

		private IEnumerable<int> getCellPointIndicesForCell(int cellIndex)
		{
			int startOffset, endOffset;
			if (cellIndex > 0)
			{
				startOffset = geometry.CellOffsets[cellIndex - 1];
			}
			else
			{
				startOffset = 0;
			}
			endOffset = geometry.CellOffsets[cellIndex];
			Debug.Assert(endOffset > startOffset);
			Debug.Assert(startOffset >= 0);
			Debug.Assert(endOffset < geometry.CellConnectivity.Length);
			return Enumerable.Range(startOffset, endOffset - startOffset);
		}

		private void setupColorScale()
		{
			Settings.ColorScale.SetMinMaxValue(minValue: GetMinimumDataValue(), maxValue: GetMaximumDataValue());
		}

		private void buildDataDescription()
		{
			if (dataSelection == null)
				ScalarDataDescription = "";
			else
				ScalarDataDescription = dataSelection.FieldName + Environment.NewLine + dataSelection.ComponentName + Environment.NewLine + "t = " + dataSelection.TimeStep;
		}

		private VectorField createVectorField(IReadOnlyList<ComponentDataDescription> vectorComponents, Mesh mesh)
		{
			if (!vectorComponents.All(c => c.Location == DataLocationType.Points))
			{
				throw new NotSupportedException("The only supported data location for vector field is Points");
			}

			var xComponent = vectorComponents[0];
			var yComponent = vectorComponents[1];
			var zComponent = vectorComponents[2];

			IntervalD xRange = IntervalD.InvertedMaxMin;
			IntervalD yRange = IntervalD.InvertedMaxMin;
			IntervalD zRange = IntervalD.InvertedMaxMin;

			Vector3[] positions = new Vector3[mesh.NodesEdgesIncidence.Count];
			Vector3[] vectors = new Vector3[mesh.NodesEdgesIncidence.Count];

			int index = 0;
			foreach (Node node in mesh.NodesEdgesIncidence.Keys)
			{
				double x = xComponent.Values[node.ID];
				double y = yComponent.Values[node.ID];
				double z = zComponent.Values[node.ID];

				xRange.MergeWith(x);
				yRange.MergeWith(y);
				zRange.MergeWith(z);

				positions[index] = node.Position;
				vectors[index] = new Vector3((float)x, (float)y, (float)z);
				index += 1;
			}

			double maxAbsValue = Math.Max(Math.Max(xRange.GetMaxAbsValue(), yRange.GetMaxAbsValue()), zRange.GetMaxAbsValue());

			const double epsilon = 1e-20;
			if (maxAbsValue < epsilon)
			{
				return null; // do not construct vector field if max value is too small
			}

			float resizeFactor = (float)(/*Settings.VectorLengthFactor*/ 0.1 / maxAbsValue);

			return new VectorField(positions, vectors, resizeFactor, moveEndOfArrowsToNodes: false);
		}

		#endregion
	}
}
