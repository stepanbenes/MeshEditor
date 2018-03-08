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
using MeshEditor.DataVisualizer.Mathematics;
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
		HashSet<int> elementsWithUndefinedNodeValues;

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

		public override bool DisplayData => currentScalarComponent != null || currentVectorComponents != null;

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
			elementsWithUndefinedNodeValues = null;
			setupColorScale();
		}

		public void UpdateVectorData(IReadOnlyList<ComponentDataDescription> vectorComponents)
		{
			Debug.Assert(vectorComponents == null || (vectorComponents.Count > 0 && vectorComponents.Count <= 3));
			currentVectorComponents = vectorComponents;
			// build vector arrows vbo from vectorComponents
			setupVectorField();
		}

		public override double GetDataValue(Node node)
		{
			if (currentScalarComponent != null)
			{
				if (currentScalarComponent.Location == DataLocationType.Points)
				{
					return currentScalarComponent.Values[node.ID];
				}
				Debug.Assert(currentScalarComponent.Location == DataLocationType.CellPoints || currentScalarComponent.Location == DataLocationType.Cells);
			}
			else if (currentVectorComponents != null)
			{
				return getVectorMagnitude(node.ID);
			}
			return double.NaN;
		}

		public override double GetDataValue(Node node, Element element)
		{
			if (currentScalarComponent == null)
				return double.NaN;

			switch (currentScalarComponent.Location)
			{
				case DataLocationType.Points:
					{
						if (hasElementSomeUndefinedNodeValues(element.ID))
							return double.NaN; // avoid interpolation of undefined color with regular color
						return currentScalarComponent.Values[node.ID];
					}
				case DataLocationType.CellPoints:
					{
						Debug.Assert(geometry != null);
						int cellOffset = (element.ID > 0) ? geometry.CellOffsets[element.ID - 1] : 0;
						int? nodeIndex = element.GetIndexOfNode_IncludingMiddleNodes(node);
						Debug.Assert(nodeIndex.HasValue); // node has to be contained in element
						return currentScalarComponent.Values[cellOffset + nodeIndex.Value]; // WARNING: correct node ordering is supposed
					}
				case DataLocationType.Cells:
					{
						return currentScalarComponent.Values[element.ID];
					}
				default:
					throw new NotSupportedException();
			}
		}

		public override int[] GetIDsOfNodesWithMaximumDataValue()
		{
			if (currentScalarComponent != null)
			{
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
			else if (currentVectorComponents != null)
			{
				return enumerateVectorMagnitudes().IndicesOfMaxElements().ToArray();
			}
			return new int[0]; //Array.Empty<int>();
		}

		public override int[] GetIDsOfNodesWithMinimumDataValue()
		{
			if (currentScalarComponent != null)
			{
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
			else if (currentVectorComponents != null)
			{
				return enumerateVectorMagnitudes().IndicesOfMinElements().ToArray();
			}
			return new int[0]; //Array.Empty<int>();
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

		private bool hasElementSomeUndefinedNodeValues(int elementId)
		{
			if (elementsWithUndefinedNodeValues == null)
			{
				// build cache
				elementsWithUndefinedNodeValues = new HashSet<int>(getElementsWithUndefinedNodeValues());

				IEnumerable<int> getElementsWithUndefinedNodeValues()
				{
					foreach (var element in mesh.Elements)
					{
						foreach (var node in element.IterateThroughAllNodes())
						{
							double value = GetDataValue(node);
							if (double.IsNaN(value))
							{
								yield return element.ID;
								break;
							}
						}
					}
				}
			}

			return elementsWithUndefinedNodeValues.Contains(elementId);
		}

		private double getVectorMagnitude(int index)
		{
			Debug.Assert(currentVectorComponents != null);
			Vector3d v = new Vector3d(currentVectorComponents.ElementAtOrDefault(0)?.Values[index] ?? 0.0, currentVectorComponents.ElementAtOrDefault(1)?.Values[index] ?? 0.0, currentVectorComponents.ElementAtOrDefault(2)?.Values[index] ?? 0.0);
			return v.Length;
		}

		private IEnumerable<double> enumerateVectorMagnitudes()
		{
			Debug.Assert(currentVectorComponents != null);
			int length = currentVectorComponents.ElementAtOrDefault(0)?.Values.Length ?? 0;
			for (int i = 0; i < length; i++)
			{
				yield return getVectorMagnitude(i);
			}
		}

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

				if (currentVectorComponents.Count > 3)
				{
					throw new InvalidOperationException($"Three vector components are maximum. Got {currentVectorComponents.Count} components instead.");
				}

				if (!currentVectorComponents.All(c => c.Location == DataLocationType.Points))
				{
					throw new NotSupportedException("The only supported data location for vector field is Points");
				}

				var xComponent = currentVectorComponents.ElementAtOrDefault(0);
				var yComponent = currentVectorComponents.ElementAtOrDefault(1);
				var zComponent = currentVectorComponents.ElementAtOrDefault(2);

				IntervalD xRange = IntervalD.Zero;
				IntervalD yRange = IntervalD.Zero;
				IntervalD zRange = IntervalD.Zero;

				var positions = new List<Vector3>(mesh.NodesEdgesIncidence.Count);
				var vectors = new List<Vector3>(mesh.NodesEdgesIncidence.Count);

				foreach (Node node in mesh.NodesEdgesIncidence.Keys)
				{
					double x = xComponent?.Values[node.ID] ?? 0.0;
					double y = yComponent?.Values[node.ID] ?? 0.0;
					double z = zComponent?.Values[node.ID] ?? 0.0;

					if (double.IsNaN(x) || double.IsNaN(y) || double.IsNaN(z)) // continue if values are missing
					{
						continue;
					}

					if (x.IsAlmostZero() && y.IsAlmostZero() && z.IsAlmostZero()) // continue if vector is almost zero
					{
						continue;
					}

					xRange.MergeWith(x);
					yRange.MergeWith(y);
					zRange.MergeWith(z);

					positions.Add(node.Position);
					vectors.Add(new Vector3((float)x, (float)y, (float)z));
				}

				double xMaxValue = xRange.GetMaxAbsValue();
				double yMaxValue = yRange.GetMaxAbsValue();
				double zMaxValue = zRange.GetMaxAbsValue();

				double maxAbsValue = Math.Max(Math.Max(xMaxValue, yMaxValue), zMaxValue);

				if (maxAbsValue.IsAlmostZero())
				{
					return null; // do not construct vector field if max value is too small
				}

				Vector3 largestElementDimensions = mesh.Edges.Aggregate(Vector3.Zero, (max, edge) => accumulateMaxDimension(max, edge.EndNode.Position - edge.BeginNode.Position));
				double scale;
				if (xMaxValue == maxAbsValue)
				{
					scale = largestElementDimensions.X / xMaxValue;
				}
				else if (yMaxValue == maxAbsValue)
				{
					scale = largestElementDimensions.Y / yMaxValue;
				}
				else
				{
					scale = largestElementDimensions.Z / zMaxValue;
				}

				return new VectorField(positions, vectors, mesh.MinimalElementRadius, scale, Settings.ArrowLengthFactor, Settings.InvertVectorArrows);
			}
		}

		private static Vector3 accumulateMaxDimension(Vector3 max, Vector3 v) => new Vector3(Math.Max(max.X, Math.Abs(v.X)), Math.Max(max.Y, Math.Abs(v.Y)), Math.Max(max.Z, Math.Abs(v.Z)));

		#endregion
	}
}
