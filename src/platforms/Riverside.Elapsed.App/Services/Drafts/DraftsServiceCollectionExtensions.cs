using Riverside.Elapsed.App.Services.Drafts;

namespace Riverside.Elapsed.App.Extensions;

public static class DraftsServiceCollectionExtensions
{
	public static IServiceCollection AddDrafts(this IServiceCollection services)
	{
		services.AddSingleton<ILocalDraftRepository, LocalDraftRepository>();
		return services;
	}
}
