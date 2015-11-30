using System;
using System.Collections.Generic;
using System.Text;

using OpenTK;
using OpenTK.Graphics.OpenGL;
using MeshEditor.Cuts;

namespace MeshEditor.Data
{
	/// <summary>
	/// Represents 2D quadrilateral element
	/// </summary>
	public class Quadrilateral : Element2D
	{
		protected Node node1, node2, node3, node4;
		protected WingedEdge edge1, edge2, edge3, edge4;

		public Quadrilateral(int id, ElementType type, Node n1, Node n2, Node n3, Node n4)
			: base(id, type)
		{
			this.node1 = n1;
			this.node2 = n2;
			this.node3 = n3;
			this.node4 = n4;
			edge1 = edge2 = edge3 = edge4 = null;
		}

		public override int NodeCount
		{
			get { return 4; }
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

		public Node Node4
		{
			get { return node4; }
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

		public WingedEdge Edge4
		{
			get { return edge4; }
			set { edge4 = value; }
		}

		public override Node SignificantNode
		{
			get { return node1; }
		}

		public override Vector3 NormalVector
		{
			get
			{
				Vector3 normal = Vector3.Cross(node2.Position - node1.Position, node4.Position - node1.Position);
				normal.Normalize();
				return normal;
			}
		}

		public override void Draw()
		{
			// provedu teselaci - rozdelim na dva trojuhelniky
			// triangle 1
			GL.Vertex3(node1.Position);
			GL.Vertex3(node2.Position);
			GL.Vertex3(node3.Position);
			// triangle 2
			GL.Vertex3(node1.Position);
			GL.Vertex3(node3.Position);
			GL.Vertex3(node4.Position);
		}

		public override IEnumerable<WingedEdge> IterateThroughAllEdges()
		{
			yield return edge1;
			yield return edge2;
			yield return edge3;
			yield return edge4;
		}

		public override IEnumerable<Node> IterateThroughAllNodes()
		{
			yield return node1;
			yield return node2;
			yield return node3;
			yield return node4;
		}

		//public override void ReplaceNode(Node from, Node to)
		//{
		//    if (node1 == from)
		//        node1 = to;
		//    else if (node2 == from)
		//        node2 = to;
		//    else if (node3 == from)
		//        node3 = to;
		//    else if (node4 == from)
		//        node4 = to;
		//    else
		//        return; //throw new ArgumentException("Face does not contain this node.");

		//    UpdateNormalVector();
		//}

		public override float ComputeArea()
		{
			Vector3 diagonal = node3.Position - node1.Position;
			float area1 = Vector3.Cross(node2.Position - node1.Position, diagonal).Length * 0.5f;
			float area2 = Vector3.Cross(diagonal, node4.Position - node1.Position).Length * 0.5f;
			return area1 + area2;
		}

		public override bool ContainsNode(Node n)
		{
			return n == node1 || n == node2 || n == node3 || n == node4;
		}

		public override Node[] GetNodeArray()
		{
			return new Node[] { node1, node2, node3, node4 };
		}

		public override void ReverseNodeOrder()
		{
			Utilities.Functions.Swap(ref node2, ref node4);
			Utilities.Functions.Swap(ref edge2, ref edge4);
		}

		public override IEnumerable<EdgeIntersection> GetAllIntersectionsOfEdgesDataIsoSurface(double dataValue, double[] nodeValues)
		{
			float parameter;
			if (Utilities.Functions.ValueIsInInterval(dataValue, nodeValues[0], nodeValues[1], out parameter))
				yield return new EdgeIntersection(node1, node2, parameter);
			if (Utilities.Functions.ValueIsInInterval(dataValue, nodeValues[1], nodeValues[2], out parameter))
				yield return new EdgeIntersection(node2, node3, parameter);
			if (Utilities.Functions.ValueIsInInterval(dataValue, nodeValues[2], nodeValues[3], out parameter))
				yield return new EdgeIntersection(node3, node4, parameter);
			if (Utilities.Functions.ValueIsInInterval(dataValue, nodeValues[3], nodeValues[0], out parameter))
				yield return new EdgeIntersection(node4, node1, parameter);
		}
	}
}
