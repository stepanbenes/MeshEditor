using System;
using System.Collections.Generic;
using System.Text;
using OpenTK;

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

		public override IEnumerable<Element2D> GenerateAllFaces(Dictionary<Element2D, Node[]> additionalNodes)
		{
			Element2D face;
			bool quadratic = ApproximationIsQuadratic;
			Vector3 center = GetCenter();
			face = GenerateFaceWithNodes(ref center, nodes[0], nodes[1], nodes[2]);
			if (face != null)
			{
				if (quadratic)
					additionalNodes[face] = new Node[] { nodes[6], nodes[7], nodes[8] };
				yield return face;
			}
			face = GenerateFaceWithNodes(ref center, nodes[3], nodes[5], nodes[4]);
			if (face != null)
			{
				if (quadratic)
					additionalNodes[face] = new Node[] { nodes[14], nodes[13], nodes[12] };
				yield return face;
			}
			face = GenerateFaceWithNodes(ref center, nodes[1], nodes[0], nodes[3], nodes[4]);
			if (face != null)
			{
				if (quadratic)
					additionalNodes[face] = new Node[] { nodes[6], nodes[9], nodes[12], nodes[10] };
				yield return face;
			}
			face = GenerateFaceWithNodes(ref center, nodes[2], nodes[1], nodes[4], nodes[5]);
			if (face != null)
			{
				if (quadratic)
					additionalNodes[face] = new Node[] { nodes[7], nodes[10], nodes[13], nodes[11] };
				yield return face;
			}
			face = GenerateFaceWithNodes(ref center, nodes[0], nodes[2], nodes[5], nodes[3]);
			if (face != null)
			{
				if (quadratic)
					additionalNodes[face] = new Node[] { nodes[8], nodes[11], nodes[14], nodes[9] };
				yield return face;
			}		
		}

		public override IEnumerable<Vector3> GetAllIntersectionsOfEdgesWithPlane(Vector3 pointOnPlane, Vector3 planeNormal)
		{
			if (ApproximationIsQuadratic)
				throw new NotImplementedException();

			int[] indexArray = { 0, 1, 1, 2, 2, 0, 3, 4, 4, 5, 5, 3, 0, 3, 1, 4, 2, 5 };
			for (int i = 0; i < indexArray.Length; )
			{
				Vector3 intersection;
				if (Utilities.Functions.LinePlaneIntersection(nodes[indexArray[i++]].Position, nodes[indexArray[i++]].Position, ref pointOnPlane, ref planeNormal, out intersection))
					yield return intersection;
			}
		}
	}
}
