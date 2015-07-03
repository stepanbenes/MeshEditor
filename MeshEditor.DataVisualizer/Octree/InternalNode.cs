using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using MeshEditor.DataVisualizer.Data;
using MeshEditor.Data;
using System.Diagnostics;
using MeshEditor.DataVisualizer.Mathematics;
using MeshEditor.CoreInterface;

namespace MeshEditor.DataVisualizer.Octree
{
	public class InternalNode : TreeNode
	{

		#region Fields, Properties, Constructor

		private readonly TreeNode[] children;

		public TreeNode[] Children { get { return children; } }

		public InternalNode(Vector3 lowerBounds, Vector3 upperBounds)
			: base(lowerBounds, upperBounds)
		{
			children = new TreeNode[8];

			children[0] = new LeafNode(Center, UpperBounds); // XYZ
			children[1] = new LeafNode(new Vector3(Center.X, Center.Y, LowerBounds.Z), new Vector3(UpperBounds.X, UpperBounds.Y, Center.Z)); // XY-Z
			children[2] = new LeafNode(new Vector3(LowerBounds.X, Center.Y, LowerBounds.Z), new Vector3(Center.X, UpperBounds.Y, Center.Z)); // -XY-Z
			children[3] = new LeafNode(new Vector3(LowerBounds.X, Center.Y, Center.Z), new Vector3(Center.X, UpperBounds.Y, UpperBounds.Z)); // -XYZ

			children[4] = new LeafNode(new Vector3(Center.X, LowerBounds.Y, Center.Z), new Vector3(UpperBounds.X, Center.Y, UpperBounds.Z)); // X-YZ
			children[5] = new LeafNode(new Vector3(Center.X, LowerBounds.Y, LowerBounds.Z), new Vector3(UpperBounds.X, Center.Y, Center.Z)); // X-Y-Z
			children[6] = new LeafNode(LowerBounds, Center); // -X-Y-Z
			children[7] = new LeafNode(new Vector3(LowerBounds.X, LowerBounds.Y, Center.Z), new Vector3(Center.X, Center.Y, UpperBounds.Z)); // -X-YZ
		}

		#endregion

		#region Overrides

		public override void InsertDataValues(IEnumerable<DataValueComponent> dataValueComponents, int dataIndex, Dictionary<int, Node> nodeIndexMap, int depth, double globalRange, ApproximationMethod method)
		{
			List<DataValueComponent>[] _tempValues = new List<DataValueComponent>[8];

			DataAbstract parentSummary;
			bool fresh = false;
			if (!DataCatalog.TryGetValue(dataIndex, out parentSummary))
			{
				parentSummary = DataCatalog[dataIndex] = new DataAbstract();
				fresh = true;
			}

			foreach (DataValueComponent dataValueComponent in dataValueComponents)
			{
				Node node;
				if (nodeIndexMap.TryGetValue(dataValueComponent.EntityNumber, out node))
				{
					Vector3 position = node.Position;
					int quadrant = getIndexOfQuadrantOnPosition(ref position); // find quadrant
					if (_tempValues[quadrant] == null)
						_tempValues[quadrant] = new List<DataValueComponent>();
					_tempValues[quadrant].Add(dataValueComponent);

					if (fresh)
						parentSummary.MergeValue(dataValueComponent);
				}
			}

			if (fresh)
			{
				//ComputeCornerValues(_tempValues.Where(list => list != null).SelectMany(list => list), nodeIndexMap, dataIndex);
				ComputeApproximation(_tempValues, nodeIndexMap, dataIndex, method);
			}

			if (double.IsNaN(globalRange))
				globalRange = parentSummary.MaxValue - parentSummary.MinValue;

			if (PropagateValuesFromParentToChildren(parentSummary, globalRange)) // Condition #2: Second condition is based on minimum allowed relative error of chosen approximation function. Algorithm replacing discrete data points with continuous approximation function also calculates relative error of the method. This number is then compared with some preset fixed value, e.g. 5%.
			{
				for (int quadrant = 0; quadrant < 8; quadrant++)
				{
					if (_tempValues[quadrant] == null)
						continue;
					if (_tempValues[quadrant].Count < Math.Max(MinLeafEntityCount, Approximation.GetMinNumberOfDataPoints(method))) // Condition #1: First condition specifies minimum number of finite element nodes located in current octree node, that are necessary to compute approximation function, e.g. least square trilinear method needs at least 8 values.
						continue;

					children[quadrant].InsertDataValues(_tempValues[quadrant], dataIndex, nodeIndexMap, depth + 1, globalRange, method);

					LeafNode leafNode = children[quadrant] as LeafNode;
					// expansion
					if (leafNode != null && depth < MaxDepth - 1) // Condition #3: Third condition describes maximum depth of octal tree. Each level of the tree exponentially increases memory consumption of data values stored in octree. Maximum depth is therefore artificially set to some acceptable value, e.g. 9. However, this depth should not be reached in common cases, it is ensured by condition #1
					{
						Debug.Assert(leafNode.DataCatalog.ContainsKey(dataIndex));

						if (PropagateValuesFromParentToChildren(/*childSummary=*/ leafNode.DataCatalog[dataIndex], globalRange)) // Condition #2
						{ // expand leaf node
							InternalNode newInternalNode = new InternalNode(leafNode.LowerBounds, leafNode.UpperBounds);
							newInternalNode.DataCatalog = leafNode.DataCatalog; // pass reference to data catalog
							newInternalNode.InsertDataValues(_tempValues[quadrant], dataIndex, nodeIndexMap, depth + 1, globalRange, method);
							children[quadrant] = newInternalNode; // replace leaf node with new internal node, forget leafNode object (will be GCed)
							leafNode = null;
						}
					}
					_tempValues[quadrant] = null; // no more needed
				}
			}

			//ComputeRegressionPlane(_tempValues.Where(list => list != null).SelectMany(list => list), nodeIndexMap, dataIndex);
			
		}

