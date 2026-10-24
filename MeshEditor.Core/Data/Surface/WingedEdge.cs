using System;
using System.Collections.Generic;
using System.Text;
using MeshEditor.Utilities;
using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL;
using System.Drawing;
using MeshEditor.Graphics;
using System.Diagnostics;

namespace MeshEditor.Data
{
	/// <summary>
	/// reprezentuje hranu prvku (zaklad struktury okridlena hrana)
	/// </summary>
	public class WingedEdge : IItemWithSignificantPoint, ISelectable
	{
		#region Fields, Constructor

		protected Node beginNode, endNode;
		protected Element2D face1, face2;
		protected List<WingedEdge> beginNeighbors;
		protected List<WingedEdge> endNeighbors;
		private Property property;

		protected float featureAngle;

		public WingedEdge(Node n1, Node n2, Element2D face1)
		{
			if (n1.ID < n2.ID) // seradim je, prvni je ten s mensim id
			{
				this.beginNode = n1;
				this.endNode = n2;
			}
			else
			{
				this.beginNode = n2;
				this.endNode = n1;
			}
			this.face1 = face1;
			this.face2 = null;
			this.beginNeighbors = null;
			this.endNeighbors = null;
			this.featureAngle = float.MaxValue; // is border
			this.property = Property.Zero;
		}

		#endregion

		#region Properties

		public Node BeginNode
		{
			get { return beginNode; }
		}

		public Node EndNode
		{
			get { return endNode; }
		}

		public Element2D Face1
		{
			get { return face1; }
			//set
			//{
			//    face1 = value;
			//    updateBorderFlag();
			//}
		}

		public Element2D Face2
		{
			get { return face2; }
			set
			{
				face2 = value;
				ComputeFeatureAngle();
			}
		}

		public float FeatureAngle
		{
			get { return featureAngle; }
		}

		public List<WingedEdge> BeginNeighbors
		{
			get { return beginNeighbors; }
			set { beginNeighbors = value; }
		}

		public List<WingedEdge> EndNeighbors
		{
			get { return endNeighbors; }
			set { endNeighbors = value; }
		}

		public virtual IEnumerable<Node> IterateThroughAllNodes()
		{
			yield return beginNode;
			yield return endNode;
		}

		#endregion

		#region Public methods

		//public /*virtual*/ float GetCost()
		//{
		//    return (endNode.Position - beginNode.Position).LengthSquared;
		//}

		public void ReplaceNode(Node oldOne, Node newOne, List<WingedEdge> newNeighbors)
		{
			if (this.beginNode == oldOne)
			{
				this.beginNode = newOne;
				this.beginNeighbors = newNeighbors;
			}
			if (this.endNode == oldOne)
			{
				this.endNode = newOne;
				this.endNeighbors = newNeighbors;
			}
		}

		public Node GetOppositeNodeTo(Node node)
		{
			if (node == beginNode)
				return endNode;
			if (node == endNode)
				return beginNode;
			throw new ArgumentException("Edge does not contain this node.", nameof(node));
		}

		//public void Draw()
		//{
		//    if (IsHardBorder)
		//        GL.Color3(Scene.HardBorderColor);
		//    else if (featureAngle >= Mesh.SoftBorderLimit)
		//        GL.Color3(Scene.SoftBorderColor);
		//    else
		//        GL.Color3(Scene.WireframeColor);

		//    GL.Vertex3(beginNode.Position);
		//    GL.Vertex3(endNode.Position);
		//}

		public void ComputeFeatureAngle()
		{
			if (face1 == null || face2 == null) // nema jednu/obe plochy => je to hranicni hrana
			{
				featureAngle = float.MaxValue;
			}
			else
			{
				featureAngle = MeshEditor.Utilities.Functions.GetAngleInDegreesBetweenUnitVectors(face1.NormalVector, face2.NormalVector);
				Debug.Assert(!float.IsNaN(featureAngle)); // featureAngle is NaN if either face is degenerated (some or all nodes have the same position)
			}
		}

		public override string ToString()
		{
			return "Edge | (Nodes: " + beginNode.ID + ", " + endNode.ID + ") | Property: " + property;
		}
		
		#endregion
		
		#region Comparing members

		public bool Equals(WingedEdge other)
		{
			return this.beginNode == other.beginNode && this.endNode == other.endNode;
		}

		public override bool Equals(object obj)
		{
			WingedEdge other = obj as WingedEdge;
			if (other != null)
			{
				return this.Equals(other);
			}
			return false;
		}

		public override int GetHashCode()
		{
			unchecked // Overflow is fine, just wrap
			{
				int hash = 17;
				hash = hash * 23 + beginNode.GetHashCode();
				hash = hash * 23 + endNode.GetHashCode();
				return hash;
			}
		}

		#endregion

		#region IItemWithSignificantPoint Members

		public Vector3 GetSignificantPoint()
		{
			return beginNode.Position;
		}

		#endregion

		#region ISelectable Members

		public Property Property
		{
			get { return property; }
			set { property = value; }
		}

		#endregion
	}
}
