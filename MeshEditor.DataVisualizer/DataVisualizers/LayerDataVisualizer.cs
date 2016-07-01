using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.Data;
using MeshEditor.DataVisualizer.Data;
using MeshEditor.LayerManager.Data;
using MeshEditor.SolutionManager;
using MeshEditor.LayerManager.Common;
using MeshEditor.IO;
using MeshEditor.DataVisualizer.IO;
using System.Threading;
using MeshEditor.CoreInterface;

namespace MeshEditor.DataVisualizer
{
	internal class LayerDataVisualizer : DataVisualizerBase
	{
		#region Fields, constructor

		Dictionary<double, ComponentDataDescription> data;
		DataSelection dataSelection;
		ComponentDataDescription currentDataComponent;

		GeometryDescription currentGeometry;

		public LayerDataVisualizer(Guid layerId)
		{
			LayerId = layerId;
		}

		#endregion

		#region Properties

		public Guid LayerId { get; }

		public DataSelection DataSelection => dataSelection;

		public override bool DisplayColors => base.DisplayColors && currentDataComponent != null;

		#endregion

		#region Public methods

		public async Task UpdateDataSelectionAsync(SolutionHub solutionHub, DataSelection newDataSelection, CancellationToken cancellationToken, SceneFacade scene, LongOpNotifier longOpNotifier)
		{
			if (newDataSelection == null)
			{
				clearData();
				return;
			}

			if (newDataSelection.MeshIndex != dataSelection?.MeshIndex)
			{
				await reloadMeshAsync(solutionHub, newDataSelection, cancellationToken, scene, longOpNotifier);
			}

			if (dataSelection == null || dataSelection.DataIndex != newDataSelection.DataIndex)
			{
				if (!newDataSelection.DataIndex.HasValue)
				{
					data = null;
				}
				else
				{
					Debug.Assert(solutionHub != null);
					var componentList = await solutionHub.LoadDataAsync(LayerId, newDataSelection.DataIndex.Value, cancellationToken);
					data = componentList.ToDictionary(d => d.TimeStep);
				}
			}

			dataSelection = newDataSelection;
			if (data == null || !data.TryGetValue(dataSelection.TimeStep, out currentDataComponent))
			{
				currentDataComponent = null;
			}
			setupColorScale();
			buildDataDescription();
		}

		private void setupColorScale()
		{
			Settings.ColorScale.SetMinMaxValue(minValue: GetMinimumDataValue(), maxValue: GetMaximumDataValue());
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

					Debug.Assert(currentGeometry != null);
					int cellOffset = (element.ID > 0) ? currentGeometry.CellOffsets[element.ID - 1] : 0;
					int? nodeIndex = element.IndexOfNode_IncludingMiddleNodes(node);
					Debug.Assert(nodeIndex.HasValue); // node has to be contained in element
					double value = currentDataComponent.Values[cellOffset + nodeIndex.Value]; // WARNING: correct node ordering is supposed
					return value;

				case DataLocationType.Cells:
					return currentDataComponent.Values[element.ID];
				default:
					throw new NotSupportedException();
			}
		}

		public override double GetDataValue(Node node, out double error)
		{
			error = 0.0;
			if (currentDataComponent != null && currentDataComponent.Location == DataLocationType.Points)
			{
				return currentDataComponent.Values[node.ID];
			}
			return double.NaN;
		}

		public override int[] GetIDsOfNodesWithMaximumDataValue()
		{
			if (currentDataComponent == null)
				return Array.Empty<int>();

			switch (currentDataComponent.Location)
			{
				case DataLocationType.Points:
					return currentDataComponent.Values.IndicesOfMaxElements().ToArray();
				case DataLocationType.CellPoints:
				case DataLocationType.Cells:
					return Array.Empty<int>(); // TODO: implement this. However, multiple elements are not supported by SignalElement function
				default:
					throw new NotSupportedException();
			}
		}

		public override int[] GetIDsOfNodesWithMinimumDataValue()
		{
			if (currentDataComponent == null)
				return Array.Empty<int>();

			switch (currentDataComponent.Location)
			{
				case DataLocationType.Points:
					return currentDataComponent.Values.IndicesOfMinElements().ToArray();
				case DataLocationType.CellPoints:
				case DataLocationType.Cells:
					return Array.Empty<int>(); // TODO: implement this. However, multiple elements are not supported by SignalElement function
				default:
					throw new NotSupportedException();
			}
		}

		public override double GetMaximumDataValue()
		{
			return currentDataComponent?.Values.Max(ignore: double.NaN) ?? double.NaN;
		}

		public override double GetMinimumDataValue()
		{
			return currentDataComponent?.Values.Min(ignore: double.NaN) ?? double.NaN;
		}

		#endregion

		#region Private methods

		private async Task reloadMeshAsync(SolutionHub solutionHub, DataSelection newDataSelection, CancellationToken cancellationToken, SceneFacade scene, LongOpNotifier longOpNotifier)
		{
			var geometry = await solutionHub.LoadGeometryAsync(LayerId, newDataSelection.MeshIndex, cancellationToken);
			AttributeDescription elementPropertiesAttribute = null;
			if (newDataSelection.ElementPropertyAttributeIndex.HasValue)
			{
				elementPropertiesAttribute = solutionHub.LoadAttribute(LayerId, newDataSelection.ElementPropertyAttributeIndex.Value);
			}

			await scene.ReloadMeshAsync(new LayerMeshFileParser(geometry, elementPropertiesAttribute), cancellationToken, longOpNotifier);

			currentGeometry = geometry;
		}

		private void clearData()
		{
			data = null;
			dataSelection = null;
			currentDataComponent = null;
		}

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
