using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RussianNationalMessengerServer.Models;

public class Account
{
    [BsonId]
    [JsonPropertyName("id")]
    //[BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [BsonElement("username")]
    [JsonPropertyName("username")]
    public string Username { get; set; }

    [BsonElement("email")]
    [JsonPropertyName("email")]
    public string Email { get; set; }

    [BsonElement("passwordHash")]
    [JsonPropertyName("passwordHash")]
    public string PasswordHash { get; set; }

    [BsonElement("createdAt")]
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [BsonElement("lastSeenAt")]
    [JsonPropertyName("lastSeenAt")]
    public DateTime LastSeenAt { get; set; }

    [BsonElement("isOnline")]
    [JsonPropertyName("isOnline")]
    public bool IsOnline { get; set; }
}
