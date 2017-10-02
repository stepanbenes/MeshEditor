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
using MeshEditor.Graphics;

namespace MeshEditor.DataVisualizer
{
	internal class LayerDataVisualizer : DataVisualizerBase
	{
		#region Fields, constructor

		readonly GeometryDescription geometry;

		Mesh mesh;
		DataSelection dataSelection;
		ComponentDataDescription currentScalarComponent;
		IReadOnlyList<ComponentDataDescription> currentVectorComponents;

		VectorField vectorField;

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

		public override bool DisplayColors => currentScalarComponent != null;

		#endregion

		#region Public methods

		public override void Initialize(Mesh mesh)
		{
			this.mesh = mesh;
		}

		public override void DrawDecorations(PropertyColorsMode propertyColorsMode)
		{

			// DRAW VECTORS AS ARROWS
			if (vectorField != null)
			{
				// rebuild vectorField if needed
				if (vectorField.LengthFactor != Settings.ArrowLengthFactor || vectorField.InvertVectorArrows != Settings.InvertVectorArrows)
				{
					setupVectorField();
				}

				vectorField.Draw();
			}

			base.DrawDecorations(propertyColorsMode);
		}

		public void UpdateScalarData(ComponentDataDescription scalarComponent)
		{
			currentScalarComponent = scalarComponent;
			setupColorScale();
		}

		public void UpdateVectorData(IReadOnlyList<ComponentDataDescription> vectorComponents)
		{
			Debug.Assert(vectorComponents == null || vectorComponents.Count == 3);
			currentVectorComponents = vectorComponents;
			// build vector arrows vbo from vectorComponents
			setupVectorField();
		}

		public override double GetDataValue(Node node)
		{
			if (currentScalarComponent == null)
				return double.NaN;

			if (currentScalarComponent.Location == DataLocationType.Points)
			{
				return currentScalarComponent.Values[node.ID];
			}
			Debug.Assert(currentScalarComponent.Location == DataLocationType.CellPoints || currentScalarComponent.Location == DataLocationType.Cells);
			return double.NaN;
		}

		public override double GetDataValue(Node node, Element element)
		{
			if (currentScalarComponent == null)
				return double.NaN;

			switch (currentScalarComponent.Location)
			{
				case DataLocationType.Points:
					return currentScalarComponent.Values[node.ID];
				case DataLocationType.CellPoints:

					Debug.Assert(geometry != null);
					int cellOffset = (element.ID > 0) ? geometry.CellOffsets[element.ID - 1] : 0;
					int? nodeIndex = element.GetIndexOfNode_IncludingMiddleNodes(node);
					Debug.Assert(nodeIndex.HasValue); // node has to be contained in element
					double value = currentScalarComponent.Values[cellOffset + nodeIndex.Value]; // WARNING: correct node ordering is supposed
					return value;

				case DataLocationType.Cells:
					return currentScalarComponent.Values[element.ID];
				default:
					throw new NotSupportedException();
			}
		}

		public override int[] GetIDsOfNodesWithMaximumDataValue()
		{
			if (currentScalarComponent == null)
				return new int[0]; //Array.Empty<int>();

			switch (currentScalarComponent.Location)
			{
				case DataLocationType.Points:
					return currentScalarComponent.Values.IndicesOfMaxElements().ToArray();
				case DataLocationType.CellPoints:
					return currentScalarComponent.Values.IndicesOfMaxElements().Select(cellPointIndex => geometry.CellConnectivity[cellPointIndex]).ToArray();
				case DataLocationType.Cells:
					return currentScalarComponent.Values.IndicesOfMaxElements().SelectMany(cellIndex => getCellPointIndicesForCell(cellIndex).Select(cellPointIndex => geometry.CellConnectivity[cellPointIndex])).ToArray();
				default:
					throw new NotSupportedException();
			}
		}

		public override int[] GetIDsOfNodesWithMinimumDataValue()
		{
			if (currentScalarComponent == null)
				return new int[0]; //Array.Empty<int>();

			switch (currentScalarComponent.Location)
			{
				case DataLocationType.Points:
					return currentScalarComponent.Values.IndicesOfMinElements().ToArray();
				case DataLocationType.CellPoints:
					return currentScalarComponent.Values.IndicesOfMinElements().Select(cellPointIndex => geometry.CellConnectivity[cellPointIndex]).ToArray();
				case DataLocationType.Cells:
					return currentScalarComponent.Values.IndicesOfMinElements().SelectMany(cellIndex => getCellPointIndicesForCell(cellIndex).Select(cellPointIndex => geometry.CellConnectivity[cellPointIndex])).ToArray();
				default:
					throw new NotSupportedException();
			}
		}

		public override double GetMaximumDataValue() => currentScalarComponent?.Values.Max(ignore: double.NaN) ?? double.NaN;

		public override double GetMinimumDataValue() => currentScalarComponent?.Values.Min(ignore: double.NaN) ?? double.NaN;

		#endregion

		#region Protected methods

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (vectorField != null)
				{
					vectorField.Dispose();
					vectorField = null;
				}
			}

			base.Dispose(disposing);
		}

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


		private void setupVectorField()
		{
			VectorField newVectorField = createVectorField();

			if (vectorField != null)
			{
				vectorField.Dispose();
				vectorField = null;
			}

			this.vectorField = newVectorField;

			VectorField createVectorField()
			{
				Debug.Assert(mesh != null);

				if (currentVectorComponents == null)
				{
					return null;
				}

				if (currentVectorComponents.Count != 3)
				{
					throw new InvalidOperationException($"Three vector components expected. Got {currentVectorComponents.Count} instead.");
				}

				if (!currentVectorComponents.All(c => c.Location == DataLocationType.Points))
				{
					throw new NotSupportedException("The only supported data location for vector field is Points");
				}

				var xComponent = currentVectorComponents[0];
				var yComponent = currentVectorComponents[1];
				var zComponent = currentVectorComponents[2];

				IntervalD xRange = IntervalD.Zero;
				IntervalD yRange = IntervalD.Zero;
				IntervalD zRange = IntervalD.Zero;

				Vector3[] positions = new Vector3[mesh.NodesEdgesIncidence.Count];
				Vector3[] vectors = new Vector3[mesh.NodesEdgesIncidence.Count];

				int index = 0;
				foreach (Node node in mesh.NodesEdgesIncidence.Keys)
				{
					double x = xComponent.Values[node.ID];
					double y = yComponent.Values[node.ID];
					double z = zComponent.Values[node.ID];

					if (double.IsNaN(x) || double.IsNaN(y) || double.IsNaN(z))
					{
						continue;
					}

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

				return new VectorField(positions, vectors, maxAbsValue, Settings.ArrowLengthFactor, Settings.InvertVectorArrows);
			}
		}

		#endregion
	}
}
