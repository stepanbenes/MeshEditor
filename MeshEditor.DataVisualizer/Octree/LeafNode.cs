using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using MeshEditor.DataVisualizer.Data;
using MeshEditor.Data;
using OpenTK.Graphics.OpenGL;
using System.Diagnostics;
using MeshEditor.CoreInterface;

namespace MeshEditor.DataVisualizer.Octree
{
	/// <summary>
	/// Represents leaf (external, outer, terminal) node of octree that contains data.
	/// </summary>
	public class LeafNode : TreeNode
	{

		#region Fields, Properties, Constructor

		public LeafNode(Vector3 lowerBounds, Vector3 upperBounds)
			: base(lowerBounds, upperBounds)
		{ }

		public LeafNode(Vector3 lowerBounds, float size)
			: this(lowerBounds, new Vector3(lowerBounds.X + size, lowerBounds.Y + size, lowerBounds.Z + size))
		{ }

		#endregion

		#region Overrides

		//public override void InsertDataValue(DataValueComponent dataValueComponent, int dataIndex, Dictionary<int, Node> nodeIndexMap, int depth)
		//{
		//	DataAbstract summary;
		//	if (!DataCatalog.TryGetValue(dataIndex, out summary))
		//	{
		//		clearTempValues(); // this is first time, clear previous data
		//		summary = DataCatalog[dataIndex] = new DataAbstract();
		//	}
		//	summary.MergeValue(dataValueComponent.Value);
			
		//	Debug.Assert(TempValues != null);
		//	AddTempValue(dataValueComponent);
		//}

		public override void InsertDataValues(IEnumerable<DataValueComponent> dataValueComponents, int dataIndex, Dictionary<int, Node> nodeIndexMap, int depth, double globalRange, ApproximationMethod method)
		{
			DataAbstract summary;
			if (!DataCatalog.TryGetValue(dataIndex, out summary))
				summary = DataCatalog[dataIndex] = new DataAbstract();
			foreach (DataValueComponent dataValueComponent in dataValueComponents)
			{
				//Debug.Assert(nodeIndexMap.ContainsKey(dataValueComponent.EntityNumber));
				//Vector3 position = nodeIndexMap[dataValueComponent.EntityNumber].Position;
				summary.MergeValue(dataValueComponent);
			}
			
			//ComputeRegressionPlane(dataValueComponents, nodeIndexMap, dataIndex);
			//ComputeCornerValues(dataValueComponents, nodeIndexMap, dataIndex);
			Debug.Assert(dataValueComponents is List<DataValueComponent>);
			ComputeApproximation(dataValueComponents as List<DataValueComponent>, nodeIndexMap, dataIndex, method);
		}

		public override LeafNode GetLeafOnPosition(ref Vector3 position)
		{
			Debug.Assert(position.X >= LowerBounds.X && position.X <= UpperBounds.X && position.Y >= LowerBounds.Y && position.Y <= UpperBounds.Y && position.Z >= LowerBounds.Z && position.Z <= UpperBounds.Z);
			return this;
		}

		public override DataAbstract GetDataAbstract(ref Vector3 position, int dataIndex)
		{
			Debug.Assert(position.X >= LowerBounds.X && position.X <= UpperBounds.X && position.Y >= LowerBounds.Y && position.Y <= UpperBounds.Y && position.Z >= LowerBounds.Z && position.Z <= UpperBounds.Z);
			Debug.Assert(DataCatalog.ContainsKey(dataIndex));
			return DataCatalog[dataIndex];
		}

		public override TreeNode GetTreeNodeOnPositionWithData(ref Vector3 position, int dataIndex)
		{
			Debug.Assert(position.X >= LowerBounds.X && position.X <= UpperBounds.X && position.Y >= LowerBounds.Y && position.Y <= UpperBounds.Y && position.Z >= LowerBounds.Z && position.Z <= UpperBounds.Z);
			Debug.Assert(DataCatalog.ContainsKey(dataIndex));
			return this;
		}

		//public override void ClearTemporaryData()
		//{
		//	clearTempValues();
		//}

		#endregion

		#region Public methods

		#endregion

	}
}
