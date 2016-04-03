using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MeshEditor.LayerManager.Serialization
{
	internal class EnumValueTypeSelectorJsonConverter : JsonConverter
	{
		public override bool CanConvert(Type objectType)
		{
			return Attribute.GetCustomAttributes(objectType).Any(v => v is EnumValueTypeSelectorAttribute);
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			if (reader.TokenType == JsonToken.Null)
			{
				return null;
			}

			// Load JObject from stream
			JObject jObject = JObject.Load(reader);

			// Use reflection to find all EnumValueTypeSelectorAttributes, if any
			foreach (EnumValueTypeSelectorAttribute attribute in Attribute.GetCustomAttributes(objectType).OfType<EnumValueTypeSelectorAttribute>())
			{
				string enumValueText = (string)jObject[attribute.EnumPropertyName];
				Type enumType = attribute.EnumValue.GetType();

				if (Enum.IsDefined(enumType, enumValueText))
				{
					object parsedEnumValue = Enum.Parse(enumType, enumValueText, ignoreCase: true);

					if (Equals(attribute.EnumValue, parsedEnumValue))
					{
						var target = Activator.CreateInstance(attribute.TargetType);
						serializer.Populate(jObject.CreateReader(), target);
						return target;
					}
				}
				else
				{
					throw new FormatException($"'{enumValueText}' is not valid option of enum type {enumType.FullName}");
				}
			}

			//throw new InvalidOperationException(); // no type found

			// Otherwise, populate the base type object properties
			{
				var target = Activator.CreateInstance(objectType);
				serializer.Populate(jObject.CreateReader(), target);
				return target;
			}
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			throw new NotImplementedException();
		}
	}
}
