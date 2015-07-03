using System;
using System.Collections.Generic;
using System.Text;

namespace MeshEditor.CoreInterface
{
	/// <summary>
	/// vsechny mody uzivatelskeho rozhrani editoru siti
	/// </summary>
	public enum EditorMode
	{
		None,
		SelectFaces,
		SelectEdges,
		SelectNodes,
		SelectElements,
        SelectBeams,
		Pan,
		Orbit,
		LookAround,
		ZoomWindow,
		RotateZ,
		PickCuttingPlanePoint
	}
}
