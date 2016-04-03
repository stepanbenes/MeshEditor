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
	[EnumValueTypeSelector(FilterType.AttributeSelection, typeof(AttributeSelectionFilter), enumPropertyName: nameof(Type))]
	public abstract class FilterBase
	{
		[JsonConverter(typeof(StringEnumConverter))]
		public abstract FilterType Type { get; }
	}
}
