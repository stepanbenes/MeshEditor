using System;
using System.Collections.Generic;
using System.Text;
using OpenTK;
using MeshEditor.Construction;
using MeshEditor.Cuts;

namespace MeshEditor.Data
{
	/// <summary>
	/// abstraktni bazova trida reprezentujici konecny prvek
	/// </summary>
	public abstract class Element : IItemWithSignificantPoint, ISelectable, IComparable, IComparable<Element>, IEquatable<Element>
	{

		#region Fields, constructor
		
		protected int id;
		//protected ApproximationType approximationType;
		protected ElementType elementType;
		private Property property;
		//protected Property[] edgeProperties;

		public Element(int id, ElementType type)
		{
			this.id = id;
			//this.approximationType = type;
			this.elementType = type;
			this.property = Property.Zero;
			//this.edgeProperties = null;
		}

		#endregion

		#region Public properties

		public int ID
		{
			get { return this.id; }
		}

		public ElementType ElementType
		{
			get { return elementType; }
		}

		public bool ApproximationIsQuadratic
		{
			get
			{ 
				return (int)elementType % 2 == 0; // WARNING: dependency on exact values of ElementType's options; ElementType with value 0 leads to quadratic approximation
			}
		}

		public string ApproximationString
		{
			get { return ApproximationIsQuadratic ? "Quadratic" : "Linear"; }
		}

		//public Property[] EdgeProperties
		//{
		//    get { return edgeProperties; }
		//    set { edgeProperties = value; }
		//}

		public abstract Node SignificantNode
		{
			get;
		}

		#endregion

		#region Public methods

		public abstract IEnumerable<EdgeIntersection> GetAllIntersectionsOfEdgesWithPlane(Vector3 planeNormal, float planeOffset);

		public abstract IEnumerable<EdgeIntersection> GetAllIntersectionsOfEdgesDataIsoSurface(double dataValue, double[] nodeValues);

		public abstract IEnumerable<Node> IterateThroughAllNodes();
		public abstract IEnumerable<Node> IterateThroughAllNodesIncludingEdgeMiddleNodes();
		public abstract int NodeCount { get; }

		public virtual bool ContainsNode(Node n)
		{
			foreach (Node test in IterateThroughAllNodes())
				if (n == test)
					return true;
			return false;
		}

		public virtual Vector3 GetCenter()
		{
			Vector3 center = Vector3.Zero;
			foreach (Node n in IterateThroughAllNodes())
				center += n.Position;
			return center / (float)NodeCount;
		}

		public override string ToString()
		{
			StringBuilder text = new StringBuilder();

			text.Append("Element ");
			text.Append(this.id);
			text.Append(" | (Nodes: ");

						
			List<Node> allNodes = new List<Node>(this.IterateThroughAllNodesIncludingEdgeMiddleNodes());
			for (int i = 0; i < allNodes.Count; i++)
			{
				text.Append(allNodes[i].ID);
				if (i == allNodes.Count - 1) // posledni prvek
					text.Append(")");
				else
					text.Append(", ");
			}

			text.Append(" | Type: ");
			text.Append(elementType.ToString());

			//text.Append(" | Type: ");
			//text.Append(this.GetType().Name);
			//text.Append(" | Approximation: ");
			//text.Append(ApproximationString);

			text.Append(" | Property: ");
			text.Append(property.ToString());

			return text.ToString();
		}

		#endregion

		#region Static members

		//public static int GetDimensionOfElementType(ElementType elementType)
		//{
		//    switch (elementType)
		//    {
		//        case ElementType.BeamLinear:
		//        case ElementType.BeamQuadratic:
		//            return 1;
		//        case ElementType.TriangleLinear:
		//        case ElementType.TriangleQuadratic:
		//        case ElementType.QuadLinear:
		//        case ElementType.QuadQuadratic:
		//            return 2;
		//        case ElementType.TetrahedronLinear:
		//        case ElementType.TetrahedronQuadratic:
		//        case ElementType.SquarePyramidLinear:
		//        case ElementType.SquarePyramidQuadratic:
		//        case ElementType.TriangularPrismLinear:
		//        case ElementType.TriangularPrismQuadratic:
		//        case ElementType.HexahedronLinear:
		//        case ElementType.HexahedronQuadratic:
		//            return 3;
		//        default:
		//            return 0;
		//    }
		//}

