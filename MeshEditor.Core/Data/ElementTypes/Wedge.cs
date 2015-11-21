using System;
using System.Collections.Generic;
using System.Text;
using OpenTK;
using MeshEditor.Cuts;
using MeshEditor.Construction;

namespace MeshEditor.Data
{
	/// <summary>
	/// Represents 3D Triangular-prism element
	/// </summary>
	public class Wedge : Element3D
	{
		public Wedge(int id, ElementType type, params Node[] nodes)
			: base(id, type, nodes)
		{ }

		public override IEnumerable<Element2D> GenerateAllFaces(Dictionary<EdgeMark, Node> quadraticNodesCache)
		{
			Element2D face;
			bool quadratic = ApproximationIsQuadratic;
			Vector3 center = GetCenter();
			face = GenerateFaceWithNodes(1, ref center, nodes[0], nodes[1], nodes[2]);
			if (face != null)
			{
				if (quadratic)
				{
					quadraticNodesCache[new EdgeMark(nodes[0].ID, nodes[1].ID)] = nodes[6];
					quadraticNodesCache[new EdgeMark(nodes[1].ID, nodes[2].ID)] = nodes[7];
					quadraticNodesCache[new EdgeMark(nodes[2].ID, nodes[0].ID)] = nodes[8];
				}
				yield return face;
			}
			face = GenerateFaceWithNodes(2, ref center, nodes[3], nodes[5], nodes[4]);
			if (face != null)
			{
				if (quadratic)
				{
					quadraticNodesCache[new EdgeMark(nodes[3].ID, nodes[5].ID)] = nodes[14];
					quadraticNodesCache[new EdgeMark(nodes[5].ID, nodes[4].ID)] = nodes[13];
					quadraticNodesCache[new EdgeMark(nodes[4].ID, nodes[3].ID)] = nodes[12];
				}
				yield return face;
			}
			face = GenerateFaceWithNodes(3, ref center, nodes[1], nodes[0], nodes[3], nodes[4]);
			if (face != null)
			{
				if (quadratic)
				{
					quadraticNodesCache[new EdgeMark(nodes[1].ID, nodes[0].ID)] = nodes[6];
					quadraticNodesCache[new EdgeMark(nodes[0].ID, nodes[3].ID)] = nodes[9];
					quadraticNodesCache[new EdgeMark(nodes[3].ID, nodes[4].ID)] = nodes[12];
					quadraticNodesCache[new EdgeMark(nodes[4].ID, nodes[1].ID)] = nodes[10];
				}
				yield return face;
			}
			face = GenerateFaceWithNodes(4, ref center, nodes[2], nodes[1], nodes[4], nodes[5]);
			if (face != null)
			{
				if (quadratic)
				{
					quadraticNodesCache[new EdgeMark(nodes[2].ID, nodes[1].ID)] = nodes[7];
					quadraticNodesCache[new EdgeMark(nodes[1].ID, nodes[4].ID)] = nodes[10];
					quadraticNodesCache[new EdgeMark(nodes[4].ID, nodes[5].ID)] = nodes[13];
					quadraticNodesCache[new EdgeMark(nodes[5].ID, nodes[2].ID)] = nodes[11];
				}
				yield return face;
			}
			face = GenerateFaceWithNodes(5, ref center, nodes[0], nodes[2], nodes[5], nodes[3]);
			if (face != null)
			{
				if (quadratic)
				{
					quadraticNodesCache[new EdgeMark(nodes[0].ID, nodes[2].ID)] = nodes[8];
					quadraticNodesCache[new EdgeMark(nodes[2].ID, nodes[5].ID)] = nodes[11];
					quadraticNodesCache[new EdgeMark(nodes[5].ID, nodes[3].ID)] = nodes[14];
					quadraticNodesCache[new EdgeMark(nodes[3].ID, nodes[0].ID)] = nodes[9];
				}
				yield return face;
			}		
		}

		private static readonly int[] nodeEdgeIndexArray = { 0, 1, 1, 2, 2, 0, 3, 4, 4, 5, 5, 3, 0, 3, 1, 4, 2, 5 };

		protected override int[] NodeEdgeIndexArray
		{
			get { return nodeEdgeIndexArray; }
		}
	}
}
