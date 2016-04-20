using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.LayerManager.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MeshEditor.SolutionManager.Configuration
{
	enum SolutionProviderType
	{
		Local,
		RestApi
	}

	[EnumValueTypeSelector(SolutionProviderType.Local, typeof(LocalSolutionProviderInfo), nameof(Type))]
	[EnumValueTypeSelector(SolutionProviderType.RestApi, typeof(RestApiSolutionProviderInfo), nameof(Type))]
	abstract class SolutionProviderInfo
	{
		[JsonConverter(typeof(StringEnumConverter))]
		public abstract SolutionProviderType Type { get; }
	}

	
	class LocalSolutionProviderInfo : SolutionProviderInfo
	{
		public override SolutionProviderType Type => SolutionProviderType.Local;
		public string Directory { get; set; }
	}

	class RestApiSolutionProviderInfo : SolutionProviderInfo
	{
		public override SolutionProviderType Type => SolutionProviderType.RestApi;
		public string BaseUri { get; set; }
		// credentials...
	}
}
