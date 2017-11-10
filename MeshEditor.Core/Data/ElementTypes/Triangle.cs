using System;
using System.Collections.Generic;
using System.Text;

using OpenTK;
using OpenTK.Graphics.OpenGL;
using MeshEditor.Cuts;
using static MeshEditor.Common.HelperFunctions;

namespace MeshEditor.Data
{
	/// <summary>
	/// Represents 2D Triangle element
	/// </summary>
	public class Triangle : Element2D
	{
		protected Node node1, node2, node3;
		protected WingedEdge edge1, edge2, edge3;

		public Triangle(int id, ElementType type, Node n1, Node n2, Node n3)
			: base(id, type)
		{
			this.node1 = n1;
			this.node2 = n2;
			this.node3 = n3;
			edge1 = edge2 = edge3 = null;
		}

		public override int NodeCount
		{
			get { return 3; }
		}

		public Node Node1
		{
			get { return node1; }
		}

		public Node Node2
		{
			get { return node2; }
		}

		public Node Node3
		{
			get { return node3; }
		}

		public WingedEdge Edge1
		{
			get { return edge1; }
			set { edge1 = value; }
		}

		public WingedEdge Edge2
		{
			get { return edge2; }
			set { edge2 = value; }
		}

		public WingedEdge Edge3
		{
			get { return edge3; }
			set { edge3 = value; }
		}

		public override Node SignificantNode
		{
			get { return node1; }
		}

		public override Vector3 NormalVector
		{
			get
			{
				Vector3 normal = Vector3.Cross(node2.Position - node1.Position, node3.Position - node1.Position);
				normal.Normalize();
				return normal;
			}
		}

		public override void Draw()
		{
			GL.Vertex3(node1.Position);
			GL.Vertex3(node2.Position);
			GL.Vertex3(node3.Position);
		}

		public override IEnumerable<WingedEdge> IterateThroughAllEdges()
		{
			yield return edge1;
			yield return edge2;
			yield return edge3;
		}

		public override IEnumerable<Node> IterateThroughAllNodes()
		{
			yield return node1;
			yield return node2;
			yield return node3;
		}

		//public override void ReplaceNode(Node from, Node to)
		//{
		//    if (node1 == from)
		//        node1 = to;
		//    else if (node2 == from)
		//        node2 = to;
		//    else if (node3 == from)
		//        node3 = to;
		//    else
		//        return;//throw new ArgumentException("Face does not contain this node.");

		//    UpdateNormalVector();
		//}

		public override float ComputeArea()
		{
			return Vector3.Cross(node2.Position - node1.Position, node3.Position - node1.Position).Length * 0.5f;
		}

		public override bool ContainsNode_IgnoreMiddleNodes(Node n)
		{
			return n == node1 || n == node2 || n == node3;
		}

		public Node GetRemainingNode(Node n1, Node n2)
		{
			if (node1 != n1 && node1 != n2)
				return node1;
			if (node2 != n1 && node2 != n2)
				return node2;
			return node3;
		}


		public override Node[] GetNodeArray()
		{
			return new Node[] { node1, node2, node3 };
		}

		public override void ReverseNodeOrder()
		{
			Swap(ref node2, ref node3);
			Swap(ref edge2, ref edge3);
		}

		public override IEnumerable<EdgeIntersection> GetAllIntersectionsOfEdgesDataIsoSurface(double dataValue, double[] nodeValues)
		{
			float parameter;
			if (Utilities.Functions.ValueIsInInterval(dataValue, nodeValues[0], nodeValues[1], out parameter))
				yield return new EdgeIntersection(node1, node2, parameter);
			if (Utilities.Functions.ValueIsInInterval(dataValue, nodeValues[1], nodeValues[2], out parameter))
				yield return new EdgeIntersection(node2, node3, parameter);
			if (Utilities.Functions.ValueIsInInterval(dataValue, nodeValues[2], nodeValues[0], out parameter))
				yield return new EdgeIntersection(node3, node1, parameter);
		}
	}
}
