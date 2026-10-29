using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Driver;
using RussianNationalMessengerServer.Dtos;
using RussianNationalMessengerServer.Models;
using RussianNationalMessengerServer.Services.AmazonService;
using RussianNationalMessengerServer.Services.ChatHub;

namespace RussianNationalMessengerServer.Controllers;



[ApiController]
[Route("api/[controller]")]
public class MessageController : Controller
{
    private readonly IHubContext<RNMHub> _hubContext;
    private readonly IFileServices _fileServices;
    private readonly MongoService _context;

    public MessageController(IFileServices fileServices, MongoService context, IHubContext<RNMHub> hubContext)
    {
        _context = context;
        _fileServices = fileServices;
        _hubContext = hubContext;
    }

    [HttpPost("send")]
    public async Task<IResult> SendMessage([FromForm] SendMessageDto messageDto)
    {
        var username = User.Identity?.Name;

        if (string.IsNullOrEmpty(username))
            return Results.Problem(
                title: "Вы не авторизованы",
                statusCode: StatusCodes.Status401Unauthorized);


        List<Attachment>? attachments = null;

        if (messageDto.Attachments is not null && messageDto.Attachments.Count > 0)
        {
            var taskLoadFiles = messageDto.Attachments.Select(async x => await _fileServices.UploadFile(x));

            attachments = (await Task.WhenAll(taskLoadFiles)).Where(x => x != null).ToList()!;
        }

        Message message = new()
        {
            Id = Guid.NewGuid().ToString(),
            Attachments = attachments,
            Author = username,
            ChatId = messageDto.ChatId,
            Content = messageDto.Content,
            SentAt = DateTime.UtcNow            
        };

        _context.Messages.InsertOne(message);

        await _context.Chats.UpdateOneAsync(x => x.Id == message.ChatId,
          Builders<Chat>.Update.Set(x => x.LastMessage, new()
          {
              Author = username,
              Content = message.Content,
              MessageId = message.Id,
              SentAt = message.SentAt
          })
        );

        await _hubContext.Clients.Group(message.ChatId).SendAsync();

        return Results.Created();
    }
}
