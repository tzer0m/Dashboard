using Dashboard.Auth;
using Dashboard.Services;
using Microsoft.EntityFrameworkCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages();
builder.Services.AddSingleton<StatusStore>();
builder.Services.AddHttpClient();
builder.Services.AddHttpClient<HealthCheckService>();
builder.Services.AddHttpClient<GitHubBadgeService>();
builder.Services.AddHostedService<HealthCheckService>();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<ApiKeyAuthFilter>();
builder.Services.AddControllers();

WebApplication app = builder.Build();
app.UseRouting();
app.UseAuthorization();
app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();
app.MapControllers();
app.Run();