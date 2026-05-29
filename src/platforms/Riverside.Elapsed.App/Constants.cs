using System.Text.Json;
using System.Text.Json.Serialization;
using Riverside.Elapsed.App.Converters.Json;

namespace Riverside.Elapsed.App;

public static partial class Constants
{
	public static readonly JsonSerializerOptions SerializerOptions = GetJsonSerializerOptions();

	public static JsonSerializerOptions GetJsonSerializerOptions()
	{
		var options = new JsonSerializerOptions
		{
			WriteIndented = true,
			PropertyNameCaseInsensitive = true,
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
			DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
			IgnoreReadOnlyProperties = true,
			NumberHandling = JsonNumberHandling.Strict,
			//ReferenceHandler = ReferenceHandler.IgnoreCycles,
		};

		options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
		//options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, true));
		//options.Converters.Add(new TimeSpanSecondsJsonConverter());
		//options.Converters.Add(new UriJsonConverter());

		return options;
	}
}
