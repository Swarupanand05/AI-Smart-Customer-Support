using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class ChatMessage
{
    [BsonId]
    public ObjectId Id { get; set; }

    public string UserEmail { get; set; }
    public string Message { get; set; }
    public string Reply { get; set; }
    public string Sentiment { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}