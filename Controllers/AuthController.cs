
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Project_Version1.Services;
using Project_Version1.DTOs;

namespace Project_Version1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _auth;
        public AuthController(AuthService auth) => _auth = auth;


        [HttpPost("register")]
        public async Task<IActionResult> Register([FromForm] RegisterDto dto)
        {
            var res = await _auth.RegisterAsync(dto);
            if (res == null) return Conflict(new { message = "Email already exists" });
            return Ok(res);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var res = await _auth.LoginAsync(dto);
            if (res == null) return Unauthorized(new { message = "Invalid credentials" });
            return Ok(res);
        }
    }
}