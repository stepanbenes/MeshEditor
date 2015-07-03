using System;
using System.Collections.Generic;
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

		public QuadFaceOfElement3D(Element3D parentElement, Node n1, Node n2, Node n3, Node n4)
			: base(parentElement.ID, parentElement.ElementType, n1, n2, n3, n4)
		{
			this.parentElement = parentElement;
		}
		
	}
}
