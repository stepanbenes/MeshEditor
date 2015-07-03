using System;
using System.Collections.Generic;
using System.Text;
using OpenTK;
using MeshEditor.Cuts;

namespace MeshEditor.Data
{
	/// <summary>
	/// Represents Square-based pyramid element
	/// </summary>
	public class Pyramid : Element3D
	{
		public Pyramid(int id, ElementType type, params Node[] nodes)
			: base(id, type, nodes)
		{ }

		public override IEnumerable<Element2D> GenerateAllFaces(Dictionary<Element2D, Node[]> additionalNodes)
		{
			Element2D face;
			bool quadratic = ApproximationIsQuadratic;
			Vector3 center = GetCenter();
			face = GenerateFaceWithNodes(ref center, nodes[0], nodes[3], nodes[2], nodes[1]);
			if (face != null)
			{
				if (quadratic)
					additionalNodes[face] = new Node[] { nodes[8], nodes[7], nodes[6], nodes[5] };
				yield return face;
			}
			face = GenerateFaceWithNodes(ref center, nodes[0], nodes[1], nodes[4]);
			if (face != null)
			{
				if (quadratic)
					additionalNodes[face] = new Node[] { nodes[5], nodes[10], nodes[9] };
				yield return face;
			}
			face = GenerateFaceWithNodes(ref center, nodes[1], nodes[2], nodes[4]);
			if (face != null)
			{
				if (quadratic)
					additionalNodes[face] = new Node[] { nodes[6], nodes[11], nodes[10] };
				yield return face;
			}
			face = GenerateFaceWithNodes(ref center, nodes[2], nodes[3], nodes[4]);
			if (face != null)
			{
				if (quadratic)
					additionalNodes[face] = new Node[] { nodes[7], nodes[12], nodes[11] };
				yield return face;
			}
			face = GenerateFaceWithNodes(ref center, nodes[3], nodes[0], nodes[4]);
			if (face != null)
			{
				if (quadratic)
					additionalNodes[face] = new Node[] { nodes[8], nodes[9], nodes[12] };
				yield return face;
			}

		}

		private static readonly int[] nodeEdgeIndexArray = { 0, 1, 1, 2, 2, 3, 3, 0, 0, 4, 1, 4, 2, 4, 3, 4 };

		protected override int[] NodeEdgeIndexArray
		{
			get { return nodeEdgeIndexArray; }
		}
	}
}
