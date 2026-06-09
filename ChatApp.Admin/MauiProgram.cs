using ChatApp.Admin.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;

namespace ChatApp.Admin;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
			});

		builder.Services.AddMauiBlazorWebView();

		var config = new ConfigurationBuilder()
			.AddJsonFile("appsettings.json", optional: true)
			.Build();
		var apiBase = config.GetSection(ApiSettings.SectionName).Get<ApiSettings>()?.BaseUrl ?? "http://localhost:5200";

		builder.Services.AddSingleton<AdminAuthState>();
		builder.Services.AddSingleton<HttpApiClient>(sp =>
		{
			var client = new HttpClient();
			var auth = sp.GetRequiredService<AdminAuthState>();
			var api = new HttpApiClient(client, auth);
			api.SetBaseUrl(apiBase);
			return api;
		});
		builder.Services.AddSingleton<IAdminAuthService, HttpAdminAuthService>();
		builder.Services.AddSingleton<IAdminService, HttpAdminService>();

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
