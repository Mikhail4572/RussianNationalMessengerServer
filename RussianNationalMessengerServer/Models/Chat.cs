using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RussianNationalMessengerServer.Models;

[Table("Chat")]
public class Chat
{
    [Key]
    [Column("_id")]
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("isGroup")]
    public bool IsGroup { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    // участники
    [JsonPropertyName("members")]
    public List<string> Members { get; set; }
}
