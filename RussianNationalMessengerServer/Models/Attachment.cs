using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace RussianNationalMessengerServer.Models;

public class Attachment
{
    [BsonId]
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [BsonElement("fileName")]
    [JsonPropertyName("fileName")]
    public string FileName { get; set; }

    [BsonElement("objectKey")]
    [JsonPropertyName("objectKey")]
    public string ObjectKey { get; set; }

    [BsonElement("contentType")]
    [JsonPropertyName("contentType")]
    public string ContentType { get; set; }

    [BsonElement("size")]
    [JsonPropertyName("size")]
    public long Size { get; set; }
}
