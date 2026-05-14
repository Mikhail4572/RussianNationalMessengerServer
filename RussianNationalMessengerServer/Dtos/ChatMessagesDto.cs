using RussianNationalMessengerServer.Models;
using System.Text.Json.Serialization;

namespace RussianNationalMessengerServer.Dtos;

public class ChatMessagesDto
{
    [JsonPropertyName("chat")]
    public Chat Chat { get; set; }

    [JsonPropertyName("messages")]
    public List<Message> Messages { get; set; }
}
