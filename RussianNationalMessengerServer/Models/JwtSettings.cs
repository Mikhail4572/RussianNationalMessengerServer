using System.Text.Json.Serialization;

namespace RussianNationalMessengerServer.Models;

public class JwtSettings
{
    [JsonPropertyName("Key")]
    public string Key { get; set; }

    [JsonPropertyName("Issuer")]
    public string Issuer { get; set; }

    [JsonPropertyName("Audience")]
    public string Audience { get; set; }

    [JsonPropertyName("ExpirationMinutes")]
    public int ExpirationMinutes { get; set; }
}
