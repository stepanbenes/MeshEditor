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
		decimal[] TimeSteps { get; }
		DataLocationType Location { get; }
	}

	public class DataFileDescriptor : IDataDescription, IEquatable<IDataDescription>
	{
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
		public decimal[] TimeSteps { get; set; }

		[JsonConverter(typeof(StringEnumConverter))]
		public DataLocationType Location { get; set; }

		public override int GetHashCode() => Index.GetHashCode();

		public override bool Equals(object obj) => Equals(obj as IDataDescription);

		public bool Equals(IDataDescription other)
		{
			if (other == null)
				return false;
			return this.Index == other.Index && this.FieldName == other.FieldName && this.ComponentName == other.ComponentName;
		}
	}
}
