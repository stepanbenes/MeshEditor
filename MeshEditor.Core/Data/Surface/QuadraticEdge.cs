using System;
using System.Collections.Generic;
using System.Text;

namespace MeshEditor.Data
{
	/// <summary>
	/// hrana prvku s kvadratickou aproximaci (normalni hrana s pridanym stredovym uzlem)
	/// </summary>
	public class QuadraticEdge : WingedEdge
	{
		private Node middleNode;

		public Node MiddleNode
		{
			get { return middleNode; }
		}

		public QuadraticEdge(Node begin, Node end, Node middle, Element2D face1)
			: base(begin, end, face1)
		{
			this.middleNode = middle;
		}

		public override string ToString()
		{
			return "Edge | (Nodes: " + beginNode.ID + ", " + endNode.ID + ", " + middleNode.ID + ") | Approximation: Quadratic | Property: " + property;
		}
	}
}
