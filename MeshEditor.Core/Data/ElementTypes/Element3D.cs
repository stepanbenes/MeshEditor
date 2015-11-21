using System;
using System.Collections.Generic;
using System.Text;
using OpenTK;
using MeshEditor.Cuts;
using MeshEditor.Construction;

namespace MeshEditor.Data
{
	/// <summary>
	/// abstraktni trida, ktera je zakladem vsech 3D konecnych prvku
	/// </summary>
	public abstract class Element3D : Element
	{
		protected Node[] nodes;

		public Element3D(int id, ElementType type, params Node[] nodes)
			: base(id, type)
		{
			this.nodes = nodes;
		}
		
		public override Node SignificantNode
		{
			get { return nodes[0]; }
		}

		public override IEnumerable<Node> IterateThroughAllNodes()
		{
			return nodes;
		}

		public override IEnumerable<Node> IterateThroughAllNodesIncludingEdgeMiddleNodes()
		{
			return nodes;
		}

		public override int NodeCount
		{
			get { return nodes.Length; }
		}

		public abstract IEnumerable<Element2D> GenerateAllFaces(Dictionary<EdgeMark, Node> quadraticNodesCache);

		public override Vector3 GetCenter()
		{
			Vector3 center = Vector3.Zero;
			foreach (Node n in nodes)
				center += n.Position;
			return center / nodes.Length;
		}

		protected Element2D GenerateFaceWithNodes(int order, ref Vector3 centerOfElement, Node node1, Node node2, Node node3)
		{
			if (node1 == node2 || node1 == node3 || node2 == node3) // face is collapsed to line, discard it
				return null;

			Element2D result = new TriangleFaceOfElement3D(this, order, node1, node2, node3);

			// jeste spravne natocim normalu, pokud smeruje dovnitr prvku
			if (Vector3.Dot(centerOfElement - node1.Position, result.NormalVector) > 0)
			{
				result.InvertNormalVector();
				result.ReverseNodeOrder();
			}
			return result;
		}

		protected Element2D GenerateFaceWithNodes(int order, ref Vector3 centerOfElement, Node node1, Node node2, Node node3, Node node4)
		{
			if (node1 == node2 || node1 == node3 || node1 == node4)
				return GenerateFaceWithNodes(order, ref centerOfElement, node2, node3, node4); // face is collapsed to triangle
			if (node2 == node3 || node2 == node4)
				return GenerateFaceWithNodes(order, ref centerOfElement, node1, node3, node4); // face is collapsed to triangle
			if (node3 == node4)
				return GenerateFaceWithNodes(order, ref centerOfElement, node1, node2, node4); // face is collapsed to triangle

			Element2D result = new QuadFaceOfElement3D(this, order, node1, node2, node3, node4);

			// jeste spravne natocim normalu, pokud smeruje dovnitr prvku
			if (Vector3.Dot(centerOfElement - node1.Position, result.NormalVector) > 0)
			{
				result.InvertNormalVector();
				result.ReverseNodeOrder();
			}
			return result;
		}

		protected abstract int[] NodeEdgeIndexArray { get; }

		public override IEnumerable<EdgeIntersection> GetAllIntersectionsOfEdgesWithPlane(Vector3 planeNormal, float planeOffset)
		{
			for (int i = 0; i < NodeEdgeIndexArray.Length; )
			{
				Node node1 = nodes[NodeEdgeIndexArray[i++]];
				Node node2 = nodes[NodeEdgeIndexArray[i++]];
				float intersection;
				if (Utilities.Functions.LinePlaneIntersection(node1.Position, node2.Position, ref planeNormal, planeOffset, out intersection))
					yield return new EdgeIntersection(node1, node2, intersection);
			}
		}

		public override IEnumerable<EdgeIntersection> GetAllIntersectionsOfEdgesDataIsoSurface(double dataValue, double[] nodeValues)
		{
			for (int i = 0; i < NodeEdgeIndexArray.Length; )
			{
				int index1 = NodeEdgeIndexArray[i++];
				int index2 = NodeEdgeIndexArray[i++];
				Node node1 = nodes[index1];
				Node node2 = nodes[index2];
				float intersection;
				if (Utilities.Functions.ValueIsInInterval(dataValue, nodeValues[index1], nodeValues[index2], out intersection))
					yield return new EdgeIntersection(node1, node2, intersection);
			}
		}
	}
}
