using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MeshEditor.Common.Serialization
{
	/// <summary>
	/// Guid read in any format accepted by Guid.Parse (e.g. with braces) as by Newtonsoft.Json, written in the "D" format.
	/// </summary>
	public class GuidJsonConverter : JsonConverter<Guid>
	{
		public override Guid Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType != JsonTokenType.String)
				throw new JsonException($"Unexpected token {reader.TokenType} when parsing guid.");
			if (reader.TryGetGuid(out Guid value) || Guid.TryParse(reader.GetString(), out value))
				return value;
			throw new JsonException($"Could not convert string to Guid: {reader.GetString()}.");
		}

		public override void Write(Utf8JsonWriter writer, Guid value, JsonSerializerOptions options) => writer.WriteStringValue(value);

		public override Guid ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => Guid.Parse(reader.GetString());

		public override void WriteAsPropertyName(Utf8JsonWriter writer, Guid value, JsonSerializerOptions options) => writer.WritePropertyName(value.ToString());
	}
}
