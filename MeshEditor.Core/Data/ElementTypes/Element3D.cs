using System;
using System.Collections.Generic;
using System.Text;
using OpenTK;
using Wintellect.PowerCollections;

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

		public abstract IEnumerable<Element2D> GenerateAllFaces(Dictionary<Element2D, Node[]> additionalNodes);

		public override Vector3 GetCenter()
		{
			Vector3 center = Vector3.Zero;
			foreach (Node n in nodes)
				center += n.Position;
			return center / nodes.Length;
		}

		protected Element2D GenerateFaceWithNodes(ref Vector3 centerOfElement, params Node[] nodesOfFace)
		{
			List<Node> distinctNodes = new List<Node>(nodesOfFace.Length);
			distinctNodes.Add(nodesOfFace[0]); // predpokladam, ze tam alespon jeden prvek je
			for (int i = 1; i < nodesOfFace.Length; i++)
			{
				if(!distinctNodes.Contains(nodesOfFace[i]))
					distinctNodes.Add(nodesOfFace[i]);
			}
			if (distinctNodes.Count <= 2)
				return null;

			Element2D result;
			if (distinctNodes.Count == 3)
				result = new TriangleFaceOfElement3D(this, distinctNodes[0], distinctNodes[1], distinctNodes[2]);
			else if (distinctNodes.Count == 4)
				result = new QuadFaceOfElement3D(this, distinctNodes[0], distinctNodes[1], distinctNodes[2], distinctNodes[3]);
			else
				throw new ArgumentException("Too much nodes for one face.");

			// jeste spravne natocim normalu, pokud smeruje dovnitr prvku
			if (Vector3.Dot(centerOfElement - distinctNodes[0].Position, result.NormalVector) > 0)
			{
				result.InvertNormalVector();
				result.ReverseNodeOrder();
			}

			return result;
		}
	}
}
