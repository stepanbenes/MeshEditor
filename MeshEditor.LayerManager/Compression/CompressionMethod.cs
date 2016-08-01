using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.LayerManager.Compression
{
	public enum CompressionMethod
	{
		Transparent = 0,
		SVD = 1,
		WT = 2,
		Default = Transparent
	}
}
