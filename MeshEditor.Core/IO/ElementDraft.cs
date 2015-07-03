using System;
using System.Collections.Generic;
using System.Text;
using MeshEditor.Data;

namespace MeshEditor.IO
{
	/// <summary>
	/// trida obsahujici informace o nejakem prvku
	/// </summary>
	public struct ElementDraft
	{
		public int ID;
		public ElementType Type;
		public int[] NodeIDs;
		public Property Property;
		public int[] EdgeProperties;
		public int[] FaceProperties;

		public override string ToString()
		{
			StringBuilder text = new StringBuilder();
			text.AppendLine("ID: " + ID);
			text.AppendLine("Element type: " + Type);
			text.Append("Node IDs: ");
			for (int i = 0; i < NodeIDs.Length; i++)
			{
				text.Append(NodeIDs[i] + " ");
			}
			text.AppendLine();
			text.AppendLine("Property number: " + Property);
			text.Append("Edge properties: ");
			if (EdgeProperties != null)
			{
				for (int i = 0; i < EdgeProperties.Length; i++)
				{
					text.Append(EdgeProperties[i] + " ");
				}
			}
			else
				text.Append("none");
			text.AppendLine();
			text.Append("Face properties: ");
			if (FaceProperties != null)
			{
				for (int i = 0; i < FaceProperties.Length; i++)
				{
					text.Append(FaceProperties[i] + " ");
				}
			}
			else
				text.Append("none");
			text.AppendLine();
			return text.ToString();
		}
	}
}
