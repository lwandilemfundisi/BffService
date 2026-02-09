using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BffService.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthController : ControllerBase
    {
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
        public IActionResult GetUser()
        {
            return Ok(new
                {
                    Name = User.Identity.Name,
                    Claims = User.Claims.Select(c => new { c.Type, c.Value })
                });
        }
    }
}
