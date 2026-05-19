using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace RussianNationalMessengerServer.Models;

public class LastMessage
{
    [BsonId]
    [JsonPropertyName("messageId")]
    public string MessageId { get; set; }

    [BsonElement("author")]
    [JsonPropertyName("author")]
    public string Author { get; set; }

    [BsonElement("content")]
    [JsonPropertyName("content")]
    public string Content { get; set; }

    [BsonElement("sentAt")]
    [JsonPropertyName("sentAt")]
    public DateTime SentAt { get; set; }
}
