using System;
using System.Collections.Generic;
using System.Text;
using OpenTK;
using MeshEditor.Cuts;

namespace MeshEditor.Data
{
	/// <summary>
	/// Represents 3D hexahedral element
	/// </summary>
	public class Hexahedron : Element3D
	{
		public Hexahedron(int id, ElementType type, params Node[] nodes)
			: base(id, type, nodes)
		{ }

		public override IEnumerable<Element2D> GenerateAllFaces(Dictionary<Element2D, Node[]> additionalNodes)
		{
			Element2D face;
			bool quadratic = ApproximationIsQuadratic;
			Vector3 center = GetCenter();
			face = GenerateFaceWithNodes(ref center, nodes[0], nodes[3], nodes[7], nodes[4]);
			if (face != null)
			{
				if (quadratic)
					additionalNodes[face] = new Node[] { nodes[11], nodes[15], nodes[19], nodes[12] };
				yield return face;
			}
			face = GenerateFaceWithNodes(ref center, nodes[1], nodes[0], nodes[4], nodes[5]);
			if (face != null)
			{
				if (quadratic)
					additionalNodes[face] = new Node[] { nodes[8], nodes[12], nodes[16], nodes[13] };
				yield return face;
			}
			face = GenerateFaceWithNodes(ref center, nodes[2], nodes[1], nodes[5], nodes[6]);
			if (face != null)
			{
				if (quadratic)
					additionalNodes[face] = new Node[] { nodes[9], nodes[13], nodes[17], nodes[14] };
				yield return face;
			}
			face = GenerateFaceWithNodes(ref center, nodes[3], nodes[2], nodes[6], nodes[7]);
			if (face != null)
			{
				if (quadratic)
					additionalNodes[face] = new Node[] { nodes[10], nodes[14], nodes[18], nodes[15] };
				yield return face;
			}
			face = GenerateFaceWithNodes(ref center, nodes[0], nodes[1], nodes[2], nodes[3]);
			if (face != null)
			{
				if (quadratic)
					additionalNodes[face] = new Node[] { nodes[8], nodes[9], nodes[10], nodes[11] };
				yield return face;
			}
			face = GenerateFaceWithNodes(ref center, nodes[4], nodes[5], nodes[6], nodes[7]);
			if (face != null)
			{
				if (quadratic)
					additionalNodes[face] = new Node[] { nodes[16], nodes[17], nodes[18], nodes[19] };
				yield return face;
			}
		}

		private static readonly int[] nodeEdgeIndexArray = { 0, 1, 1, 2, 2, 3, 3, 0, 4, 5, 5, 6, 6, 7, 7, 4, 0, 4, 1, 5, 2, 6, 3, 7 };

		protected override int[] NodeEdgeIndexArray
		{
			get { return nodeEdgeIndexArray; }
		}
	}
}
