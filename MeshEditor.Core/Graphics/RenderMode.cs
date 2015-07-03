using System;
using System.Collections.Generic;
using System.Text;

namespace MeshEditor.Graphics
{
	/// <summary>
	/// Zpusob vykresleni modelu
	/// </summary>
	[Serializable]
	[Flags]
	public enum RenderMode
	{
		None = 0,
		Points = 1,
		AllLines = 2, // Wireframe
		Faces = 4,
		BorderLines = 8,
		
		FacesLines = Faces | AllLines,
		LinesPoints = AllLines | Points,
		FacesBorder = Faces | BorderLines,
		FacesLinesPoints = FacesLines | Points
	}
}
