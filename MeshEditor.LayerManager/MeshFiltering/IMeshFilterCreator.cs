using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.LayerManager.Data;

namespace MeshEditor.LayerManager.MeshFiltering
{
	interface IMeshFilterCreator
	{
		GeometryDescription Create(GeometryDescription source);
	}
}
