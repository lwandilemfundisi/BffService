using BffService.Api.Helpers;
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
            return Redirect("/");
        }

        [HttpGet]
        [Route("user")]
        [Authorize]
        public async Task<IActionResult> GetUserAsync()
        {
            string accessToken = await HttpContext.GetTokenAsync("access_token");
            if (!await CookiesHepler.IsTokenActive(accessToken))
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
                return Unauthorized();
            }

            return Ok(new
            {
                Claims = User.Claims.Select(c => new { c.Type, c.Value })
            });
        }
    }
}
