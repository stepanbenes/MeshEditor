using System;
using System.Collections.Generic;
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
		int meshIndex, dataIndex;
		double timeStep;
		Dictionary<double, ComponentDataDescription> data;

		public LayerDataVisualizer(Guid layerId, int meshIndex)
		{
			LayerId = layerId;
			MeshIndex = meshIndex;
		}

		public Guid LayerId { get; }

		public int MeshIndex
		{
			get { return meshIndex; }
			set
			{
				if (meshIndex != value)
				{
					meshIndex = value;
					clearData();
				}
			}
		}

		public int DataIndex => dataIndex;

		public double TimeStep => timeStep;
		
		public void LoadData(SolutionHub solutionHub, int dataIndex, double timeStep)
		{
			clearData();

			if (DataIndex != dataIndex)
			{
				data = solutionHub.LoadData(LayerId, dataIndex).ToDictionary(d => d.TimeStep);
				this.dataIndex = dataIndex;
			}

			var dataComponent = data[timeStep];
			Settings.ColorScale.SetMinMaxValue(dataComponent.Values.Min(), dataComponent.Values.Max());
			this.timeStep = timeStep;
		}

		private void clearData()
		{
			data = null;
			dataIndex = 0;
			timeStep = double.NaN;
		}

		public override int GetDataColor(Node node, Element element)
		{
			ComponentDataDescription dataComponent;
			if (data != null && data.TryGetValue(timeStep, out dataComponent) && dataComponent.Location == DataLocationType.Points)
			{
				double dataValue = dataComponent.Values[node.ID];
				return GetColorForDataValue(dataValue);
			}
			return ColorScale.UndefinedValueColor;
		}
	}
}
