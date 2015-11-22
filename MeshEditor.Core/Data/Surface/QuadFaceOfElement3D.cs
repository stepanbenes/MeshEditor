using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace MeshEditor.Data
{
	/// <summary>
	/// trida reprezentujici ctyruhelnikovou plochu nejakeho 3D konecneho prvku
	/// </summary>
	public class QuadFaceOfElement3D : Quadrilateral, IFaceOfElement3D
	{
		private Element3D parentElement;

		public Element3D ParentElement
		{
			get { return parentElement; }
		}

		public QuadFaceOfElement3D(Element3D parentElement, int order, Node n1, Node n2, Node n3, Node n4)
			: base(order, parentElement.ElementType, n1, n2, n3, n4)
		{
			this.parentElement = parentElement;
		}

		public override bool Equals(object obj)
		{
			return ReferenceEquals(this, obj); // two faces can have same id (face order) and parentElement.ID (if ChangeParentElement is called)
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

		public void ChangeParentElement(Element3D newParentElement)
		{
			Debug.Assert(newParentElement != null);
			Debug.Assert(!parentElement.Equals(newParentElement));

			parentElement = newParentElement;
		}
	}
}
