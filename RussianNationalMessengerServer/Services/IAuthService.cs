using RussianNationalMessengerServer.Models;

namespace RussianNationalMessengerServer.Services;

public interface IAuthService
{
    string GenerateJwtToken(Account user);
}
