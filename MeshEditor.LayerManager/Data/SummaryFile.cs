using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.LayerManager.Filters;

namespace MeshEditor.LayerManager.Data
{
	public class SummaryFile
	{
		public Guid Id { get; set; }
		public string Name { get; set; }

		public Guid? ParentId { get; set; }
		public Filter Filter { get; set; }

		public MeshFileDescriptor[] Meshes { get; set; }

		public Dictionary<string, FieldDescriptor> Fields { get; set; }
	}
}
