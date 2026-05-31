using System.Text.Json;
using System.Text.Json.Serialization;

namespace Riverside.Elapsed.App.Converters.Json;

public sealed class UriJsonConverter : JsonConverter<Uri>
{
	public override Uri? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var s = reader.GetString();
		if (string.IsNullOrWhiteSpace(s))
			return new Uri("about:blank");

		return new Uri(s, UriKind.Absolute);
	}

	public override void Write(Utf8JsonWriter writer, Uri value, JsonSerializerOptions options)
	{
		writer.WriteStringValue(value.ToString());
	}
}
