using Duende.Bff;
using Duende.Bff.AccessTokenManagement;
using Duende.Bff.Yarp;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services
    .AddBff(opts => opts.ManagementBasePath = "/bff")
    .AddRemoteApis();

builder.Services.AddAuthentication(options => 
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;

})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options => 
{
    options.Cookie.Name = "__Host-bff";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.Path = "/";
})
.AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options => 
{
    options.Authority = "https://localhost:8443/realms/OnlineTicketSalesRealm";
    options.ClientId = "OnlineTicketSalesBff";
    options.ClientSecret = "nluXYrk1ECM08fYYq9HOY1TBPPUaGXME";
    options.ResponseType = OpenIdConnectResponseType.Code;
    options.SaveTokens = true;
    options.RequireHttpsMetadata = true;
    options.GetClaimsFromUserInfoEndpoint = true;
    options.CallbackPath = "/signin-oidc";
    options.SignedOutCallbackPath = "/signout-callback-oidc";

    options.Scope.Clear();
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("email");
    options.Scope.Add("eventsApi-scope");
});

builder.Services.AddAuthorization();
builder.Services.AddReverseProxy()
    .LoadFromMemory(
    new[] 
    {
        new Yarp.ReverseProxy.Configuration.RouteConfig()
        {
            RouteId = "eventsApi",
            ClusterId = "eventsApiCluster",
            Match = new Yarp.ReverseProxy.Configuration.RouteMatch()
            {
                Path = "/bff/events/{**catch-all}"
            },
            //{
            //    new Yarp.ReverseProxy.Transforms.PathRemovePrefixTransformFactory("/events")
            //}
        }
    },
    new[]
    {
        new Yarp.ReverseProxy.Configuration.ClusterConfig()
        {
            ClusterId = "eventsApiCluster",
            Destinations = new Dictionary<string, Yarp.ReverseProxy.Configuration.DestinationConfig>()
            {
                { "destination1", new Yarp.ReverseProxy.Configuration.DestinationConfig() { Address = "https://localhost:25965/" } }
            }
        }
    });

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();
app.UseForwardedHeaders();
//app.UsePathBase("/bff");

app.UseRouting();
app.UseAuthentication();
app.UseBff();
app.UseAuthorization();

app.MapBffManagementEndpoints();
app.MapRemoteBffApiEndpoint("/bff/events", new Uri("https://localhost:25965/")).WithAccessToken(RequiredTokenType.User).RequireAuthorization();

app.MapGet("/bff/debug", (HttpContext ctx) => 
{
    return new 
    {
        Authenticated = ctx.User.Identity?.IsAuthenticated,
        Name = ctx.User.Identity?.Name,
        Path = ctx.Request.Path,
        PathBase = ctx.Request.PathBase
    };
}).RequireAuthorization();
app.MapControllers();

app.Run();
