using System;
using System.Collections.Generic;
using System.Text;
using MeshEditor.Data;

namespace MeshEditor.IO
{
	/// <summary>
	/// trida obsahujici informace o nejake hrane.
	/// </summary>
	public struct EdgeDraft
	{
		public int Node1ID;
		public int Node2ID;
		public Property Property;

		public EdgeDraft(Property property, int node1ID, int node2ID)
		{
			this.Property = property;
			this.Node1ID = node1ID;
			this.Node2ID = node2ID;
		}
	}
}
