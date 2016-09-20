using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MeshEditor.LayerManager.Data
{
	public interface IDataDescription
	{
		string FieldName { get; }
		string ComponentName { get; }
		int Index { get; }
		double[] TimeSteps { get; }
		DataLocationType Location { get; }
	}

	public class DataFileDescriptor : IDataDescription
	{
		public DataFileDescriptor()
		{ }

		internal static DataFileDescriptor CreateFrom(IDataDescription source)
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
