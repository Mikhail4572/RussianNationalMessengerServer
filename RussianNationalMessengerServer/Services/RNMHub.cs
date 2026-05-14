using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RussianNationalMessengerServer.Dtos;
using RussianNationalMessengerServer.Models;
using System.Collections.Concurrent;
using System.Security.Claims;

namespace RussianNationalMessengerServer.Services;

[Authorize]
public class RNMHub : Hub
{
    private readonly IDbContextFactory<RNMContext> _dbFactory;

    // username -> connection devices ConnectionId
    private static readonly ConcurrentDictionary<string, List<string>> _connections = [];

    public RNMHub(IDbContextFactory<RNMContext> dbFactory) =>
        _dbFactory = dbFactory;

    public override async Task OnConnectedAsync()
    {
        // Получаем UserId из JWT
        var username = Context.User?.FindFirst(ClaimTypes.Name)?.Value;

        if (string.IsNullOrEmpty(username))
        {
            Context.Abort();
            return;
        }

        await using var dbContext = await _dbFactory.CreateDbContextAsync();

        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Username == username);

        if (user == null)
        {
            Context.Abort();
            return;
        }

        user.IsOnline = true;
        await dbContext.SaveChangesAsync();

        _connections.AddOrUpdate(username,
            _ => [Context.ConnectionId],
            (_, list) =>
            {
                list.Add(Context.ConnectionId);
                return list;
            });


        var userChatMembers = await dbContext.ChatMembers.Where(x => x.Username == user.Username).ToListAsync();

        foreach (var chat in userChatMembers)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, chat.ChatId.ToString());
        }

        var userChats = await dbContext.Chats.Where(x => x.ChatMembers.Any(y => y.Username == username)).ToListAsync();
        List<ChatMessagesDto> chatMessagesDto = [];

        foreach (var item in userChats)
        {
            chatMessagesDto.Add(new()
            {
                Chat = item,
                Messages = [.. dbContext.Messages.Where(x => x.ChatId == item.Id).OrderByDescending(x => x.SentAt)]//Take(30)
            });
        }

        if (chatMessagesDto.Count > 0)
        {
            await Clients.Client(Context.ConnectionId).SendAsync("OnChatMessages", chatMessagesDto);
        }

        await base.OnConnectedAsync();
    }

    public async Task SendMessage(Guid chatId, string content)
    {
        var username = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(username))
            return;

        await using var db = await _dbFactory.CreateDbContextAsync();

        // Проверка: пользователь в чате?
        var isMember = await db.ChatMembers
            .AnyAsync(x => x.ChatId == chatId && x.Username == username);

        // написать отправку сообщения
        // что юзер не имеет доступа к этому чату
        if (!isMember)
            return;

        Message message = new()
        {
            ChatId = chatId,
            Author = username,
            Content = content
        };

        db.Messages.Add(message);
        await db.SaveChangesAsync();

     /*   var dto = new
        {
            Id = message.Id,
            ChatId = chatId,
            Author = username,
            Content = content,
            SentAt = message.SentAt
        };
     */
        // отправка ВСЕМ в чате
        await Clients.Group(chatId.ToString()).SendAsync("ReceiveMessage", message);
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
                await using var db = await _dbFactory.CreateDbContextAsync();
                var disconn_user = db.Users.FirstOrDefault(x => x.Username == login);
                
                if (disconn_user != null)
                {
                    disconn_user.IsOnline = false;
                    //уведомить остальных что пользователь вышел из сети)
                    await db.SaveChangesAsync();
                }
            }
        }

        await base.OnDisconnectedAsync(exception);
    }
}
