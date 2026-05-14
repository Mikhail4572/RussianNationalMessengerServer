using RussianNationalMessengerServer.Services;
using RussianNationalMessengerServer.Models;
using RussianNationalMessengerServer.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace RussianNationalMessengerServer.Controllers;


[ApiController]
[Route("api/[controller]")]
public class LoginController : ControllerBase
{
    private readonly RNMContext _context;
    private readonly IAuthService _authService;

    public LoginController(RNMContext context, IAuthService authService)
    {
        _context = context;
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> LoginAsync([FromBody] LoginDto loginDto)
    {
        if(!ModelState.IsValid)
            return BadRequest(ModelState);

        if (await _context.Users.FirstOrDefaultAsync(x => x.Username == loginDto.UserName && x.PasswordHash == loginDto.Password) is not User user)
            return StatusCode(401, "Неверный логин или пароль");

        var token = _authService.GenerateJwtToken(user);

        return Ok(new LoginResponseDto
        {
            Token = token,
            Login = user.Username,
            ExpiresAt = DateTime.Now.AddMinutes(60)
        });
    }
}
