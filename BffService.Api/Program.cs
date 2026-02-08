using Duende.Bff;
using Duende.Bff.Yarp;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services
    .AddBff()
    .AddRemoteApis();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "My BFF API", Version = "v1" });
    // Optional: Add security definitions if your API uses authentication (e.g., JWT Bearer)
});

builder.Services.AddAuthentication(options => 
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;

})
.AddCookie(options => 
{
    options.Cookie.Name = "__Host-bff";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
})
.AddOpenIdConnect(options => 
{
    options.Authority = "http://localhost:84/realms/OnlineTicketSalesRealm/";
    options.ClientId = "OnlineTicketSalesBff";
    options.ClientSecret = "5kXXJJzPU7TMBbiScj7W0Y0DaAozc44R";
    options.ResponseType = OpenIdConnectResponseType.Code;
    options.SaveTokens = true;
});

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
    // Optional: set the UI at the app's root
    // options.RoutePrefix = string.Empty; 
});

app.UseForwardedHeaders(new ForwardedHeadersOptions 
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseBff();
app.MapBffManagementEndpoints();
app.MapRemoteBffApiEndpoint("/bff/events", new Uri("http://localhost:25964/")).WithAccessToken();
app.MapControllers();

app.Run();
