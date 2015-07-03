using System;
using System.Collections.Generic;
using System.Text;

namespace MeshEditor.Data
{
	/// <summary>
	/// typ entity. vyuzito v poli vlastnosti pridruzenych ke kazdemu uzlu. nemenit cisla, nemenit nazvy, vyuziva se pro zapis a cteni ze souboru
	/// </summary>
	public enum EntityType : byte
	{
		Vertex = 1,
		Edge = 2, // Curve
		Surface = 3,
		Region = 4,
		Patch = 5,
		Shell = 6
	}

	public enum PreprocessorSections
	{
		Unknown,
		files,
		probdesc,
		loadcase,
		nodvertpr,
		nodedgpr,
		nodsurfpr,
		nodvolpr,
		eledgpr,
		elsurfpr,
		elvolpr,
		outdrv,
		gfunct
	}
}
