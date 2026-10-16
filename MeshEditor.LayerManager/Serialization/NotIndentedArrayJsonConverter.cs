using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MeshEditor.Common.Serialization;

namespace MeshEditor.LayerManager.Serialization
{
	/// <summary>
	/// Writes int and double arrays on a single line even in indented JSON.
	/// </summary>
	internal class NotIndentedArrayJsonConverter : JsonConverterFactory
	{
		public override bool CanConvert(Type typeToConvert)
		{
			return typeToConvert == typeof(int[]) || typeToConvert == typeof(double[]);
		}

		public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
		{
			if (typeToConvert == typeof(int[]))
				return new ArrayConverter<int>(value => value.ToString(CultureInfo.InvariantCulture));
			return new ArrayConverter<double>(value => double.IsNaN(value) || double.IsInfinity(value) ? $"\"{NewtonsoftNumberFormat.Format(value)}\"" : NewtonsoftNumberFormat.Format(value));
		}

		private class ArrayConverter<T> : JsonConverter<T[]>
		{
			private readonly Func<T, string> format;

			public ArrayConverter(Func<T, string> format)
			{
				this.format = format;
			}

			public override T[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
			{
				throw new NotImplementedException();
			}

			public override void Write(Utf8JsonWriter writer, T[] value, JsonSerializerOptions options)
			{
				writer.WriteRawValue("[" + string.Join(",", value.Select(format)) + "]", skipInputValidation: true);
			}
		}
	}
}
