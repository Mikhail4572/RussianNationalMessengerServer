namespace RussianNationalMessengerServer.Dtos;

public class ResponseAttachmentDto
{
    public string Id { get; set; }
    public string FileName { get; set; }
    public string ContentType { get; set; }
    public long Size { get; set; }
}
