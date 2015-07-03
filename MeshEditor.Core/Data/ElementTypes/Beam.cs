using System;
using System.Collections.Generic;
using System.Text;
using OpenTK;
using MeshEditor.Cuts;

namespace MeshEditor.Data
{
	/// <summary>
	/// Represents 1D Element
	/// </summary>
	public class Beam : Element
	{
		protected Node beginNode, endNode;

		public Node BeginNode
		{
			get { return beginNode; }
		}

		public Node EndNode
		{
			get { return endNode; }
		}

		public override Node SignificantNode
		{
			get { return beginNode; }
		}

		public override int NodeCount
		{
			get { return 2; }
		}
		
		public Beam(int id, ElementType type, Node n1, Node n2)
			: base(id, type)
		{
			this.beginNode = n1;
			this.endNode = n2;
		}

		public override IEnumerable<Node> IterateThroughAllNodes()
		{
			yield return beginNode;
			yield return endNode;
		}

		public override IEnumerable<Node> IterateThroughAllNodesIncludingEdgeMiddleNodes()
		{
			return IterateThroughAllNodes();
		}

		public override string ToString()
		{
			return "Beam ID: " + id + " | (Nodes: " + beginNode.ID + ", " + endNode.ID + ")" + (ApproximationIsQuadratic ? (" | Approximation: " + ApproximationString) : string.Empty) + " | Property: " + Property;
		}

		public override IEnumerable<EdgeIntersection> GetAllIntersectionsOfEdgesWithPlane(Vector3 planeNormal, float planeOffset)
		{
			float intersection;
			if (Utilities.Functions.LinePlaneIntersection(beginNode.Position, endNode.Position, ref planeNormal, planeOffset, out intersection))
				yield return new EdgeIntersection(beginNode, endNode, intersection);
		}

		public override IEnumerable<EdgeIntersection> GetAllIntersectionsOfEdgesDataIsoSurface(double dataValue, double[] nodeValues)
		{
			float intersection;
			if (Utilities.Functions.ValueIsInInterval(dataValue, nodeValues[0], nodeValues[1], out intersection))
				yield return new EdgeIntersection(beginNode, endNode, intersection);
		}
	}
}
