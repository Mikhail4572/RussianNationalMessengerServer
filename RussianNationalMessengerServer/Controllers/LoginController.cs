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
    public async Task<IResult> LoginAsync([FromBody] LoginDto loginDto)
    {
        if (!ModelState.IsValid)
        {
            return Results.Problem(
                title: "Некорректный JSON",
                statusCode: StatusCodes.Status400BadRequest
                );
        }

        var user = await _context.Accounts.Find(x => x.Username == loginDto.UserName && x.PasswordHash == loginDto.Password).FirstOrDefaultAsync();

        if (user is null)
        {
            return Results.Problem(
                title: "Неверный логин или пароль",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var token = _authService.GenerateJwtToken(user);

        user.PasswordHash = "";


        return Results.Ok(new LoginResponseDto
        {
            Token = token,
            User = user
        });
    }
}
