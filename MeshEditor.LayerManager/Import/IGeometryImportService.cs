using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.LayerManager.Data;

namespace MeshEditor.LayerManager.Import
{
	public interface IGeometryImportService
	{
		GeometryDescription ReadGeometry();
	}
}
