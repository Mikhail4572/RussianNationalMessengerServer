using Amazon.S3;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Driver;
using RussianNationalMessengerServer.Dtos;
using RussianNationalMessengerServer.Models;
using RussianNationalMessengerServer.Services.AmazonService;
using System.Collections.Concurrent;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace RussianNationalMessengerServer.Services.ChatHub;


[Authorize]
public partial class RNMHub : Hub
{
    // username -> connection devices ConnectionId
    private static readonly ConcurrentDictionary<string, List<string>> _connections = [];
    private readonly MongoService _context;
    private readonly IFileServices _fileServices;
    public RNMHub(MongoService context, IFileServices fileServices)
    {
        _fileServices = fileServices;
        _context = context;
    }

    public override async Task OnConnectedAsync()
    {
        var user_id = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(user_id))
        {
            Context.Abort();
            return;
        }

        var user = await _context.Accounts.Find(x => x.Username == user_id).FirstOrDefaultAsync();

        if (user is null)
        {
            Context.Abort();
            return;
        }

        await _context.Accounts.UpdateOneAsync(x => x.Username == user.Username, Builders<Account>.Update.Set(x => x.IsOnline, true));

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
                await _context.Accounts.UpdateOneAsync(x => x.Username == login, Builders<Account>.Update.Set(x => x.IsOnline, false));
            }
        }

        await base.OnDisconnectedAsync(exception);
    }
}
