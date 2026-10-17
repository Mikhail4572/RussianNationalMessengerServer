using Microsoft.AspNetCore.SignalR;
using MongoDB.Driver;
using RussianNationalMessengerServer.Dtos;
using RussianNationalMessengerServer.Models;
using System.Security.Claims;

namespace RussianNationalMessengerServer.Services.ChatHub;

public partial class RNMHub
{
    public async Task DeleteChat(string chatId)
    {
        if (await _context.Chats.Find(x => x.Id == chatId).FirstOrDefaultAsync() is not Chat chat)
            return;

        await _context.Messages.DeleteManyAsync(x => x.ChatId == chatId);

        await _context.Chats.DeleteOneAsync(x => x.Id == chatId);

        var onlineMembersChatConectIds = _connections.Where(x => chat.Members.Contains(x.Key)).SelectMany(x => x.Value).ToList();

        await Clients.Clients(onlineMembersChatConectIds).SendAsync("onDeleteChat", chat.Id);
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

    public async Task CreateChat(Message firstMessage, string groupName, string[] members)
    {
        var user_id = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(user_id))
            return;

        if (firstMessage is null)
            return;

        members = [.. members.Distinct()];
        firstMessage.Id = Guid.NewGuid().ToString();
        firstMessage.SentAt = DateTime.UtcNow;

        var count = await _context.Accounts.CountDocumentsAsync(x => members.Contains(x.Username));

        if (count != members.Length)
            return;

        //if (_context.Accounts.Find(x => members.Contains(x.Username)).Count() != members.Length)
        //    return;

        Chat chat = new()
        {
            Id = Guid.NewGuid().ToString(),
            Name = (groupName == "") ? null : groupName,
            IsGroup = !string.IsNullOrEmpty(groupName),
            CreatedAt = DateTime.UtcNow,
            Members = [.. members],
            LastMessage = new()
            {
                MessageId = firstMessage.Id,
                Author = user_id,
                Content = firstMessage.Content,
                SentAt = firstMessage.SentAt
            }
        };

        await _context.Chats.InsertOneAsync(chat);

        await _context.Messages.InsertOneAsync(new()
        {
            Id = firstMessage.Id,
            Author = user_id,
            Content = firstMessage.Content,
            ChatId = chat.Id,
            SentAt = firstMessage.SentAt,
            IsDeleted = false,
            IsEdited = false
        });

        // получаем учасников чата, которые сейчас онлайн
        var onlineMembersChatConectIds = _connections.Where(x => members.Contains(x.Key)).SelectMany(x => x.Value).ToList();

        // добавляем пользователей в чат
        var tasksAddGroup = onlineMembersChatConectIds.Select(async x => await Groups.AddToGroupAsync(x, chat.Id)).ToList();
        await Task.WhenAll(tasksAddGroup);

        onlineMembersChatConectIds.Remove(Context.ConnectionId);

        // отправляем чат всем его участникам кроме вызвавшего создание
        await Clients.Clients(onlineMembersChatConectIds).SendAsync("onChats", new List<Chat> { chat });

        var oldChatId = firstMessage.ChatId;
        firstMessage.ChatId = chat.Id;

        // отправляем создателю чата, chatId который дал клиент, сам чат и последнее сообщение в нём
        await Clients.Client(Context.ConnectionId).SendAsync("onCreateChat", chat, firstMessage);
    }

}
