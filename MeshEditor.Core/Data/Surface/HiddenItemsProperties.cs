using System;
using System.Collections.Generic;
using System.Text;
using MeshEditor.Construction;
using MeshEditor.Utilities;

namespace MeshEditor.Data
{
	/// <summary>
	/// trida shromazdujici a uchovavajici vlastnosti momentalne skrytych entit.
	/// vzniknou prirazenim vlastnosti plocham nebo hranam na povrchu rezu a naslednym obnovenim site do puvodniho stavu.
	/// </summary>
	public class HiddenItemsProperties
	{
		private Dictionary<TriangleMark, Property> triangleProperties;
		private Dictionary<QuadMark, Property> quadProperties;
		private Dictionary<EdgeMark, Property> edgeProperties;
		
		public HiddenItemsProperties()
		{
			Clear();
		}

		public void Clear()
		{
			triangleProperties = new Dictionary<TriangleMark, Property>();
			quadProperties = new Dictionary<QuadMark, Property>();
			edgeProperties = new Dictionary<EdgeMark, Property>();
		}

		public Dictionary<TriangleMark, Property> TriangleProperties
		{
			get { return triangleProperties; }
		}

		public Dictionary<QuadMark, Property> QuadProperties
		{
			get { return quadProperties; }
		}

		public Dictionary<EdgeMark, Property> EdgeProperties
		{
			get { return edgeProperties; }
		}

		// ------------------------------------------------------------------

		public void AddFaceProperty(Element2D face)
		{
			Triangle t = face as Triangle;
			if (t != null)
			{
				TriangleMark mark = new TriangleMark(t.Node1.ID, t.Node2.ID, t.Node3.ID);
				Add(ref mark, face.Property);
			}
			Quadrilateral q = face as Quadrilateral;
			if (q != null)
			{
				QuadMark mark = new QuadMark(q.Node1.ID, q.Node2.ID, q.Node3.ID, q.Node4.ID);
				Add(ref mark, face.Property);
			}
		}

		public void AddEdgeProperty(WingedEdge edge)
		{
			//long mark = ((long)edge.BeginNode.ID << 32) + edge.EndNode.ID; /**/ // vypocitam znacku - je to slozenina indexu obou uzlu hrany
			EdgeMark mark = new EdgeMark(edge.BeginNode.ID, edge.EndNode.ID);
			Add(ref mark, edge.Property);
		}

		public void Add(ref TriangleMark mark, Property property)
		{
			triangleProperties[mark] = property;
		}

		public void Add(ref QuadMark mark, Property property)
		{
			quadProperties[mark] = property;
		}

		public void Add(ref EdgeMark mark, Property property)
		{
			edgeProperties[mark] = property;
		}

		public bool TryGetPropertyAndRemove(ref TriangleMark mark, out Property property)
		{
			if (triangleProperties.TryGetValue(mark, out property))
			{
				triangleProperties.Remove(mark);
				return true;
			}
			return false;
		}

		public bool TryGetPropertyAndRemove(ref QuadMark mark, out Property property)
		{
			if (quadProperties.TryGetValue(mark, out property))
			{
				quadProperties.Remove(mark);
				return true;
			}
			return false;
		}

		public bool TryGetPropertyAndRemove(ref EdgeMark mark, out Property property)
		{
			if (edgeProperties.TryGetValue(mark, out property))
			{
				edgeProperties.Remove(mark);
				return true;
			}
			return false;
		}

		public override string ToString()
		{
			return "triangles: " + triangleProperties.Count + "; quads: " + quadProperties.Count + "; edges: " + edgeProperties.Count;
		}
	}
}
