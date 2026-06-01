#if false
namespace Riverside.Elapsed.App.Services.Endpoints;

internal class DebugHttpHandler : DelegatingHandler
{
	private readonly ILogger _logger;

	public DebugHttpHandler(ILogger<DebugHttpHandler> logger, HttpMessageHandler? innerHandler = null)
		: base(innerHandler ?? new HttpClientHandler())
	{
		_logger = logger;
	}

	protected async override Task<HttpResponseMessage> SendAsync(
		HttpRequestMessage request,
		CancellationToken cancellationToken)
	{
		var response = await base.SendAsync(request, cancellationToken);
#if DEBUG
		if (!response.IsSuccessStatusCode)
		{
			_logger.LogDebug("Unsuccessful API Call");
			if (request.RequestUri is not null)
			{
				_logger.LogDebug($"{request.RequestUri} ({request.Method})");
			}
			
			foreach ((var key, var values) in request.Headers.ToDictionary(x => x.Key, x => string.Join(", ", x.Value)))
			{
				_logger.LogDebug($"{key}: {values}");
			}

			var content = request.Content is not null ? await request.Content.ReadAsStringAsync() : null;
			if (!string.IsNullOrEmpty(content))
			{
				_logger.LogDebug(content);
			}

			// Uncomment to automatically break when an API call fails while debugging
			// System.Diagnostics.Debugger.Break();
		}
#endif
		return response;
	}
}

#endif
