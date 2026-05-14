using System.Text.Json.Serialization;

namespace RussianNationalMessengerServer.Dtos;

public class LoginDto
{
    [JsonPropertyName("username")]
    public string UserName { get; set; }

    [JsonPropertyName("password")]
    public string Password { get; set; }
}
