using BffService.Api.Helpers;
using Duende.Bff;
using Duende.Bff.Yarp;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.OpenApi;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services
    .AddBff(opts=> opts.ManagementBasePath = "/bff")
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

    options.Events.OnValidatePrincipal = async context =>
    {
        using (FileStream fs = File.Create("C:\\MyLogs\\Bff\\cookie_validation_log.txt"))
        {
            var accessToken = await context.HttpContext.GetTokenAsync("access_token");
            var data = Encoding.UTF8.GetBytes($"Validating cookie at {DateTime.UtcNow}, Access Token: {accessToken}\n");
            fs.Write(data, 0, data.Length);
            if (string.IsNullOrEmpty(accessToken))
            {
                data = Encoding.UTF8.GetBytes("No access token found in cookie.\n");
                fs.Write(data, 0, data.Length);
                context.RejectPrincipal();
                data = Encoding.UTF8.GetBytes("Rejecting principal due to missing access token.\n");
                fs.Write(data, 0, data.Length);
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                data = Encoding.UTF8.GetBytes("Signed out user due to missing access token.\n");
                fs.Write(data, 0, data.Length);
                return;
            }

            data = Encoding.UTF8.GetBytes("Access token found, validating with introspection endpoint.\n");
            fs.Write(data, 0, data.Length);
            var isActive = await CookiesHepler.IsTokenActive(accessToken);
            if (!isActive)
            {
                data = Encoding.UTF8.GetBytes("Access token is not active, rejecting principal.\n");
                fs.Write(data, 0, data.Length);
                context.RejectPrincipal();
                    data = Encoding.UTF8.GetBytes("Rejecting principal due to inactive access token.\n");
                    fs.Write(data, 0, data.Length);
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                data = Encoding.UTF8.GetBytes("Signed out user due to inactive access token.\n");
                fs.Write(data, 0, data.Length);
            }

        }
    };
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
});

builder.Services.AddAuthorization();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();
app.UseForwardedHeaders();
app.UsePathBase("/bff");

app.UseRouting();
app.UseAuthentication();
app.UseBff();
app.UseAuthorization();

app.MapBffManagementEndpoints();
//app.MapRemoteBffApiEndpoint("/bff/events", new Uri("http://localhost:25964/")).WithAccessToken();
app.MapControllers();

app.Run();
