using RussianNationalMessengerServer.Models;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using MongoDB.Driver;


namespace RussianNationalMessengerServer.Services.ChatHub;

public partial class RNMHub
{
    public async Task GetUsersByName(string name)
    {
        // .Project(x => x.Id) = List<T>.Select(x => x.Id)

        var username = Context.User?.FindFirst(ClaimTypes.Name)?.Value;

        if (string.IsNullOrEmpty(username))
            return;

        List<Account> users = await _context.Accounts.Find(x => x.Username.Contains(name, StringComparison.CurrentCultureIgnoreCase)
            && x.Username != username).Limit(20).ToListAsync();

        await Clients.Caller.SendAsync("onSearchUsers", users);
    }
}
