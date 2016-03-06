using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.LayerManager.Data
{
	public class LayerResult : IResultDescription
	{
		public Guid LayerId { get; set; }
		public string FieldName { get; set; }
		public string ComponentName { get; set; }
		public int Index { get; set; }
		public double? TimeStep { get; set; }

		public Dictionary<string, object> Compression { get; set; }

		/// <summary>
		/// double array data in Base64 string format
		/// </summary>
		public string Data { get; set; }
	}
}
