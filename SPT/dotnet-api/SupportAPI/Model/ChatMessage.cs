using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class ChatMessage
{
    [BsonId]
    public ObjectId Id { get; set; }

    public string UserEmail { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Reply { get; set; } = string.Empty;
    public string Sentiment { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}