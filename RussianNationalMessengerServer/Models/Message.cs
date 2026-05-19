using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace RussianNationalMessengerServer.Models;

public class Message
{
    [BsonId]
    [JsonPropertyName("id")]
//    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [BsonElement("chatId")]
    [JsonPropertyName("chatId")]
    public string ChatId { get; set; }

    [BsonElement("author")]
    [JsonPropertyName("author")]
    public string Author { get; set; }

    [BsonElement("content")]
    [JsonPropertyName("content")]
    public string Content { get; set; }

    [BsonElement("sentAt")]
    [JsonPropertyName("sentAt")]
    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    [BsonElement("isEdited")]
    [JsonPropertyName("isEdited")]
    public bool IsEdited { get; set; } = false;

    [BsonElement("isDeleted")]
    [JsonPropertyName("isDeleted")]
    public bool IsDeleted { get; set; } = false;
}
