using Microsoft.AspNetCore.SignalR;
using MongoDB.Bson;
using MongoDB.Driver;
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

    public async Task SendMessage(Message message)
    {
        var user_id = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(user_id))
            return;

        // Проверка: пользователь в чате?
        var isMember = await _context.Chats.Find(x => x.Id == message.ChatId && x.Members.Contains(user_id)).AnyAsync();

        if (!isMember)
            return;

        message.Id = Guid.NewGuid().ToString();
        message.ChatId = message.ChatId;
        message.Author = user_id;
        message.SentAt = DateTime.UtcNow;
        message.IsDeleted = false;
        message.IsEdited = false;

        await _context.Messages.InsertOneAsync(message);

        await _context.Chats.UpdateOneAsync(x => x.Id == message.ChatId,
            Builders<Chat>.Update.Set(x => x.LastMessage, new()
            {
                Author = user_id,
                Content = message.Content,
                MessageId = message.Id,
                SentAt = message.SentAt,
            })
        );

        // отправка всем в чате
        await Clients.Group(message.ChatId).SendAsync("onMessage", message);
    }


    public async Task GetMessages(string chatId)
    {
        List<Message> messages = await _context.Messages.Find(x => x.ChatId == chatId).SortBy(x => x.SentAt).Limit(50).ToListAsync();
        await Clients.Caller.SendAsync("onMessages", chatId, messages);
    }
}
