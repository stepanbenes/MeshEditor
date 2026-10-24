using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MeshEditor.LayerManager.Serialization
{
	/// <summary>
	/// Deserializes an abstract type as one of its derived types selected by the value of an enum property (<see cref="EnumValueTypeSelectorAttribute"/>),
	/// serializes it as its runtime type.
	/// </summary>
	internal class EnumValueTypeSelectorJsonConverter : JsonConverterFactory
	{
		public override bool CanConvert(Type typeToConvert)
		{
			return Attribute.GetCustomAttributes(typeToConvert).Any(v => v is EnumValueTypeSelectorAttribute);
		}

		public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
		{
			return (JsonConverter)Activator.CreateInstance(typeof(Converter<>).MakeGenericType(typeToConvert));
		}

		private class Converter<T> : JsonConverter<T> where T : class
		{
			private readonly EnumValueTypeSelectorAttribute[] attributes = Attribute.GetCustomAttributes(typeof(T)).OfType<EnumValueTypeSelectorAttribute>().ToArray();

			public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
			{
				using (JsonDocument document = JsonDocument.ParseValue(ref reader))
				{
					JsonElement element = document.RootElement;
					if (element.ValueKind != JsonValueKind.Object)
						throw new JsonException($"Unexpected JSON value {element.ValueKind} when parsing {typeof(T).FullName}.");

					foreach (EnumValueTypeSelectorAttribute attribute in attributes)
					{
						string enumValueText = getPropertyValueText(element, attribute.EnumPropertyName);
						Type enumType = attribute.EnumValue.GetType();

						if (enumValueText != null && Enum.IsDefined(enumType, enumValueText))
						{
							object parsedEnumValue = Enum.Parse(enumType, enumValueText, ignoreCase: true);

							if (Equals(attribute.EnumValue, parsedEnumValue))
							{
								return (T)element.Deserialize(attribute.TargetType, options);
							}
						}
						else
						{
							throw new JsonException($"'{enumValueText}' is not valid option of enum type {enumType.FullName}");
						}
					}

					throw new JsonException($"No type found for {typeof(T).FullName}.");
				}
			}

			public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
			{
				JsonSerializer.Serialize(writer, value, value.GetType(), options);
			}

			/// <summary>
			/// Text of the property value, the property name is matched case-insensitively when there is no exact match (as Newtonsoft's JObject.GetValue).
			/// </summary>
			private static string getPropertyValueText(JsonElement element, string propertyName)
			{
				if (!element.TryGetProperty(propertyName, out JsonElement value))
				{
					value = element.EnumerateObject().FirstOrDefault(p => string.Equals(p.Name, propertyName, StringComparison.OrdinalIgnoreCase)).Value;
				}
				switch (value.ValueKind)
				{
					case JsonValueKind.String:
						return value.GetString();
					case JsonValueKind.Undefined:
					case JsonValueKind.Null:
						return null;
					default:
						return value.GetRawText();
				}
			}
		}
	}
}
