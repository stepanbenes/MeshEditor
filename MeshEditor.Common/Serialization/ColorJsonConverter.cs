using System;
using System.ComponentModel;
using System.Drawing;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MeshEditor.Common.Serialization
{
	/// <summary>
	/// Color as a string of its type converter ("Red", "10, 20, 30", "128, 10, 20, 30"), as written by Newtonsoft.Json.
	/// </summary>
	public class ColorJsonConverter : JsonConverter<Color>
	{
		private static readonly TypeConverter converter = TypeDescriptor.GetConverter(typeof(Color));

		public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType != JsonTokenType.String)
				throw new JsonException($"Unexpected token {reader.TokenType} when parsing color.");
			return (Color)converter.ConvertFromInvariantString(reader.GetString());
		}

		public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
		{
			writer.WriteStringValue(converter.ConvertToInvariantString(value));
		}
	}
}
