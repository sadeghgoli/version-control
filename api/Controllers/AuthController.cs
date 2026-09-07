using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace UpdateCenter.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _config;

    public AuthController(IConfiguration config)
    {
        _config = config;
    }

    [HttpGet("me")]
    public IActionResult Me()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Unauthorized();
        }

        return Ok(new
        {
            username = User.Identity.Name,
            displayName = User.FindFirst("displayName")?.Value ?? User.Identity.Name
        });
    }

    [AllowAnonymous]
    [HttpPost("dev-login")]
    public async Task<IActionResult> DevLogin([FromBody] DevLoginRequest request)
    {
        if (_config.GetValue("Sso:Enabled", false))
        {
            return BadRequest(new { message = "ورود آزمایشی وقتی SSO فعال است مجاز نیست." });
        }

        var username = string.IsNullOrWhiteSpace(request.Username) ? "admin" : request.Username.Trim();
        await SignInAsync(username);
        return Ok(new { username, displayName = username });
    }

    [AllowAnonymous]
    [HttpGet("sso-login")]
    public IActionResult SsoLogin()
    {
        var authorize = _config["Sso:AuthorizeUrl"];
        if (string.IsNullOrWhiteSpace(authorize))
        {
            return BadRequest(new { message = "آدرس SSO تنظیم نشده است. از ورود آزمایشی استفاده کنید." });
        }

        var callback = _config["Sso:CallbackUrl"] ?? $"{Request.Scheme}://{Request.Host}/api/auth/callback";
        var clientId = _config["Sso:ClientId"] ?? "update-center";
        var url = $"{authorize}?response_type=code&client_id={Uri.EscapeDataString(clientId)}&redirect_uri={Uri.EscapeDataString(callback)}";
        return Redirect(url);
    }

    [AllowAnonymous]
    [HttpGet("callback")]
    public async Task<IActionResult> Callback([FromQuery] string? code, [FromQuery] string? name)
    {
        var username = string.IsNullOrWhiteSpace(name) ? (string.IsNullOrWhiteSpace(code) ? "sso-user" : code[..Math.Min(12, code.Length)]) : name;
        await SignInAsync(username);
        var panel = _config["Panel:Origin"] ?? "http://localhost:5174";
        return Redirect(panel);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok();
    }

    private async Task SignInAsync(string username)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, username),
            new("displayName", username)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = true }
        );
    }

    public record DevLoginRequest(string? Username);
}
