using System;
using System.Collections.Generic;
using System.Text;

namespace MeshEditor.Graphics
{
	/// <summary>
	/// mod pro barevne zobrazeni vlastnosti. priznaky lze libovolne kombinovat
	/// </summary>
	[Flags]
	public enum PropertyColorsMode
	{
		None = 0,
		Nodes = 1,
		Edges = 2,
		Faces = 4,
		Elements = 8,
        Beams = 16
	}
}
