using System.Text.Json.Serialization;

namespace RussianNationalMessengerServer.Dtos;

public class SendMessageDto
{
    [JsonPropertyName("chatId")]
    public required string ChatId { get; set; }

    [JsonPropertyName("content")]
    public string? Content { get; set; } 

    [JsonPropertyName("attachments")]
    public List<IFormFile>? Attachments { get; set; } = null;
}
