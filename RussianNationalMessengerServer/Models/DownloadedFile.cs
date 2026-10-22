namespace RussianNationalMessengerServer.Models;

public class DownloadedFile
{
    public required Stream Stream { get; init; }
    public required string ContentType { get; init; }
    public required string FileName { get; init; }
}
