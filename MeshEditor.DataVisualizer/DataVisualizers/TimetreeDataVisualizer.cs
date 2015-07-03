using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using MeshEditor.CoreInterface;
using MeshEditor.Data;
using MeshEditor.DataVisualizer.Data;
using MeshEditor.DataVisualizer.Octree;

using GL = OpenTK.Graphics.OpenGL.GL;
using EnableCap = OpenTK.Graphics.OpenGL.EnableCap;
using MeshEditor.DataVisualizer.IO;
using MeshEditor.Graphics;

namespace MeshEditor.DataVisualizer
{
	public class TimetreeDataVisualizer : OctreeDataVisualizer
	{

		#region Fields, constructor

		IntervalD timeRange;
		bool compressTime;
		double[] fixedTimes;

		Dictionary<int, int> compressionTable;
		Dictionary<int, double> dataTimes;

		public TimetreeDataVisualizer()
		{
			timeRange = IntervalD.Zero;
		}

		#endregion

		#region Overrides

		protected override DataIndex translateDataIndex(int index)
		{
			Debug.Assert(compressionTable.ContainsKey(index));
			Debug.Assert(dataTimes.ContainsKey(index));
			return new DataIndex(compressionTable[index], dataTimes[index]);
		}

		public override void LoadData(IApproximationParameters approximationParameters, string[] filenames, LongOpNotifier longOpNotifier)
		{
			this.compressTime = approximationParameters.CompressTime;
			this.fixedTimes = approximationParameters.FixedTimes ?? new double[0];

			base.LoadData(approximationParameters, filenames, longOpNotifier);

			unifyTimeSteps(longOpNotifier);
		}

		public override void FinishUp()
		{
			base.FinishUp();

			Settings.ScalarDataIndex = Settings.ScalarDataIndex.WithTime(timeRange.Min); // set to min value
		}

		public override bool IsContinuousInTime(out IntervalD timeRange)
		{
			timeRange = this.timeRange;
			// TODO: different timeRange according to current data index?
			return true;
		}

		public override void DrawItems(PropertyColorsMode propertyColorsMode)
		{
			base.DrawItems(propertyColorsMode);

			if (Settings.DrawGrid)
			{
				bool lightEnabled = GL.IsEnabled(EnableCap.Lighting);
				if (lightEnabled)
					GL.Disable(EnableCap.Lighting);

				GL.LineWidth(1f);
				GL.Disable(EnableCap.Lighting);
				GL.Color3(0f, 0f, 1f); // blue

				octreeRoot.Draw(Settings.ScalarDataIndex);

				if (lightEnabled)
					GL.Enable(EnableCap.Lighting);
			}
		}

		#endregion

		#region Private methods

		private void unifyTimeSteps(LongOpNotifier longOpNotifier)
		{
			Debug.Assert(longOpNotifier != null);
			longOpNotifier.ReportProgress(-1, taskName: "Time compression");

			Dictionary<DataType, DataInfo> dataTypeMap = new Dictionary<DataType, DataInfo>();
			this.compressionTable = new Dictionary<int, int>();
			this.dataTimes = new Dictionary<int, double>();

			timeRange = IntervalD.InvertedMaxMin;

			foreach (var pair in DataIndexMap)
			{
				DataInfo dataInfo = pair.Key;
				int dataIndex = pair.Value;

				DataInfo pivot;
				if (!dataTypeMap.TryGetValue(dataInfo.DataType, out pivot))
					pivot = dataTypeMap[dataInfo.DataType] = dataInfo;

				int pivotIndex = DataIndexMap[pivot];

				double time = dataInfo.Time;

				timeRange.MergeWith(time);

				for (int i = 0; i < dataInfo.DataType.ComponentCount; i++)
				{
					compressionTable[dataIndex + i] = pivotIndex + i;
					dataTimes[dataIndex + i] = time;
				}
			}

			// Walk through octree and join same data values from different time steps to DataSequence object
			try
			{
				octreeRoot.ProcessBelongingNodes(nodeIndexMap.Values, (treeNode, nodes) => unifyTimeStepsInTreeNode(treeNode, nodes, longOpNotifier));
			}
			catch (OperationCanceledException)
			{
				// Do nothing, Adjust DataIndexMap and quit method
			}

			// compress DataIndexMap - relink data indexes of data info objects to the pivots for each data time set
			foreach (DataInfo dataInfo in DataIndexMap.Keys.ToArray())
			{
				DataIndexMap[dataInfo] = DataIndexMap[dataTypeMap[dataInfo.DataType]];
			}
		}

		private void unifyTimeStepsInTreeNode(TreeNode treeNode, IEnumerable<Node> nodesInTreeNode, LongOpNotifier longOpNotifier)
		{
			if (longOpNotifier.IsCancelled)
				throw new OperationCanceledException();

			// Replace DataAbstract objects with new DataSequence object, Remove excess memory

			Dictionary<int, DataAbstract> compressedDataCatalog = new Dictionary<int, DataAbstract>();
			foreach (var pair in treeNode.DataCatalog)
			{
				int dataIndex = pair.Key;
				DataAbstract timeStamp = pair.Value;

				int clusterIndex = compressionTable[dataIndex];

				DataAbstract timeCluster;
				if (!compressedDataCatalog.TryGetValue(clusterIndex, out timeCluster))
					timeCluster = compressedDataCatalog[clusterIndex] = new DataSequence();

				DataSequence sequence = timeCluster as DataSequence;

				Debug.Assert(sequence != null);

				sequence.AddTimeStamp(dataTimes[dataIndex], timeStamp);
			}

			foreach (DataSequence sequence in compressedDataCatalog.Values)
			{
				sequence.SetupDataDescription();
				if (this.compressTime)
				{
					sequence.CompressTimeInDomain(nodesInTreeNode.Select(node => node.Position), fixedTimes);
				}
			}

			treeNode.DataCatalog = compressedDataCatalog;
		}

		#endregion

	}
}
