using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.LayerManager.Compression
{
	public class TransparentCompressionParameters : CompressionParameters
	{
		public override CompressionMethod Method => CompressionMethod.Transparent;
	}
}
