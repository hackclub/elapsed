using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Riverside.Elapsed.App.Converters.Json;

public sealed class TimeSpanSecondsJsonConverter : JsonConverter<TimeSpan>
{
	public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType == JsonTokenType.Number && reader.TryGetDouble(out var seconds))
			return TimeSpan.FromSeconds(seconds);

		if (reader.TokenType == JsonTokenType.String)
		{
			var s = reader.GetString();
			if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var sec))
				return TimeSpan.FromMilliseconds(sec);
		}

		throw new JsonException("Invalid timespan value.");
	}

	public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
	{
		writer.WriteNumberValue(value.TotalSeconds);
	}
}