		/// <summary>
		/// Returns node count for element of specified type
		/// </summary>
		/// <param name="elementType">type of element</param>
		/// <returns>Number of nodes that has element with specified element type</returns>
		public static int MapElementTypeToNodeCount(ElementType elementType)
		{
			switch (elementType)
			{
				case ElementType.BeamLinear:
					return 2;
				case ElementType.BeamQuadratic:
					return 3;
				case ElementType.TriangleLinear:
					return 3;
				case ElementType.TriangleQuadratic:
					return 6;
				case ElementType.QuadLinear:
					return 4;
				case ElementType.QuadQuadratic:
					return 8;
				case ElementType.TetrahedronLinear:
					return 4;
				case ElementType.TetrahedronQuadratic:
					return 10;
				case ElementType.SquarePyramidLinear:
					return 5;
				case ElementType.SquarePyramidQuadratic:
					return 13;
				case ElementType.TriangularPrismLinear:
					return 6;
				case ElementType.TriangularPrismQuadratic:
					return 15;
				case ElementType.HexahedronLinear:
					return 8;
				case ElementType.HexahedronQuadratic:
					return 20;
				default:
					throw new ArgumentException("This argument is not supported", "elementType");
			}
		}

		/// <summary>
		/// Returns edge count for element of specified type
		/// </summary>
		/// <param name="elementType">type of element</param>
		/// <returns>Number of edges that has element with specified element type</returns>
		public static int MapElementTypeToEdgeCount(ElementType elementType)
		{
			switch (elementType)
			{
				case ElementType.BeamLinear:
				case ElementType.BeamQuadratic:
					return 1; /* or 0? */
				case ElementType.TriangleLinear:
				case ElementType.TriangleQuadratic:
					return 3;
				case ElementType.QuadLinear:
				case ElementType.QuadQuadratic:
					return 4;
				case ElementType.TetrahedronLinear:
				case ElementType.TetrahedronQuadratic:
					return 6;
				case ElementType.SquarePyramidLinear:
				case ElementType.SquarePyramidQuadratic:
					return 8;
				case ElementType.TriangularPrismLinear:
				case ElementType.TriangularPrismQuadratic:
					return 9;
				case ElementType.HexahedronLinear:
				case ElementType.HexahedronQuadratic:
					return 12;
				default:
					throw new ArgumentException(string.Format("Argument '{0}' is not supported.", elementType), "elementType");
			}
		}

		/// <summary>
		/// Returns face count for element of specified type
		/// </summary>
		/// <param name="elementType">type of element</param>
		/// <returns>Number of faces that has element with specified element type</returns>
		public static int MapElementTypeToFaceCount(ElementType elementType)
		{
			switch (elementType)
			{
				case ElementType.BeamLinear:
				case ElementType.BeamQuadratic:
					return 0;
				case ElementType.TriangleLinear:
				case ElementType.TriangleQuadratic:
					return 1;
				case ElementType.QuadLinear:
				case ElementType.QuadQuadratic:
					return 1;
				case ElementType.TetrahedronLinear:
				case ElementType.TetrahedronQuadratic:
					return 4;
				case ElementType.SquarePyramidLinear:
				case ElementType.SquarePyramidQuadratic:
					return 5;
				case ElementType.TriangularPrismLinear:
				case ElementType.TriangularPrismQuadratic:
					return 5;
				case ElementType.HexahedronLinear:
				case ElementType.HexahedronQuadratic:
					return 6;
				default:
					throw new ArgumentException(string.Format("Argument '{0}' is not supported.", elementType), "elementType");
			}
		}
		
		public static ApproximationType GetApproximationTypeFrom(ElementType elementType)
		{
			switch (elementType)
			{
				case ElementType.BeamLinear:
				case ElementType.TriangleLinear:
				case ElementType.QuadLinear:
				case ElementType.TetrahedronLinear:
				case ElementType.SquarePyramidLinear:
				case ElementType.TriangularPrismLinear:
				case ElementType.HexahedronLinear:
					return ApproximationType.Linear;
				case ElementType.BeamQuadratic:
				case ElementType.TriangleQuadratic:
				case ElementType.QuadQuadratic:
				case ElementType.TetrahedronQuadratic:
				case ElementType.SquarePyramidQuadratic:
				case ElementType.TriangularPrismQuadratic:
				case ElementType.HexahedronQuadratic:
					return ApproximationType.Quadratic;
				default:
					throw new ArgumentException(string.Format("Argument '{0}' is not supported.", elementType), "elementType");
			}
		}

