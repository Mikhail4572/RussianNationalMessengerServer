using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Driver;
using RussianNationalMessengerServer.Dtos;
using RussianNationalMessengerServer.Models;
using System.Collections.Concurrent;
using System.Security.Claims;

namespace RussianNationalMessengerServer.Services;

[Authorize]
public class RNMHub : Hub
{
    // username -> connection devices ConnectionId
    private static readonly ConcurrentDictionary<string, List<string>> _connections = [];
    private readonly MongoService _context;

    public RNMHub(MongoService context) =>
        _context = context;

    public override async Task OnConnectedAsync()
    {
        var user_id = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(user_id))
        {
            Context.Abort();
            return;
        }

        var user = await _context.Accounts.Find(x => x.Id == user_id).FirstOrDefaultAsync();

        if (user is null)
        {
            Context.Abort();
            return;
        }

        await _context.Accounts.UpdateOneAsync(x => x.Id == user.Id, Builders<Account>.Update.Set(x => x.IsOnline, true));

        _connections.AddOrUpdate(user_id,
            _ => [Context.ConnectionId],
            (_, list) =>
            {
                list.Add(Context.ConnectionId);
                return list;
            });


        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var login = _connections.FirstOrDefault(x => x.Value.Contains(Context.ConnectionId)).Key;

        if (string.IsNullOrEmpty(login))
            return;

        if (_connections.TryGetValue(login, out var list))
        {
            list.Remove(Context.ConnectionId);

            if (list.Count == 0)
            {
                _connections.TryRemove(login, out _);

                //уведомить остальных что пользователь вышел из сети
                await _context.Accounts.UpdateOneAsync(x => x.Id == login, Builders<Account>.Update.Set(x => x.IsOnline, false));
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task RemoveMessage(string messageId, string chatId)
    {
        if (!_context.Messages.Find(x => x.Id == messageId).Any())
            return;

        await _context.Messages.DeleteOneAsync(x => x.Id == messageId);

        var chat = await _context.Chats.Find(x => x.Id == chatId).FirstOrDefaultAsync();

        if (chat is null)
            return;

        var lastMessage = await _context.Messages.Find(x => x.ChatId == chatId).SortByDescending(x => x.SentAt).FirstOrDefaultAsync();

        LastMessage? message = lastMessage is null ? null : new()
        {
            MessageId = lastMessage.Id,
            Content = lastMessage.Content,
            Author = lastMessage.Author,
            SentAt = lastMessage.SentAt
        };

        await _context.Chats.UpdateOneAsync(x => x.Id == chatId, Builders<Chat>.Update.Set(x => x.LastMessage, message));

        await Clients.Group(chat.Id).SendAsync("onRemoveMessage", chat.Id, messageId);
    }

    public async Task GetChats()
    {
        var user_id = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(user_id))
        {
            await Clients.Client(Context.ConnectionId).SendAsync("onError", new ResponseDto()
            {
                Message = "вы не авторизированны",
                Type = TypeMessage.Error
            });

            Context.Abort();
            return;
        }

        var userChats = await _context.Chats.Find(x => x.Members.Contains(user_id)).ToListAsync();

        foreach (var chat in userChats)
            await Groups.AddToGroupAsync(Context.ConnectionId, chat.Id);

        await Clients.Caller.SendAsync("onChats", userChats);
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
