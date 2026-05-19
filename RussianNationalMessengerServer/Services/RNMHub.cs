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
        // Получаем UserId из JWT
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
               
                var disconn_user = _context.Accounts.Find(x => x.Username == login).FirstOrDefault();
                
                if (disconn_user is not null)
                {
                    disconn_user.IsOnline = false;
                    //уведомить остальных что пользователь вышел из сети)

                    await _context.Accounts.UpdateOneAsync(x => x.Id == disconn_user.Id, Builders<Account>.Update.Set(x => x.IsOnline, false));
                }
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendMessage(string chatId, string content)
    {
        var user_id = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(user_id))
            return;

        // Проверка: пользователь в чате?
        var isMember = await _context.Chats.Find(x => x.Id == chatId && x.Members.Contains(user_id)).AnyAsync();

        if (!isMember)
            return;

        Message message = new()
        {
            Id = Guid.NewGuid().ToString(),
            ChatId = chatId,
            Author = user_id,
            Content = content,
            SentAt = DateTime.UtcNow,
            IsDeleted = false,
            IsEdited = false
        };

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
        await Clients.Group(chatId).SendAsync("onMessage", message);
    }


}