		//private bool fallThrough = false;

		//public override void InsertDataValue(DataValueComponent dataValueComponent, int dataIndex, Dictionary<int, Node> nodeIndexMap, int depth)
		//{
		//	DataAbstract parentSummary;
		//	if (!DataCatalog.TryGetValue(dataIndex, out parentSummary))
		//	{
		//		clearTempValues(); // this is first time, clear previous data
		//		fallThrough = false;
		//		parentSummary = DataCatalog[dataIndex] = new DataAbstract();
		//	}
		//	parentSummary.MergeValue(dataValueComponent.Value);

		//	Node node;
		//	Vector3 position;
		//	int quadrant;
		//	TreeNode child;

		//	if (!fallThrough && propagateValueFromParentToChild(parentSummary))
		//	{
		//		foreach (DataValueComponent component in TempValues)
		//		{
		//			if (!nodeIndexMap.TryGetValue(component.EntityNumber, out node))
		//				continue;

		//			position = node.Position;
		//			quadrant = getIndexOfQuadrantOnPosition(ref position); // find quadrant
		//			child = children[quadrant];

		//			child.InsertDataValue(component, dataIndex, nodeIndexMap, depth + 1);
		//		}
		//		clearTempValues();
		//		fallThrough = true;
		//	}

		//	//Console.WriteLine("dataIndex: " + dataIndex + " depth: " + depth);
			
		//	if (!nodeIndexMap.TryGetValue(dataValueComponent.EntityNumber, out node))
		//		return;

		//	position = node.Position;
		//	quadrant = getIndexOfQuadrantOnPosition(ref position); // find quadrant
		//	child = children[quadrant];

		//	if (fallThrough)
		//	{
		//		child.InsertDataValue(dataValueComponent, dataIndex, nodeIndexMap, depth + 1);

		//		LeafNode leafNode = child as LeafNode;
		//		// expansion
		//		if (leafNode != null && depth < LeafNode.MaxDepth - 1) // if not reached maximum depth
		//		{
		//			Debug.Assert(leafNode.DataCatalog.ContainsKey(dataIndex));
					
		//			if (leafNodeNeedsExpansion(parentSummary, /*childSummary=*/ leafNode.DataCatalog[dataIndex]))
		//			{ // expand leaf node
		//				InternalNode newInternalNode = new InternalNode(leafNode.LowerBounds, leafNode.UpperBounds);
		//				foreach (DataValueComponent value in leafNode.TempValues)
		//				{
		//					newInternalNode.InsertDataValue(value, dataIndex, nodeIndexMap, depth + 1);
		//				}
		//				child = children[quadrant] = newInternalNode; // replace leaf node with new internal node, forget leafNode object (will be GCed)
		//				leafNode = null;
		//			}
		//		}
		//	}
		//	else
		//	{
		//		AddTempValue(dataValueComponent);
		//	}
		//}

		public override void Draw(DataIndex dataIndex, double globalRange)
		{
			if (double.IsNaN(globalRange) && DataCatalog.ContainsKey(dataIndex.Index))
				globalRange = DataCatalog[dataIndex.Index].MaxValue - DataCatalog[dataIndex.Index].MinValue;

			bool any = false;
			foreach (TreeNode child in children)
			{
				if (child.DataCatalog.ContainsKey(dataIndex.Index) && child.DataCatalog[dataIndex.Index].ContainsTime(dataIndex.Time))
				{
					child.Draw(dataIndex, globalRange);
					any = true;
				}
			}
			if (!any)
			{
				base.Draw(dataIndex, globalRange);
			}
		}

		public override LeafNode GetLeafOnPosition(ref Vector3 position)
		{
			return children[getIndexOfQuadrantOnPosition(ref position)].GetLeafOnPosition(ref position);
		}

