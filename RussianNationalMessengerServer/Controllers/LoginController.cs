using RussianNationalMessengerServer.Services;
using RussianNationalMessengerServer.Models;
using RussianNationalMessengerServer.Dtos;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace RussianNationalMessengerServer.Controllers;


[ApiController]
[Route("api/[controller]")]
public class LoginController : ControllerBase
{
    private readonly MongoService _context;
    private readonly IAuthService _authService;

    public LoginController(MongoService context, IAuthService authService)
    {
        _context = context;
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> LoginAsync([FromBody] LoginDto loginDto)
    {
        if(!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _context.Accounts.Find(x => x.Username == loginDto.UserName && x.PasswordHash == loginDto.Password).FirstOrDefaultAsync();

        if(user is null)
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
