using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using OpenTK.Mathematics;
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

		public void SwapParentElementForOppositeOne(Element3D newParentElement)
		{
			Debug.Assert(newParentElement != null);
			Debug.Assert(!parentElement.Equals(newParentElement));

			parentElement = newParentElement;
			ReverseNodeOrder();
			// NOTE: face property remains the same
		}
	}
}
