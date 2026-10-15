using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MeshEditor.Common.Serialization
{
	/// <summary>
	/// Arrays and lists of floating-point numbers. The numbers are written as raw values (see <see cref="NewtonsoftNumberFormat"/>)
	/// which Utf8JsonWriter does not indent, so the indentation of the array items is written here.
	/// </summary>
	public class FloatingPointCollectionJsonConverter : JsonConverterFactory
	{
		private static readonly Type[] elementTypes = { typeof(double), typeof(float), typeof(decimal), typeof(double?), typeof(float?), typeof(decimal?) };

		public override bool CanConvert(Type typeToConvert) => getElementType(typeToConvert) != null;

		public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
		{
			Type elementType = getElementType(typeToConvert);
			Type converterType = typeToConvert.IsArray ? typeof(ArrayConverter<>) : typeof(ListConverter<>);
			return (JsonConverter)Activator.CreateInstance(converterType.MakeGenericType(elementType));
		}

		private static Type getElementType(Type type)
		{
			Type elementType = type.IsArray && type.GetArrayRank() == 1 ? type.GetElementType() :
				type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>) ? type.GetGenericArguments()[0] :
				null;
			return elementTypes.Contains(elementType) ? elementType : null;
		}

		private static List<T> readList<T>(ref Utf8JsonReader reader, JsonSerializerOptions options)
		{
			if (reader.TokenType != JsonTokenType.StartArray)
				throw new JsonException($"Unexpected token {reader.TokenType} when parsing array.");
			var elementTypeInfo = options.GetTypeInfo(typeof(T));
			var list = new List<T>();
			while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
			{
				list.Add(reader.TokenType == JsonTokenType.Null ? default(T) : (T)JsonSerializer.Deserialize(ref reader, elementTypeInfo));
			}
			return list;
		}

		private static void writeList<T>(Utf8JsonWriter writer, IEnumerable<T> values)
		{
			writer.WriteStartArray();
			string indentation = writer.Options.Indented ? writer.Options.NewLine + new string(writer.Options.IndentCharacter, writer.Options.IndentSize * writer.CurrentDepth) : string.Empty;
			foreach (T value in values)
			{
				switch (value)
				{
					case null:
						writer.WriteNullValue();
						break;
					case double d:
						NewtonsoftNumberFormat.Write(writer, d, indentation);
						break;
					case float f:
						NewtonsoftNumberFormat.Write(writer, f, indentation);
						break;
					case decimal m:
						NewtonsoftNumberFormat.Write(writer, m, indentation);
						break;
				}
			}
			writer.WriteEndArray();
		}

		private class ArrayConverter<T> : JsonConverter<T[]>
		{
			public override T[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => readList<T>(ref reader, options).ToArray();

			public override void Write(Utf8JsonWriter writer, T[] value, JsonSerializerOptions options) => writeList(writer, value);
		}

		private class ListConverter<T> : JsonConverter<List<T>>
		{
			public override List<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => readList<T>(ref reader, options);

			public override void Write(Utf8JsonWriter writer, List<T> value, JsonSerializerOptions options) => writeList(writer, value);
		}
	}
}
