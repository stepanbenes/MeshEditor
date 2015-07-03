using System;
using System.Collections.Generic;
using System.Text;
using MeshEditor.Data;

namespace MeshEditor.IO
{
	/// <summary>
	/// trida obsahujici identifikacni popis nejake hrany
	/// </summary>
	public struct FaceDraft
	{
		public int[] NodeIDs;
		public Property Property;

		public FaceDraft(Property property, params int[] nodeIDs)
		{
			this.Property = property;
			this.NodeIDs = nodeIDs;
		}
	}
}
