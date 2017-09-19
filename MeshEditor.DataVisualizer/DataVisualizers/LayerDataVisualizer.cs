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

namespace MeshEditor.DataVisualizer
{
	internal class LayerDataVisualizer : DataVisualizerBase
	{
		#region Fields, constructor

		readonly GeometryDescription geometry;
		Dictionary<double, ComponentDataDescription> data;

		DataSelection dataSelection;
		ComponentDataDescription currentDataComponent;

		public LayerDataVisualizer(GeometryDescription geometry, IVisualizerSettings settings)
			: base(settings)
		{
			Debug.Assert(geometry != null);
			this.geometry = geometry;
			data = new Dictionary<double, ComponentDataDescription>();
		}

		#endregion

		#region Properties

		public DataSelection DataSelection => dataSelection;

		public override bool DisplayColors => base.DisplayColors && currentDataComponent != null;

		#endregion

		#region Public methods

		public void UpdateDataSelection(DataSelection newDataSelection, Dictionary<double, ComponentDataDescription> scalarComponentsTimeStepMap, ILookup<double, ComponentDataDescription> vectorComponentsTimeStepMap)
		{
			Debug.Assert(data != null || scalarComponentsTimeStepMap != null);

			dataSelection = newDataSelection;
			if (scalarComponentsTimeStepMap != null)
			{
				data = scalarComponentsTimeStepMap;
			}

			if (!data.TryGetValue(dataSelection.TimeStep, out currentDataComponent))
			{
				currentDataComponent = null;
			}

			setupColorScale();
			buildDataDescription();

			// TODO: build vector arrows vbo from vectorComponents
			// TODO: check if Location is in nodes, otherwise it is not supported
			// TODO: if vectorDataIndex is null, clear vector arrows vbo; vectorComponents should be empty
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

		//private async Task reloadMeshAsync(SolutionHub solutionHub, DataSelection newDataSelection, CancellationToken cancellationToken, SceneFacade scene, Action<string, int> progressReport)
		//{
		//	progressReport?.Invoke("Loading geometry", -1);
		//	var geometry = await solutionHub.LoadGeometryAsync(newDataSelection.LayerId, newDataSelection.MeshIndex, cancellationToken);
		//	AttributeDescription elementPropertiesAttribute = null;
		//	if (newDataSelection.ElementPropertyAttributeIndex.HasValue)
		//	{
		//		elementPropertiesAttribute = await solutionHub.LoadAttributeAsync(newDataSelection.LayerId, newDataSelection.ElementPropertyAttributeIndex.Value, cancellationToken);
		//	}

		//	var meshFileParser = new LayerMeshFileParser(solutionDescriptionText, geometry, elementPropertiesAttribute);
		//	await scene.ReloadMeshInLayerAsync(newDataSelection.LayerId, meshFileParser, cancellationToken, progressReport);

		//	currentGeometry = geometry;
		//}

		private void setupColorScale()
		{
			Settings.ColorScale.SetMinMaxValue(minValue: GetMinimumDataValue(), maxValue: GetMaximumDataValue());
		}

		//private void clearData()
		//{
		//	data = null;
		//	dataSelection = null;
		//	currentDataComponent = null;
		//}

		private void buildDataDescription()
		{
			if (dataSelection == null)
				ScalarDataDescription = "";
			else
				ScalarDataDescription = dataSelection.FieldName + Environment.NewLine + dataSelection.ComponentName + Environment.NewLine + "t = " + dataSelection.TimeStep;
		}

		#endregion
	}
}
