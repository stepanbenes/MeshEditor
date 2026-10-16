using System;
using System.Collections.Generic;
using MeshEditor.LayerManager.Filters;
using System.Text.Json.Serialization;

namespace MeshEditor.LayerManager.Data
{
	public class SummaryFile
	{
		public Guid Id { get; set; }

		public string Name { get; set; }

		public Guid? ParentId { get; set; }

		public Filter Filter { get; set; }

		public MeshFileDescriptor[] Meshes { get; set; }

		public Dictionary<string, FieldDescriptor> Fields { get; set; } = new Dictionary<string, FieldDescriptor>();

		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public Guid? MeshFallbackLayerId { get; set; }

		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public Guid? AttributeFallbackLayerId { get; set; }

		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public Guid? DataFallbackLayerId { get; set; }
	}
}
