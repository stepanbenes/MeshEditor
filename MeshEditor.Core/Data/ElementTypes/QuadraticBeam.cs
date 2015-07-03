using System;
using System.Collections.Generic;
using System.Text;

namespace MeshEditor.Data
{
	/// <summary>
	/// 1D prvek (Beam) s kvadratickou aproximaci (obsahuje dodatecny uzel)
	/// </summary>
	public class QuadraticBeam : Beam
	{
		private Node middleNode;

		public Node MiddleNode
		{
			get { return middleNode; }
		}

		public override int NodeCount
		{
			get { return 3; }
		}
		
		public QuadraticBeam(int id, ElementType type, Node n1, Node n2, Node middle)
			: base(id, type, n1, n2)
		{
			this.middleNode = middle;
		}

		public override IEnumerable<Node> IterateThroughAllNodes()
		{
			yield return beginNode;
			yield return endNode;
			yield return middleNode;
		}

		public override IEnumerable<Node> IterateThroughAllNodesIncludingEdgeMiddleNodes()
		{
			return IterateThroughAllNodes();
		}

		public override string ToString()
		{
			return "Beam ID: " + id + " | (Nodes: " + beginNode.ID + ", " + endNode.ID + ", " + middleNode.ID + ") | Approximation: " + ApproximationString + " | Property: " + Property;
		}
	}
}
