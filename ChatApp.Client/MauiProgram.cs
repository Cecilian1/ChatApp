using ChatApp.Client.Services;
using Microsoft.Extensions.Configuration;

namespace ChatApp.Client;

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
        var apiSettings = config.GetSection(ApiSettings.SectionName).Get<ApiSettings>() ?? new ApiSettings();
        builder.Services.AddSingleton(apiSettings);

        builder.Services.AddSingleton<UserAuthState>();
        builder.Services.AddSingleton<HttpApiClient>(sp =>
        {
            var client = new HttpClient();
            var auth = sp.GetRequiredService<UserAuthState>();
            var settings = sp.GetRequiredService<ApiSettings>();
            var api = new HttpApiClient(client, auth, settings);
            return api;
        });
        builder.Services.AddSingleton<IUserAuthService, HttpUserAuthService>();
        builder.Services.AddSingleton<IChatAppService, HttpChatAppService>();
        builder.Services.AddSingleton<ChatHubService>();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
#endif

        return builder.Build();
    }
}
