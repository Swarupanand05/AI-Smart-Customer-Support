using MongoDB.Bson;
using MongoDB.Driver;
using SupportAPI.Models;

namespace SupportAPI.Services
{
    public class MongoService
    {
        private readonly IMongoCollection<ChatMessage> _chats;
        private readonly IMongoCollection<Ticket> _tickets;

        public MongoService(IConfiguration config)
        {
            var client = new MongoClient(config["MongoDB:ConnectionString"]);
            var database = client.GetDatabase(config["MongoDB:DatabaseName"]);

            _tickets = database.GetCollection<Ticket>("Tickets");
            _chats = database.GetCollection<ChatMessage>("Chats"); // ✅ ADD THIS
        }

        public async Task CreateTicket(Ticket ticket)
        {
            await _tickets.InsertOneAsync(ticket);
        }
        public async Task SaveChat(ChatMessage chat)
        {
            await _chats.InsertOneAsync(chat);
        }

        public async Task<List<Ticket>> GetTickets()
        {
            return await _tickets.Find(_ => true).ToListAsync();
        }

        public async Task UpdateStatus(string id, string status)
        {
            var filter = Builders<Ticket>.Filter.Eq(t => t.Id, new ObjectId(id));
            var update = Builders<Ticket>.Update.Set(t => t.Status, status);

            await _tickets.UpdateOneAsync(filter, update);
        }
    }
}