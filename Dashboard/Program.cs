using Dashboard.Auth;
using Dashboard.Hubs;
using Dashboard.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages();
builder.Services.AddSingleton<StatusStore>();
builder.Services.AddHttpClient();
builder.Services.AddHttpClient("HealthCheckService", client => client.Timeout = TimeSpan.FromSeconds(5));
builder.Services.AddHttpClient<GitHubBadgeService>();
builder.Services.AddSingleton<HealthCheckService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<HealthCheckService>());
builder.Services.AddMemoryCache();
builder.Services.AddScoped<ApiKeyAuthFilter>();
builder.Services.AddControllers();
builder.Services.AddSignalR();

WebApplication app = builder.Build();
app.UseRouting();
app.UseAuthorization();
app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();
app.MapControllers();
app.MapHub<ServiceStatusHub>("/serviceStatusHub");
app.Run();