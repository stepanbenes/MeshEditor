using System;
using System.Linq;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace MeshEditor.Common.Serialization
{
	/// <summary>
	/// System.Text.Json options that read and write JSON the same way as Newtonsoft.Json with its default settings did
	/// (files written by previous versions must stay readable and new files readable by previous versions).
	/// </summary>
	public static class JsonOptions
	{
		/// <summary>
		/// Options for single-line JSON.
		/// </summary>
		public static JsonSerializerOptions Compact { get; } = Create(writeIndented: false);

		/// <summary>
		/// Options for indented JSON.
		/// </summary>
		public static JsonSerializerOptions Indented { get; } = Create(writeIndented: true);

		public static JsonSerializerOptions Create(bool writeIndented, params JsonConverter[] converters)
		{
			var options = new JsonSerializerOptions
			{
				WriteIndented = writeIndented,
				// Newtonsoft.Json matches property names case-insensitively, skips comments, accepts trailing commas and numbers in strings
				PropertyNameCaseInsensitive = true,
				ReadCommentHandling = JsonCommentHandling.Skip,
				AllowTrailingCommas = true,
				NumberHandling = JsonNumberHandling.AllowReadingFromString,
				// Newtonsoft.Json serializes public fields too
				IncludeFields = true,
				// Newtonsoft.Json escapes only control characters, quotes and backslashes, not HTML characters or non-ASCII letters
				Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
				// Newtonsoft.Json has no limit when writing (deep layer trees)
				MaxDepth = 256,
				TypeInfoResolver = new DefaultJsonTypeInfoResolver { Modifiers = { orderMembersAsNewtonsoft, inheritConverterAttributes } },
			};
			foreach (var converter in converters)
			{
				options.Converters.Add(converter);
			}
			options.Converters.Add(new FloatingPointCollectionJsonConverter());
			options.Converters.Add(new NewtonsoftDoubleJsonConverter());
			options.Converters.Add(new NewtonsoftSingleJsonConverter());
			options.Converters.Add(new NewtonsoftDecimalJsonConverter());
			options.Converters.Add(new GuidJsonConverter());
			options.Converters.Add(new ColorJsonConverter());
			options.Converters.Add(new EnumJsonConverterFactory(writeAsString: false));
			options.MakeReadOnly();
			return options;
		}

		/// <summary>
		/// Newtonsoft.Json writes fields first, then properties, both in the reflection order (members of derived classes first).
		/// </summary>
		private static void orderMembersAsNewtonsoft(JsonTypeInfo typeInfo)
		{
			if (typeInfo.Kind != JsonTypeInfoKind.Object || typeInfo.Properties.Count < 2)
				return;
			const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
			var memberNames = typeInfo.Type.GetFields(flags).Select(f => f.Name)
				.Concat(typeInfo.Type.GetProperties(flags).Select(p => p.Name))
				.Distinct()
				.ToList();
			int indexOf(JsonPropertyInfo property)
			{
				int index = property.AttributeProvider is MemberInfo member ? memberNames.IndexOf(member.Name) : -1;
				return index < 0 ? int.MaxValue : index;
			}
			var ordered = typeInfo.Properties.OrderBy(indexOf).ToList(); // stable sort
			typeInfo.Properties.Clear();
			foreach (var property in ordered)
			{
				typeInfo.Properties.Add(property);
			}
		}

		/// <summary>
		/// Newtonsoft.Json applies [JsonConverter] of an overridden (abstract) property also to the overriding property, System.Text.Json does not.
		/// </summary>
		private static void inheritConverterAttributes(JsonTypeInfo typeInfo)
		{
			if (typeInfo.Kind != JsonTypeInfoKind.Object)
				return;
			foreach (var property in typeInfo.Properties)
			{
				if (property.CustomConverter != null || !(property.AttributeProvider is PropertyInfo propertyInfo) || propertyInfo.IsDefined(typeof(JsonConverterAttribute), inherit: false))
					continue;
				var attribute = (JsonConverterAttribute)Attribute.GetCustomAttribute(propertyInfo, typeof(JsonConverterAttribute), inherit: true); // searches overridden properties
				if (attribute == null)
					continue;
				var converter = attribute.CreateConverter(property.PropertyType) ?? (JsonConverter)Activator.CreateInstance(attribute.ConverterType);
				if (converter is JsonConverterFactory factory)
				{
					converter = factory.CreateConverter(property.PropertyType, typeInfo.Options);
				}
				property.CustomConverter = converter;
			}
		}
	}
}
