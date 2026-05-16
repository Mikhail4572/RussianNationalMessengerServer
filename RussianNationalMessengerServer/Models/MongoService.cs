using MongoDB.Driver;

namespace RussianNationalMessengerServer.Models;

public class MongoService
{
    public MongoService(IConfiguration config)
    {
        MongoClient client = new(config["MongoDbSettings:ConnectionString"]);

        var db = client.GetDatabase("RussianNationalMessangerDB");

        Messages = db.GetCollection<Message>("Message");
        Chats = db.GetCollection<Chat>("Chat");
        Accounts = db.GetCollection<Account>("Account");
    }

    public IMongoCollection<Message> Messages { get; }
    public IMongoCollection<Chat> Chats { get; }
    public IMongoCollection<Account> Accounts { get; }

}
    /*
    // messages
    public Task InsertMessage(Message msg)
        => _messages.InsertOneAsync(msg);

    public Task<List<Message>> GetMessages(string chatId)
        => _messages.Find(x => x.ChatId == chatId)
                    .SortBy(x => x.SentAt).ToListAsync();

    // chats
    public Task<List<Chat>> GetUserChats(string userId)
        => _chats.Find(x => x.Members.Contains(userId))
                 .ToListAsync();
    

    public Task UpdateChat(Chat chat)
        => _chats.ReplaceOneAsync(x => x.Id == chat.Id, chat);
    */


