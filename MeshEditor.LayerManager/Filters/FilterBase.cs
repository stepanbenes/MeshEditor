using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MeshEditor.LayerManager.Filters
{
	[KnownType(typeof(AttributeSelectionFilter))]
	public abstract class FilterBase
	{
		[JsonConverter(typeof(StringEnumConverter))]
		public abstract FilterType Type { get; }
	}
}
