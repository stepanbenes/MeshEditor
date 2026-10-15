using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MeshEditor.Common.Serialization
{
	/// <summary>
	/// Enums as handled by Newtonsoft.Json: read from a number or a name (case-insensitive), written as a number
	/// (or as a name when <c>writeAsString</c> is set, which corresponds to Newtonsoft's StringEnumConverter).
	/// </summary>
	public class EnumJsonConverterFactory : JsonConverterFactory
	{
		private readonly bool writeAsString;

		public EnumJsonConverterFactory(bool writeAsString)
		{
			this.writeAsString = writeAsString;
		}

		public override bool CanConvert(Type typeToConvert) => typeToConvert.IsEnum;

		public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
		{
			return (JsonConverter)Activator.CreateInstance(typeof(EnumJsonConverter<>).MakeGenericType(typeToConvert), new object[] { writeAsString });
		}
	}

	/// <summary>
	/// Enums read from a number or a name (case-insensitive) and written as a name, as by Newtonsoft's StringEnumConverter.
	/// </summary>
	public class StringEnumJsonConverter : EnumJsonConverterFactory
	{
		public StringEnumJsonConverter() : base(writeAsString: true)
		{ }
	}

	public class EnumJsonConverter<T> : JsonConverter<T> where T : struct, Enum
	{
		private static readonly bool isFlags = typeof(T).IsDefined(typeof(FlagsAttribute), inherit: false);
		private static readonly string[] names = Enum.GetNames<T>(); // sorted by value
		private static readonly ulong[] values = names.Select(name => toUInt64(Enum.Parse<T>(name))).ToArray();

		private readonly bool writeAsString;

		public EnumJsonConverter(bool writeAsString)
		{
			this.writeAsString = writeAsString;
		}

		private static ulong toUInt64(T value) => Enum.GetUnderlyingType(typeof(T)) == typeof(ulong) ? Convert.ToUInt64(value) : unchecked((ulong)Convert.ToInt64(value));

		/// <summary>
		/// Name of the value as chosen by Newtonsoft.Json (binary search, so it can differ from Enum.ToString when more names have the same value).
		/// </summary>
		private static string getName(T value)
		{
			if (isFlags)
				return value.ToString();
			int index = Array.BinarySearch(values, toUInt64(value));
			return index >= 0 ? names[index] : null;
		}

		public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			switch (reader.TokenType)
			{
				case JsonTokenType.Number:
					return (T)Enum.ToObject(typeof(T), reader.TryGetInt64(out long number) ? number : (object)reader.GetUInt64());
				case JsonTokenType.String:
					string text = reader.GetString();
					if (Enum.TryParse(text, ignoreCase: true, out T value))
						return value;
					throw new JsonException($"Requested value '{text}' was not found in enum {typeof(T).FullName}.");
				default:
					throw new JsonException($"Unexpected token {reader.TokenType} when parsing enum {typeof(T).FullName}.");
			}
		}

		public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
		{
			if (writeAsString)
			{
				string name = getName(value);
				if (!string.IsNullOrEmpty(name) && !char.IsDigit(name[0]) && name[0] != '-') // values without a name are written as numbers
				{
					writer.WriteStringValue(name);
					return;
				}
			}
			writer.WriteRawValue(value.ToString("D"), skipInputValidation: true);
		}
	}
}
