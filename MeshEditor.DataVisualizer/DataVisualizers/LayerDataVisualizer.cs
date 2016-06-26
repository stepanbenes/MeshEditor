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

namespace MeshEditor.DataVisualizer
{
	internal class LayerDataVisualizer : DataVisualizerBase
	{
		#region Fields, constructor

		Dictionary<double, ComponentDataDescription> data;
		DataSelection dataSelection;
		ComponentDataDescription currentDataComponent;

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

		public void UpdateDataSelection(SolutionHub solutionHub, DataSelection newDataSelection)
		{
			if (newDataSelection == null)
			{
				clearData();
				return;
			}

			if (dataSelection == null || dataSelection.DataIndex != newDataSelection.DataIndex)
			{
				Debug.Assert(solutionHub != null);
				data = solutionHub.LoadData(LayerId, newDataSelection.DataIndex).ToDictionary(d => d.TimeStep);
			}

			dataSelection = newDataSelection;
			data.TryGetValue(dataSelection.TimeStep, out currentDataComponent);
			setupColorScale();
			buildDataDescription();
		}

		private void setupColorScale()
		{
			if (currentDataComponent != null)
			{
				double? min = currentDataComponent.Values.Min(ignore: double.NaN);
				double? max = currentDataComponent.Values.Max(ignore: double.NaN);

				Settings.ColorScale.SetMinMaxValue(min ?? double.NaN, max ?? double.NaN);
			}
			else
			{
				Settings.ColorScale.SetMinMaxValue(double.NaN, double.NaN);
			}
		}

		public override int GetDataColor(Node node, Element element)
		{
			if (currentDataComponent != null && currentDataComponent.Location == DataLocationType.Points)
			{
				double dataValue = currentDataComponent.Values[node.ID];
				return GetColorForDataValue(dataValue);
			}
			return ColorScale.UndefinedValueColor;
		}

		#endregion

		#region Private methods

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
