using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MeshEditor.LayerManager.Data
{
	internal interface IDataLayerDescription
	{
		string FieldName { get; set; }
		string ComponentName { get; set; }
		int Index { get; set; }
		double[] TimeSteps { get; set; }
		DataLocationType Location { get; set; }
	}

	public class DataFileDescriptor : IDataLayerDescription
	{
		public DataFileDescriptor()
		{ }

		internal static DataFileDescriptor CreateFrom(IDataLayerDescription source)
		{
			return new DataFileDescriptor
			{
				FieldName = source.FieldName,
				ComponentName = source.ComponentName,
				Index = source.Index,
				TimeSteps = source.TimeSteps?.ToArray(),
				Location = source.Location
			};
		}

		public string FieldName { get; set; }
		public string ComponentName { get; set; }
		public int Index { get; set; }

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public double[] TimeSteps { get; set; }

		[JsonConverter(typeof(StringEnumConverter))]
		public DataLocationType Location { get; set; }
	}
}
