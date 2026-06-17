using ChatApp.Web.Hubs;
using ChatApp.Web.Services;
using ChatApp.Web.Services.Http;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ApiSettings>(builder.Configuration.GetSection(ApiSettings.SectionName));
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient<ApiHttpClient>();

builder.Services.AddControllersWithViews();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddScoped<IUserAccountService, HttpUserAccountService>();
builder.Services.AddScoped<IChatService, HttpChatService>();
builder.Services.AddScoped<IFriendService, HttpFriendService>();
builder.Services.AddScoped<IGroupService, HttpGroupService>();
builder.Services.AddSingleton<ApiRealtimeBridge>();
builder.Services.AddSignalR();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllers();
app.MapHub<WebChatHub>("/hubs/chat");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
