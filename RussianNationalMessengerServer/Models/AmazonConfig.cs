using System.Text.Json.Serialization;

namespace RussianNationalMessengerServer.Models;

public class AmazonConfig
{
    [JsonPropertyName("containerName")]
    public string ContainerName { get; set; }

    [JsonPropertyName("keyName")]
    public string KeyName { get; set; }

    [JsonPropertyName("keyID")]
    public string KeyID { get; set; }

    [JsonPropertyName("applicationKey")]
    public string ApplicationKey { get; set; }

    [JsonPropertyName("clientConfig")]
    public ClientConfig ClientConfig { get; set; }
}

public class ClientConfig
{
    
    [JsonPropertyName("serviceURL")]
    public string ServiceURL { get; set; }

    [JsonPropertyName("forcePathStyle")]
    public bool ForcePathStyle { get; set; }
}

