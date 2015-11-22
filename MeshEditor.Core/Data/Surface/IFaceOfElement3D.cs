using System;
using System.Collections.Generic;
using System.Text;

namespace MeshEditor.Data
{
	/// <summary>
	/// rozhrani, ktere musi implementovat kazdy objekt plochy 3D prvku
	/// </summary>
	public interface IFaceOfElement3D
	{
		Property Property { get; }

		Element3D ParentElement { get; }

		void ChangeParentElement(Element3D newParentElement);
	}
}
