using System;
using System.Collections.Generic;
using MeshEditor.LayerManager.Filters;
using Newtonsoft.Json;

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

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public Guid? MeshFallbackLayerId { get; set; }

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public Guid? AttributeFallbackLayerId { get; set; }

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public Guid? DataFallbackLayerId { get; set; }
	}
}