		public static IEnumerable<EdgeMark> GetSequenceOfEdges(ElementType elementType, int[] nodeIDs)
		{
			// TODO: implement missing pyramid and triangular prism elements
			// depends on edge ordering!
			switch (elementType)
			{
				case ElementType.BeamLinear:
				case ElementType.BeamQuadratic:
					yield break; // beam has no edge (in mesh representation in this application)
				case ElementType.TriangleLinear:
				case ElementType.TriangleQuadratic:
					yield return new EdgeMark(nodeIDs[0], nodeIDs[1]);
					yield return new EdgeMark(nodeIDs[1], nodeIDs[2]);
					yield return new EdgeMark(nodeIDs[2], nodeIDs[0]);
					break;
				case ElementType.QuadLinear:
				case ElementType.QuadQuadratic:
					yield return new EdgeMark(nodeIDs[0], nodeIDs[1]);
					yield return new EdgeMark(nodeIDs[1], nodeIDs[2]);
					yield return new EdgeMark(nodeIDs[2], nodeIDs[3]);
					yield return new EdgeMark(nodeIDs[3], nodeIDs[0]);
					break;
				case ElementType.TetrahedronLinear:
				case ElementType.TetrahedronQuadratic:
					yield return new EdgeMark(nodeIDs[0], nodeIDs[1]);
					yield return new EdgeMark(nodeIDs[1], nodeIDs[2]);
					yield return new EdgeMark(nodeIDs[2], nodeIDs[0]);

					yield return new EdgeMark(nodeIDs[1], nodeIDs[3]);
					yield return new EdgeMark(nodeIDs[3], nodeIDs[2]);
					yield return new EdgeMark(nodeIDs[3], nodeIDs[0]);
					break;
				case ElementType.SquarePyramidLinear:
				case ElementType.SquarePyramidQuadratic:

					throw new NotImplementedException("Square pyramid edge ordering was not specified.");

					//yield return new EdgeMark(nodeIDs[0], nodeIDs[1]);
					//yield return new EdgeMark(nodeIDs[1], nodeIDs[2]);
					//yield return new EdgeMark(nodeIDs[2], nodeIDs[3]);
					//yield return new EdgeMark(nodeIDs[3], nodeIDs[0]);

					//yield return new EdgeMark(nodeIDs[0], nodeIDs[4]);
					//yield return new EdgeMark(nodeIDs[1], nodeIDs[4]);
					//yield return new EdgeMark(nodeIDs[2], nodeIDs[4]);
					//yield return new EdgeMark(nodeIDs[3], nodeIDs[4]);
					//break;
				case ElementType.TriangularPrismLinear:
				case ElementType.TriangularPrismQuadratic:

					throw new NotImplementedException("Triangular prism edge ordering was not specified.");

					//yield return new EdgeMark(nodeIDs[0], nodeIDs[1]);
					//yield return new EdgeMark(nodeIDs[1], nodeIDs[2]);
					//yield return new EdgeMark(nodeIDs[2], nodeIDs[0]);

					//yield return new EdgeMark(nodeIDs[3], nodeIDs[4]);
					//yield return new EdgeMark(nodeIDs[4], nodeIDs[5]);
					//yield return new EdgeMark(nodeIDs[5], nodeIDs[3]);

					//yield return new EdgeMark(nodeIDs[0], nodeIDs[3]);
					//yield return new EdgeMark(nodeIDs[1], nodeIDs[4]);
					//yield return new EdgeMark(nodeIDs[2], nodeIDs[5]);
					//break;
				case ElementType.HexahedronLinear:
				case ElementType.HexahedronQuadratic:
					yield return new EdgeMark(nodeIDs[0], nodeIDs[1]);
					yield return new EdgeMark(nodeIDs[1], nodeIDs[2]);
					yield return new EdgeMark(nodeIDs[2], nodeIDs[3]);
					yield return new EdgeMark(nodeIDs[3], nodeIDs[0]);

					yield return new EdgeMark(nodeIDs[0], nodeIDs[4]);
					yield return new EdgeMark(nodeIDs[1], nodeIDs[5]);
					yield return new EdgeMark(nodeIDs[2], nodeIDs[6]);
					yield return new EdgeMark(nodeIDs[3], nodeIDs[7]);

					yield return new EdgeMark(nodeIDs[4], nodeIDs[5]);
					yield return new EdgeMark(nodeIDs[5], nodeIDs[6]);
					yield return new EdgeMark(nodeIDs[6], nodeIDs[7]);
					yield return new EdgeMark(nodeIDs[7], nodeIDs[4]);
					break;
				default:
					throw new NotSupportedException();
			}
		}

