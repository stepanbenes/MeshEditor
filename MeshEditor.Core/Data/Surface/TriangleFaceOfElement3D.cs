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

		public TriangleFaceOfElement3D(Element3D parentElement, Node n1, Node n2, Node n3)
			: base(parentElement.ID, parentElement.ElementType, n1, n2, n3)
		{
			this.parentElement = parentElement;
		}
		
	}
}
