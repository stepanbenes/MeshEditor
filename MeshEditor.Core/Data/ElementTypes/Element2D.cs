using System;
using System.Collections.Generic;
using System.Text;
using OpenTK;
using MeshEditor.Cuts;
using System.Diagnostics;
using System.Linq;

namespace MeshEditor.Data
{
	/// <summary>
	/// abstraktni bazova trida reprezentujici 2D konecny prvek nebo plochu 3D prvku
	/// </summary>
	public abstract class Element2D : Element
	{

		#region Field, Constructor, Property

		protected Vector3 normal;
		private Element2D[] twinElements;

		public Element2D(int id, ElementType type)
			: base(id, type)
		{ }

		public Vector3 NormalVector
		{
			get { return normal; }
		}

		#endregion

		#region Abstract public methods & properties

		public abstract void Draw();
		public abstract void UpdateNormalVector();
		public abstract IEnumerable<WingedEdge> IterateThroughAllEdges();
		//public abstract void ReplaceNode(Node from, Node to);
		public abstract float ComputeArea();
		public abstract void ReverseNodeOrder();

		public abstract Node[] GetNodeArray();

		#endregion

		#region Public instance methods

		public void InvertNormalVector()
		{
			normal = -normal;
		}

		public IEnumerable<Element2D> GetNeighbors(float borderAngleLimit)
		{
			foreach (WingedEdge edge in IterateThroughAllEdges())
			{
				if (edge.FeatureAngle < borderAngleLimit)
				{
					if (this == edge.Face1 && edge.Face2 != null)
						yield return edge.Face2;
					else if (edge.Face1 != null)
						yield return edge.Face1;
				}
			}
		}

		//public bool IsEdgeCounterClockWise(Node n1, Node n2)
		//{
		//    List<Node> nodes = new List<Node>(IterateThroughAllNodes());
		//    for (int i = 0; i < nodes.Count; i++)
		//    {
		//        if (nodes[i] == n1)
		//            return nodes[(i + 1) % nodes.Count] == n2;
		//        if (nodes[i] == n2)
		//            return nodes[(nodes.Count + i - 1) % nodes.Count] == n1;
		//    }
		//    throw new ArgumentException("This face does not contain specified nodes.");
		//}

		public IEnumerable<Node> IterateThroughAllEdgeMiddleNodes()
		{
			foreach (WingedEdge e in IterateThroughAllEdges())
			{
				QuadraticEdge q = e as QuadraticEdge;
				if (q != null)
					yield return q.MiddleNode;
			}
		}

		public override IEnumerable<Node> IterateThroughAllNodesIncludingEdgeMiddleNodes()
		{
			foreach (Node n in IterateThroughAllNodes())
				yield return n;
			foreach (Node n in IterateThroughAllEdgeMiddleNodes())
				yield return n;
		}

		public override string ToString()
		{
			if (this is IFaceOfElement3D)
			{
				StringBuilder text = new StringBuilder();

				Element3D parent = ((IFaceOfElement3D)this).ParentElement;
				if (parent != null)
				{
					text.Append("Face of element ");
					text.Append(parent.ID);
				}
				else
					text.Append("Face");

				text.Append(" | (Nodes: ");

				List<Node> allNodes = new List<Node>(IterateThroughAllNodesIncludingEdgeMiddleNodes());
				for (int i = 0; i < allNodes.Count; i++)
				{
					text.Append(allNodes[i].ID);
					if (i == allNodes.Count - 1) // posledni prvek
						text.Append(")");
					else
						text.Append(", ");
				}

				text.Append(" | Parent element type: ");
				text.Append(parent.ElementType.ToString());

				text.Append(" | Property: ");
				text.Append(Property.ToString());

				return text.ToString();
			}
			return base.ToString();
		}

		public override IEnumerable<EdgeIntersection> GetAllIntersectionsOfEdgesWithPlane(Vector3 planeNormal, float planeOffset)
		{
			foreach (WingedEdge edge in IterateThroughAllEdges())
			{
				float intersection;
				if (Utilities.Functions.LinePlaneIntersection(edge.BeginNode.Position, edge.EndNode.Position, ref planeNormal, planeOffset, out intersection))
					yield return new EdgeIntersection(edge.BeginNode, edge.EndNode, intersection);
			}
		}

		#region Twin elements operations

		public bool HasTwinElements
		{
			get { return twinElements != null; }
		}

		public int NumberOfTwinElements
		{
			get { return HasTwinElements ? twinElements.Length : 0; }
		}

		public IEnumerable<Element2D> GetTwinElements()
		{
			return HasTwinElements ? twinElements : Enumerable.Empty<Element2D>();
		}

		public Element2D PopFirstTwinElement()
		{
			Debug.Assert(HasTwinElements);
			Element2D firstTwinElement = twinElements[0];
			RemoveTwinElement(firstTwinElement);
			return firstTwinElement;
		}

		public void MoveTwinElementsTo(Element2D other)
		{
			Debug.Assert(other != null);
			Debug.Assert(!other.HasTwinElements);
			other.twinElements = this.twinElements;
			this.twinElements = null;
		}

		public bool ContainsTwinElement(Element2D twinElementToTest)
		{
			Debug.Assert(twinElementToTest != null);
			if (twinElements == null)
				return false;

			for (int i = 0; i < twinElements.Length; i++)
			{
				if (twinElements[i].Equals(twinElementToTest))
					return true;
			}
			return false;
		}

		public void AddTwinElement(Element2D twinElementToAdd)
		{
			Debug.Assert(twinElementToAdd != null);
			Debug.Assert(twinElementToAdd.CompareTo(this) > 0);

			if (twinElements == null)
			{
				twinElements = new Element2D[] { twinElementToAdd };
			}
			else
			{
				//Array.Resize(ref twinElements, twinElements.Length + 1);
				//twinElements[twinElements.Length - 1] = twinElementToAdd;

				Element2D[] newArray = new Element2D[twinElements.Length + 1];
				newArray[twinElements.Length] = twinElementToAdd;
				for (int i = 0; i < twinElements.Length; i++)
				{
					int comparison = twinElementToAdd.CompareTo(twinElements[i]);
					Debug.Assert(comparison != 0);
					if (comparison > 0)
					{
						newArray[i] = twinElements[i];
					}
					else
					{
						newArray[i] = twinElementToAdd;
						Array.Copy(twinElements, i, newArray, i + 1, twinElements.Length - i);
						break;
					}
				}
				Debug.Assert(Utilities.Functions.CheckIfArrayIsStrictlyIncreasing(newArray));
				twinElements = newArray;
			}
		}

		public bool RemoveTwinElement(Element2D twinElementToRemove)
		{
			Debug.Assert(twinElementToRemove != null);
			if (twinElements == null)
			{
				return false;
			}

			int index = Array.IndexOf(twinElements, twinElementToRemove);

			if (index < 0)
			{
				return false;
			}

			if (twinElements.Length == 1)
			{
				Debug.Assert(index == 0);
				twinElements = null;
			}
			else
			{
				Debug.Assert(twinElements.Length > 1);
				Element2D[] newArray = new Element2D[twinElements.Length - 1];
				if (index > 0)
				{
					Array.Copy(twinElements, 0, newArray, 0, index);
				}
				if (index < twinElements.Length - 1)
				{
					Array.Copy(twinElements, index + 1, newArray, index, twinElements.Length - index - 1);
				}
				Debug.Assert(Utilities.Functions.CheckIfArrayIsStrictlyIncreasing(newArray));
				twinElements = newArray;
            }

			return true;
		}

		#endregion

		#endregion

	}
}
