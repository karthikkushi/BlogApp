using Microsoft.AspNetCore.Mvc;
using BlogApp.Api.Services;
using System.Threading.Tasks;
using BlogApp.Api.Models;

namespace BlogApp.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("signup")]
        public async Task<IActionResult> SignUp([FromBody] SignUpRequest request)
        {
            if (string.IsNullOrEmpty(request.Email) ||
                string.IsNullOrEmpty(request.Password) ||
                string.IsNullOrEmpty(request.Username))
            {
                return BadRequest("Email, password, and username are required.");
            }

            try
            {
                var session = await _authService.SignUpAsync(
                    request.Email,
                    request.Password,
                    request.Username
                );

                if (session == null || session.User == null)
                {
                    return BadRequest("Sign-up failed. Please check your email and password.");
                }

                return Ok(new
                {
                    token = session.AccessToken,
                    userId = session.User.Id
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error during sign-up: {ex.Message}");
            }
        }

        [HttpPost("signin")]
        public async Task<IActionResult> SignIn([FromBody] SignInRequest request)
        {
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest("Email and password are required.");
            }

            try
            {
                var session = await _authService.SignInAsync(request.Email, request.Password);
                if (session == null || session.User == null)
                {
                    return BadRequest("Sign-in failed. Please check your email and password.");
                }

                return Ok(new
                {
                    token = session.AccessToken,
                    userId = session.User.Id
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error during sign-in: {ex.Message}");
            }
        }
    }
}
