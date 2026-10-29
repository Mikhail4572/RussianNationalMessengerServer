
namespace RussianNationalMessengerServer.Dtos;

public class ResponseMessageDto
{
    public string MessageId { get; set; }
    public string Content { get; set; }
    public DateTime SentAt { get; set; }
    public List<ResponseAttachmentDto> Attachments { get; set; }
}
