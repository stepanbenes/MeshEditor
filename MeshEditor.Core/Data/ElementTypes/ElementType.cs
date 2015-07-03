using System;
using System.Collections.Generic;
using System.Text;

namespace MeshEditor.Data
{
	/// <summary>
	/// typ elementu. cislovani odpovida specifikaci vstupniho formatu souboru se siti
	/// </summary>
	public enum ElementType : int
	{
		// cisla jsou dulezita, odpovidaji cislu typu prvku ve vstupnim souboru

		// 1D
		BeamLinear = 1,
		BeamQuadratic = 2,
		// 2D
		TriangleLinear = 3,
		TriangleQuadratic = 4,
		QuadLinear = 5,
		QuadQuadratic = 6,
		// 3D
		TetrahedronLinear = 7,
		TetrahedronQuadratic = 8,
		SquarePyramidLinear = 9,
		SquarePyramidQuadratic = 10,
		TriangularPrismLinear = 11,
		TriangularPrismQuadratic = 12,
		HexahedronLinear = 13,
		HexahedronQuadratic = 14
	}
}
