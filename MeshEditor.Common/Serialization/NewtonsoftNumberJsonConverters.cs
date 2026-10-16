using System;
using System.Buffers;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MeshEditor.Common.Serialization
{
	/// <summary>
	/// Floating-point numbers formatted as by Newtonsoft.Json: always with a decimal place ("1.0"),
	/// NaN and infinities as strings ("NaN", "Infinity", "-Infinity").
	/// </summary>
	public static class NewtonsoftNumberFormat
	{
		public static string Format(double value)
		{
			if (double.IsNaN(value) || double.IsInfinity(value))
				return value.ToString(CultureInfo.InvariantCulture);
			return ensureDecimalPlace(value.ToString("R", CultureInfo.InvariantCulture));
		}

		public static string Format(float value)
		{
			if (float.IsNaN(value) || float.IsInfinity(value))
				return value.ToString(CultureInfo.InvariantCulture);
			return ensureDecimalPlace(value.ToString("R", CultureInfo.InvariantCulture));
		}

		public static string Format(decimal value) => ensureDecimalPlace(value.ToString(CultureInfo.InvariantCulture));

		/// <summary>
		/// Writes the number as JSON value (NaN and infinities as strings). The indentation is written before raw values (Utf8JsonWriter does not indent them).
		/// </summary>
		public static void Write(Utf8JsonWriter writer, double value, string indentation = "")
		{
			if (double.IsNaN(value) || double.IsInfinity(value))
				writer.WriteStringValue(Format(value));
			else
				writer.WriteRawValue(indentation + Format(value), skipInputValidation: true);
		}

		public static void Write(Utf8JsonWriter writer, float value, string indentation = "")
		{
			if (float.IsNaN(value) || float.IsInfinity(value))
				writer.WriteStringValue(Format(value));
			else
				writer.WriteRawValue(indentation + Format(value), skipInputValidation: true);
		}

		public static void Write(Utf8JsonWriter writer, decimal value, string indentation = "") => writer.WriteRawValue(indentation + Format(value), skipInputValidation: true);

		private static string ensureDecimalPlace(string text)
		{
			if (text.IndexOf('.') != -1 || text.IndexOf('E') != -1 || text.IndexOf('e') != -1)
				return text;
			return text + ".0";
		}

		internal static double ReadDouble(ref Utf8JsonReader reader)
		{
			switch (reader.TokenType)
			{
				case JsonTokenType.Number:
					return reader.GetDouble();
				case JsonTokenType.String:
					string text = reader.GetString();
					if (double.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double value))
						return value;
					throw new JsonException($"Could not convert string to double: {text}.");
				default:
					throw new JsonException($"Unexpected token {reader.TokenType} when parsing double.");
			}
		}

		internal static decimal ReadDecimal(ref Utf8JsonReader reader)
		{
			switch (reader.TokenType)
			{
				case JsonTokenType.Number:
					if (reader.TryGetDecimal(out decimal value))
						return value;
					return parseDecimal(System.Text.Encoding.UTF8.GetString(reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan.ToArray()));
				case JsonTokenType.String:
					return parseDecimal(reader.GetString());
				default:
					throw new JsonException($"Unexpected token {reader.TokenType} when parsing decimal.");
			}
		}

		private static decimal parseDecimal(string text)
		{
			if (decimal.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out decimal value))
				return value;
			throw new JsonException($"Could not convert string to decimal: {text}.");
		}
	}

	public class NewtonsoftDoubleJsonConverter : JsonConverter<double>
	{
		public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => NewtonsoftNumberFormat.ReadDouble(ref reader);

		public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options) => NewtonsoftNumberFormat.Write(writer, value);

		public override double ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => double.Parse(reader.GetString(), NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture);

		public override void WriteAsPropertyName(Utf8JsonWriter writer, double value, JsonSerializerOptions options) => writer.WritePropertyName(value.ToString(CultureInfo.InvariantCulture));
	}

	public class NewtonsoftSingleJsonConverter : JsonConverter<float>
	{
		// Newtonsoft.Json reads every floating-point number as double and then converts it
		public override float Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => (float)NewtonsoftNumberFormat.ReadDouble(ref reader);

		public override void Write(Utf8JsonWriter writer, float value, JsonSerializerOptions options) => NewtonsoftNumberFormat.Write(writer, value);

		public override float ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => float.Parse(reader.GetString(), NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture);

		public override void WriteAsPropertyName(Utf8JsonWriter writer, float value, JsonSerializerOptions options) => writer.WritePropertyName(value.ToString(CultureInfo.InvariantCulture));
	}

	public class NewtonsoftDecimalJsonConverter : JsonConverter<decimal>
	{
		public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => NewtonsoftNumberFormat.ReadDecimal(ref reader);

		public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options) => NewtonsoftNumberFormat.Write(writer, value);

		public override decimal ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => decimal.Parse(reader.GetString(), NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture);

		// dictionary keys are written without the added decimal place
		public override void WriteAsPropertyName(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options) => writer.WritePropertyName(value.ToString(CultureInfo.InvariantCulture));
	}
}
