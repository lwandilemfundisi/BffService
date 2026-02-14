using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;

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

        [HttpGet]
        [Route("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
            return Redirect("/home");
        }

        [HttpGet]
        [Route("user")]
        [Authorize]
        public async Task<IActionResult> GetUserAsync()
        {
            string accessToken = await HttpContext.GetTokenAsync("access_token");
            if (!await IsTokenActive(accessToken))
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
            var authToken = Convert.ToBase64String(Encoding.ASCII.GetBytes("OnlineTicketSalesBff:nluXYrk1ECM08fYYq9HOY1TBPPUaGXME"));
            introspectionClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authToken);

            introspectionRequest.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "token", accessToken }
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
