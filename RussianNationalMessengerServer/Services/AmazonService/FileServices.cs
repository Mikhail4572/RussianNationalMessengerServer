using RussianNationalMessengerServer.Models;
using Microsoft.Extensions.Options;
using Amazon.S3.Model;
using System.Net;
using Amazon.S3;

namespace RussianNationalMessengerServer.Services.AmazonService;

public class FileServices : IFileServices
{
    private readonly IAmazonS3 _s3Client;
    private readonly AmazonConfig _amazonConfig;

    public FileServices(IAmazonS3 s3Client, IOptions<AmazonConfig> options)
    {
        _s3Client = s3Client;
        _amazonConfig = options.Value;
    }

    public async Task<Attachment?> UploadFile(IFormFile file, CancellationToken token = default)
    {
        string key = Guid.NewGuid().ToString();
        string fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);

        PutObjectRequest request = new()
        {
            BucketName = _amazonConfig.ContainerName,
            Key = fileName,
            InputStream = file.OpenReadStream(),
            ContentType = file.ContentType
        };

        var result = await _s3Client.PutObjectAsync(request, token);

        if (result.HttpStatusCode != HttpStatusCode.OK)
            return null;


        return new()
        {
            ContentType = file.ContentType,
            FileName = file.FileName,
            ObjectKey = key,
            Size = file.Length
        };
    }

    public async Task<DownloadedFile?> GetFile(string objectKey, string fileName, string contentType, CancellationToken token = default)
    {
        var response = await _s3Client.GetObjectAsync(_amazonConfig.ContainerName, objectKey, token);

        if (response.HttpStatusCode != HttpStatusCode.OK)
            return null;

        return new()
        {
            Stream = response.ResponseStream,
            ContentType = contentType,
            FileName = fileName
        };
    }

    public async Task<bool> DeleteFile(string objectKey, CancellationToken token = default)
    {
        await _s3Client.DeleteObjectAsync(_amazonConfig.ContainerName, objectKey, token);
        return default;
    }
}