		public override DataAbstract GetDataAbstract(ref Vector3 position, int dataIndex)
		{
			DataAbstract data;
			if(!DataCatalog.TryGetValue(dataIndex, out data))
				return null;
			TreeNode child = children[getIndexOfQuadrantOnPosition(ref position)];
			DataAbstract childData;
			if (!child.DataCatalog.TryGetValue(dataIndex, out childData))
				return data;
			return child.GetDataAbstract(ref position, dataIndex);
		}

		public override TreeNode GetTreeNodeOnPositionWithData(ref Vector3 position, int dataIndex)
		{
			if (!DataCatalog.ContainsKey(dataIndex))
				return null;
			TreeNode child = children[getIndexOfQuadrantOnPosition(ref position)];
			if (!child.DataCatalog.ContainsKey(dataIndex))
				return this;
			return child.GetTreeNodeOnPositionWithData(ref position, dataIndex);
		}

		//public override double GetValueApproximationInNode(Node node, int dataIndex)
		//{
		//	if (!DataCatalog.ContainsKey(dataIndex))
		//		return double.NaN;
		//	double value = base.GetValueApproximationInNode(node, dataIndex);
		//	Vector3 position = node.Position;
		//	TreeNode child = children[getIndexOfQuadrantOnPosition(ref position)];
		//	if (!child.DataCatalog.ContainsKey(dataIndex))
		//		return value;
		//	return value + child.GetValueApproximationInNode(node, dataIndex);
		//}

		public override double GetValueApproximationAt(ref Vector4 spacetime, int dataIndex)
		{
			if (!DataCatalog.ContainsKey(dataIndex))
				return double.NaN;
			double value = base.GetValueApproximationAt(ref spacetime, dataIndex);
			Vector3 position = spacetime.Xyz;
			TreeNode child = children[getIndexOfQuadrantOnPosition(ref position)];
			if (!child.DataCatalog.ContainsKey(dataIndex) || !child.DataCatalog[dataIndex].ContainsTime(spacetime.W))
				return value;
			return value + child.GetValueApproximationAt(ref spacetime, dataIndex);
		}

		public override void ProcessBelongingNodes(IEnumerable<Node> nodes, Action<TreeNode, IEnumerable<Node>> operation)
		{
			// Process this tree node
			base.ProcessBelongingNodes(nodes, operation);

			// Process children nodes
			List<Node>[] sortedNodes = new List<Node>[8];
			for (int i = 0; i < 8; i++)
				sortedNodes[i] = new List<Node>();
			foreach (Node node in nodes)
			{
				Vector3 position = node.Position;
				sortedNodes[getIndexOfQuadrantOnPosition(ref position)].Add(node);
			}
			for (int i = 0; i < 8; i++)
			{
				if (sortedNodes[i].Count > 0)
					children[i].ProcessBelongingNodes(sortedNodes[i], operation);
			}
		}

		#endregion

		#region Private methods

		private int getIndexOfQuadrantOnPosition(ref Vector3 position)
		{
			bool right = position.X >= Center.X;
			bool top = position.Y >= Center.Y;
			bool front = position.Z >= Center.Z;
			int quadrant;
			if (right)
			{
				if (top)
				{
					quadrant = front ? 0 : 1;
				}
				else // bottom
				{
					quadrant = front ? 4 : 5;
				}
			}
			else // left
			{
				if (top)
				{
					quadrant = front ? 3 : 2;
				}
				else // bottom
				{
					quadrant = front ? 7 : 6;
				}
			}
			return quadrant;
		}

		#endregion

		#region Static methods

		public static bool PropagateValuesFromParentToChildren(DataAbstract dataSummary, double globalRange)
		{
			Debug.Assert(!double.IsNaN(dataSummary.MinValue) && !double.IsNaN(dataSummary.MaxValue) && !double.IsInfinity(dataSummary.MinValue) && !double.IsInfinity(dataSummary.MaxValue));

			//double range = dataSummary.MaxValue - dataSummary.MinValue;
			//return range > Constants.Epsilon;
			if (dataSummary.MaxError <= Common.Epsilon || globalRange <= Common.Epsilon)
				return false;
			double relativeError = dataSummary.MaxError / globalRange;
			return relativeError > MinRelativeErrorToExpand;
		}

		//public static bool LeafNodeNeedsExpansion(DataAbstract parentSummary, DataAbstract childSummary, double globalRange)
		//{
		//	if (childSummary.ItemCount <= MinLeafEntityCountToExpand)
		//		return false;

		//	//Debug.Assert(parentSummary.MaxAbsoluteError > Constants.Epsilon);

		//	//double range = Math.Abs(parentSummary.MaxValue - parentSummary.MinValue);
		//	//double relativeError = (range > Constants.Epsilon) ? (childSummary.MaxAbsoluteError / range) : childSummary.MaxAbsoluteError;
		//	//return relativeError > MinRelativeErrorToExpand;

		//	return PropagateValuesFromParentToChildren(childSummary, globalRange);
		//}

		#endregion

	}
}
