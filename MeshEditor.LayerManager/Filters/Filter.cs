using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.LayerManager.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MeshEditor.LayerManager.Filters
{
	[EnumValueTypeSelector(FilterType.Surface, typeof(SurfaceFilter), enumPropertyName: nameof(Type))]
	[EnumValueTypeSelector(FilterType.AttributeSelection, typeof(AttributeSelectionFilter), enumPropertyName: nameof(Type))]
	[EnumValueTypeSelector(FilterType.Slice, typeof(SliceFilter), enumPropertyName: nameof(Type))]
	[EnumValueTypeSelector(FilterType.TimeCompression, typeof(TimeCompressionFilter), enumPropertyName: nameof(Type))]
	[EnumValueTypeSelector(FilterType.Deformation, typeof(DeformationFilter), enumPropertyName: nameof(Type))]
	[EnumValueTypeSelector(FilterType.IsoSurface, typeof(IsoSurfaceFilter), enumPropertyName: nameof(Type))]
	public abstract class Filter
	{
		[JsonConverter(typeof(StringEnumConverter))]
		public abstract FilterType Type { get; }
	}
}
