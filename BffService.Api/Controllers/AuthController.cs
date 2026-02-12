using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BffService.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthController(IHttpClientFactory httpClientFactory) : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

        [HttpGet]
        [Route("login")]
        public IActionResult Login(string returnUrl)
        {
            var authProps = new AuthenticationProperties
            {
                RedirectUri = returnUrl,
            };

            return Challenge(authProps, OpenIdConnectDefaults.AuthenticationScheme);
        }

        [HttpPost]
        [Route("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
            return Ok();
        }

        [HttpGet]
        [Route("user")]
        [Authorize]
        public async Task<IActionResult> GetUserAsync()
        {
            if (await IsTokenActive(User.Claims.FirstOrDefault(c => c.Type == "access_token")?.Value) == false)
            {
                return Unauthorized();
            }

            return Ok(new
            {
                Claims = User.Claims.Select(c => new { c.Type, c.Value })
            });
        }

        public async Task<bool> IsTokenActive(string accessToken)
        {
            var introspectionClient = _httpClientFactory.CreateClient();
            var introspectionRequest = new HttpRequestMessage(HttpMethod.Post, "https://localhost:8443/realms/OnlineTicketSalesRealm/protocol/openid-connect/token/introspect");
            introspectionRequest.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "token", accessToken },
                { "client_id", "OnlineTicketSalesBff" },
                { "client_secret", "nluXYrk1ECM08fYYq9HOY1TBPPUaGXME" }
            });
            var response = await introspectionClient.SendAsync(introspectionRequest);
            if (!response.IsSuccessStatusCode)
            {
                return false;
            }
            var content = await response.Content.ReadAsStringAsync();
            var tokenInfo = System.Text.Json.JsonDocument.Parse(content);
            return tokenInfo.RootElement.GetProperty("active").GetBoolean();

        }
    }
}
