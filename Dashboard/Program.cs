using Dashboard.Data;
using Dashboard.Services;
using Microsoft.EntityFrameworkCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages();
builder.Services.AddSingleton<StatusStore>();
builder.Services.AddHttpClient();
builder.Services.AddHttpClient<HealthCheckService>();
builder.Services.AddHostedService<HealthCheckService>();
builder.Services.AddDbContext<DashboardDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("Robert1")));
builder.Services.AddScoped<DnsUpdateService>();
builder.Services.AddMemoryCache();

WebApplication app = builder.Build();
app.UseRouting();
app.UseAuthorization();
app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();
app.Run();