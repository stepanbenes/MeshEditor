using MeshEditor.DataVisualizer.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MeshEditor.DataVisualizer
{
	public interface IDataVisualizerController
	{
		IVisualizerSettings Settings { get; }

		Dictionary<DataInfo, int> DataIndexMap { get; }

		void SetDeformationDataIndex(DataIndex dataIndex);

		bool IsContinuousInTime(out IntervalD timeRange);
	}
}
