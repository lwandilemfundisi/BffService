using BffService.Api.Helpers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
                RedirectUri = string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl,
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
            System.IO.File.CreateText("accessToken.txt").WriteLine(accessToken);

            if (!await CookiesHepler.IsTokenActive(accessToken))
            {
                return Unauthorized();
            }

            return Ok(new
            {
                Claims = User.Claims.Select(c => new { c.Type, c.Value })
            });
        }
    }
}
