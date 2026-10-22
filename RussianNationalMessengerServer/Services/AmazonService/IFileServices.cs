using RussianNationalMessengerServer.Models;

namespace RussianNationalMessengerServer.Services.AmazonService;

public interface IFileServices
{
    Task<Attachment?> UploadFile(IFormFile file, CancellationToken token = default);
    Task<DownloadedFile?> GetFile(string objectKey, string fileName, string contentType, CancellationToken token = default);
    Task<bool> DeleteFile(string objectKey, CancellationToken token = default);
}
