using System;
using System.Collections.Generic;
using System.Text;
using OpenTK;

namespace MeshEditor.Data
{
	/// <summary>
	/// represents 3D tetrahedral finite element
	/// </summary>
	public class Tetrahedron : Element3D
	{
		public Tetrahedron(int id, ElementType type, params Node[] nodes)
			: base(id, type, nodes)
		{ }

		public override IEnumerable<Element2D> GenerateAllFaces(Dictionary<Element2D, Node[]> additionalNodes)
		{
			//Element2D f1 = new TriangleFaceOfElement3D(this, nodes[0], nodes[2], nodes[1]);
			//Element2D f2 = new TriangleFaceOfElement3D(this, nodes[0], nodes[3], nodes[2]);
			//Element2D f3 = new TriangleFaceOfElement3D(this, nodes[0], nodes[1], nodes[3]);
			//Element2D f4 = new TriangleFaceOfElement3D(this, nodes[1], nodes[2], nodes[3]);

			//if (ApproximationIsQuadratic)
			//{
			//    additionalNodes[f1] = new Node[] { nodes[6], nodes[5], nodes[4] };
			//    additionalNodes[f2] = new Node[] { nodes[7], nodes[9], nodes[6] };
			//    additionalNodes[f3] = new Node[] { nodes[4], nodes[8], nodes[7] };
			//    additionalNodes[f4] = new Node[] { nodes[5], nodes[9], nodes[8] };
			//}

			//return new Element2D[] { f1, f2, f3, f4 };



			Element2D face;
			bool quadratic = ApproximationIsQuadratic;
			Vector3 center = GetCenter();
			face = GenerateFaceWithNodes(ref center, nodes[0], nodes[2], nodes[1]);
			if (face != null)
			{
				if (quadratic)
					additionalNodes[face] = new Node[] { nodes[6], nodes[5], nodes[4] };
				yield return face;
			}
			face = GenerateFaceWithNodes(ref center, nodes[0], nodes[3], nodes[2]);
			if (face != null)
			{
				if (quadratic)
					additionalNodes[face] = new Node[] { nodes[7], nodes[9], nodes[6] };
				yield return face;
			}
			face = GenerateFaceWithNodes(ref center, nodes[0], nodes[1], nodes[3]);
			if (face != null)
			{
				if (quadratic)
					additionalNodes[face] = new Node[] { nodes[4], nodes[8], nodes[7] };
				yield return face;
			}
			face = GenerateFaceWithNodes(ref center, nodes[1], nodes[2], nodes[3]);
			if (face != null)
			{
				if (quadratic)
					additionalNodes[face] = new Node[] { nodes[5], nodes[9], nodes[8] };
				yield return face;
			}

		}

		public override IEnumerable<Vector3> GetAllIntersectionsOfEdgesWithPlane(Vector3 pointOnPlane, Vector3 planeNormal)
		{
			if (ApproximationIsQuadratic)
				throw new NotImplementedException();
			int[] indexArray = { 0, 1, 1, 2, 2, 0, 0, 3, 1, 3, 2, 3 };
			for (int i = 0; i < indexArray.Length; )
			{
				Vector3 intersection;
				if (Utilities.Functions.LinePlaneIntersection(nodes[indexArray[i++]].Position, nodes[indexArray[i++]].Position, ref pointOnPlane, ref planeNormal, out intersection))
					yield return intersection;
			}
		}
	}
}
