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

namespace MeshEditor.DataVisualizer
{
	internal class LayerDataVisualizer : DataVisualizerBase
	{
		#region Fields, constructor

		Dictionary<double, ComponentDataDescription> data;
		DataSelection dataSelection;

		public LayerDataVisualizer(Guid layerId)
		{
			LayerId = layerId;
		}

		#endregion

		#region Properties

		public Guid LayerId { get; }

		public DataSelection DataSelection => dataSelection;

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

			var dataComponent = data[newDataSelection.TimeStep];
			
			Settings.ColorScale.SetMinMaxValue(dataComponent.Values.Min(), dataComponent.Values.Max()); // TODO: handle NaN values

			dataSelection = newDataSelection;
			buildDataDescription();
		}

		public override int GetDataColor(Node node, Element element)
		{
			ComponentDataDescription dataComponent;
			if (data != null && dataSelection != null && data.TryGetValue(dataSelection.TimeStep, out dataComponent) && dataComponent.Location == DataLocationType.Points)
			{
				double dataValue = dataComponent.Values[node.ID];
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
