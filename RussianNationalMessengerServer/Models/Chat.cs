using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace RussianNationalMessengerServer.Models;

public class Chat
{
    [BsonId]
    [JsonPropertyName("id")]
 //   [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [BsonElement("name")]
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [BsonElement("isGroup")]
    [JsonPropertyName("isGroup")]
    public bool IsGroup { get; set; }

    [BsonElement("createdAt")]
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    // участники
    [BsonElement("members")]
    [JsonPropertyName("members")]
    public List<string> Members { get; set; }

    [BsonElement("lastMessage")]
    [JsonPropertyName("lastMessage")]
    public LastMessage? LastMessage { get; set; }
}
