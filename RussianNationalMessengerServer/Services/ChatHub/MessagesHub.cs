using Microsoft.AspNetCore.SignalR;
using MongoDB.Bson;
using MongoDB.Driver;
using RussianNationalMessengerServer.Dtos;
using RussianNationalMessengerServer.Models;
using System.Security.Claims;

namespace RussianNationalMessengerServer.Services.ChatHub;

public partial class RNMHub
{
    public async Task RemoveMessage(string messageId, string chatId)
    {
        if (!_context.Messages.Find(x => x.Id == messageId).Any())
            return;

        await _context.Messages.DeleteOneAsync(x => x.Id == messageId);

        var chat = await _context.Chats.Find(x => x.Id == chatId).FirstOrDefaultAsync();

        if (chat is null)
            return;

        var isLastMessage = (await _context.Chats.Find(x => x.Id == chatId).FirstOrDefaultAsync())?.LastMessage?.MessageId == messageId;

        if (isLastMessage)
        {
            var lastMessage = await _context.Messages.Find(x => x.ChatId == chatId).SortByDescending(x => x.SentAt).FirstOrDefaultAsync();

            LastMessage? message = lastMessage is null ? null : new()
            {
                MessageId = lastMessage.Id,
                Content = lastMessage.Content,
                Author = lastMessage.Author,
                SentAt = lastMessage.SentAt
            };

            await _context.Chats.UpdateOneAsync(x => x.Id == chatId, Builders<Chat>.Update.Set(x => x.LastMessage, message));
        }

        await Clients.Group(chat.Id).SendAsync("onRemoveMessage", chat.Id, messageId);
    }

    //----------------------------------------------------------------------------------------------------------------------------------------------
    public async Task SendMessage(SendMessageDto messageDto)
    {
        var user_id = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(user_id))
            return;

        // Проверка: пользователь в чате?
        var isMember = await _context.Chats.Find(x => x.Id == messageDto.ChatId && x.Members.Contains(user_id)).AnyAsync();

        if (!isMember)
            return;

        List<Attachment>? attachments = null;

        if (messageDto.Attachments is not null && messageDto.Attachments.Count > 0)
        {
            var taskLoadFiles = messageDto.Attachments.Select(async x => await _fileServices.UploadFile(x));

            attachments = (await Task.WhenAll(taskLoadFiles)).Where(x => x != null).ToList()!;
        }

        Message add = new()
        {
            Id = Guid.NewGuid().ToString(),
            Author = user_id,
            ChatId = messageDto.ChatId,
            Content = messageDto.Content ?? string.Empty,
            Attachments = attachments
        };

        await _context.Messages.InsertOneAsync(add);

        await _context.Chats.UpdateOneAsync(x => x.Id == add.ChatId,
            Builders<Chat>.Update.Set(x => x.LastMessage, new()
            {
                Author = user_id,
                Content = add.Content,
                MessageId = add.Id,
                SentAt = add.SentAt,
            })
        );

        // отправка всем в чате
        await Clients.Group(add.ChatId).SendAsync("onMessage", add);
    }

    public async Task GetMessages(string chatId)
    {
        List<Message> messages = await _context.Messages.Find(x => x.ChatId == chatId).SortBy(x => x.SentAt).Limit(50).ToListAsync();
        await Clients.Caller.SendAsync("onMessages", chatId, messages);
    }
}
