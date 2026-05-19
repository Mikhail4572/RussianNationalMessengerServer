using System.Text.Json.Serialization;

namespace RussianNationalMessengerServer.Dtos;

public enum TypeMessage
{
    Error = 0,
    Complete = 1,
    Other = 2
}

public class ResponseDto
{
    [JsonPropertyName("message")]
    public required string Message { get; set; }

    [JsonPropertyName("type")]
    public TypeMessage Type { get; set; } = TypeMessage.Error;
}
