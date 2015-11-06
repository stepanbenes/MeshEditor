using System;
using System.Collections.Generic;
using System.Text;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace MeshEditor.Data
{
	/// <summary>
	/// reprezentuje trojuhelnikovou plochu 3D prvku
	/// </summary>
	public class TriangleFaceOfElement3D : Triangle, IFaceOfElement3D
	{
		private Element3D parentElement;

		public Element3D ParentElement
		{
			get { return parentElement; }
		}

		public TriangleFaceOfElement3D(Element3D parentElement, int order, Node n1, Node n2, Node n3)
			: base(order, parentElement.ElementType, n1, n2, n3)
		{
			this.parentElement = parentElement;
		}

		public override bool Equals(object obj)
		{
			var other = obj as TriangleFaceOfElement3D;
			if (other == null)
				return false;
			return this.parentElement.ID == other.parentElement.ID && this.id == other.id;
		}

		public override int GetHashCode()
		{
			unchecked // Overflow is fine, just wrap
			{
				int hash = 17;
				hash = hash * 23 + parentElement.ID.GetHashCode();
				hash = hash * 23 + id.GetHashCode();
				return hash;
			}
		}
	}
}
