using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.LayerManager.Storage
{
	public interface IStorageService
	{
		Stream Save(Uri uri);
		Stream Load(Uri uri);
	}
}