		public static IEnumerable<object> GetSequenceOfFaces(ElementType elementType, int[] nodeIDs)
		{
			// TODO: implement missing pyramid and triangular prism elements
			// depends on face ordering!
			switch (elementType)
			{
				case ElementType.BeamLinear:
				case ElementType.BeamQuadratic:
					yield break; // beam has no face
				case ElementType.TriangleLinear:
				case ElementType.TriangleQuadratic:
					yield return new TriangleMark(nodeIDs[0], nodeIDs[1], nodeIDs[2]);
					break;
				case ElementType.QuadLinear:
				case ElementType.QuadQuadratic:
					yield return new QuadMark(nodeIDs[0], nodeIDs[1], nodeIDs[2], nodeIDs[3]);
					break;
				case ElementType.TetrahedronLinear:
				case ElementType.TetrahedronQuadratic:
					yield return new TriangleMark(nodeIDs[2], nodeIDs[1], nodeIDs[3]);
					yield return new TriangleMark(nodeIDs[0], nodeIDs[2], nodeIDs[3]);
					yield return new TriangleMark(nodeIDs[1], nodeIDs[0], nodeIDs[3]);
					yield return new TriangleMark(nodeIDs[0], nodeIDs[1], nodeIDs[2]);
					break;
				case ElementType.SquarePyramidLinear:
				case ElementType.SquarePyramidQuadratic:

					throw new NotImplementedException("Square pyramid face ordering was not specified.");

					//yield return new TriangleMark(nodeIDs[0], nodeIDs[1], nodeIDs[4]);
					//yield return new TriangleMark(nodeIDs[1], nodeIDs[2], nodeIDs[4]);
					//yield return new TriangleMark(nodeIDs[2], nodeIDs[3], nodeIDs[4]);
					//yield return new TriangleMark(nodeIDs[3], nodeIDs[0], nodeIDs[4]);
					//yield return new QuadMark(nodeIDs[0], nodeIDs[1], nodeIDs[2], nodeIDs[3]);
					//break;
				case ElementType.TriangularPrismLinear:
				case ElementType.TriangularPrismQuadratic:

					throw new NotImplementedException("Triangular prism face ordering was not specified.");

					//yield return new QuadMark(nodeIDs[0], nodeIDs[2], nodeIDs[5], nodeIDs[3]);
					//yield return new QuadMark(nodeIDs[1], nodeIDs[0], nodeIDs[3], nodeIDs[4]);
					//yield return new QuadMark(nodeIDs[2], nodeIDs[1], nodeIDs[4], nodeIDs[5]);
					//yield return new TriangleMark(nodeIDs[0], nodeIDs[1], nodeIDs[2]);
					//yield return new TriangleMark(nodeIDs[3], nodeIDs[4], nodeIDs[5]);
					//break;
				case ElementType.HexahedronLinear:
				case ElementType.HexahedronQuadratic:
					yield return new QuadMark(nodeIDs[0], nodeIDs[3], nodeIDs[7], nodeIDs[4]);
					yield return new QuadMark(nodeIDs[1], nodeIDs[0], nodeIDs[4], nodeIDs[5]);
					yield return new QuadMark(nodeIDs[2], nodeIDs[1], nodeIDs[5], nodeIDs[6]);
					yield return new QuadMark(nodeIDs[3], nodeIDs[2], nodeIDs[6], nodeIDs[7]);
					yield return new QuadMark(nodeIDs[0], nodeIDs[1], nodeIDs[2], nodeIDs[3]);
					yield return new QuadMark(nodeIDs[4], nodeIDs[5], nodeIDs[6], nodeIDs[7]);
					break;
				default:
					throw new NotSupportedException();
			}
		}

		#endregion

		#region IItemWithSignificantPoint Members

		public virtual Vector3 GetSignificantPoint()
		{
			return SignificantNode.Position;
		}

		#endregion


		#region ISelectable Members

		public Property Property
		{
			get { return this.property; }
			set { this.property = value; }
		}

		#endregion

		#region IComparable Members

		public int CompareTo(object obj)
		{
			return CompareTo(obj as Element);
		}

		public int CompareTo(Element other)
		{
			if (other == null)
				return 1;
			return this.id.CompareTo(other.id);
		}

		#endregion

		#region Equality & Hashing

		public bool Equals(Element other)
		{
			if (other == null)
				return false;
			return this.id == other.id;
		}

		public override bool Equals(object obj)
		{
			return this.Equals(obj as Element);
		}

		public override int GetHashCode()
		{
			return id.GetHashCode();
		}

		#endregion

	}
}
