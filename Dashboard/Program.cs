using Dashboard.Auth;
using Dashboard.Hubs;
using Dashboard.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages(options => options.Conventions.AuthorizePage("/Edit"));
builder.Services.AddSingleton<StatusStore>();
builder.Services.AddSingleton<HostStore>();
builder.Services.AddHttpClient();
builder.Services.AddHttpClient("KumaStatusService", client => client.Timeout = TimeSpan.FromSeconds(10));
builder.Services.AddHttpClient<GitHubBadgeService>();
builder.Services.AddSingleton<KumaStatusService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<KumaStatusService>());
builder.Services.AddMemoryCache();
builder.Services.AddScoped<ApiKeyAuthFilter>();
builder.Services.AddControllers();
builder.Services.AddSignalR();

// Sign in through Pocket ID, remembered with a cookie. Only the editor requires it; everything else stays public.
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
}).AddCookie().AddOpenIdConnect(options =>
{
    builder.Configuration.GetSection("Oidc").Bind(options);
    options.ResponseType = OpenIdConnectResponseType.Code;
});

// Trust the scheme cloudflared forwards, so Pocket ID is sent an https return address rather than the tunnel's plain http.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// Build the app and configure the pipeline.
WebApplication app = builder.Build();
app.UseForwardedHeaders();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();
app.MapControllers();
app.MapHub<ServiceStatusHub>("/serviceStatusHub");
app.Run();