using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace SupportAPI.Models
{
    public class Ticket
    {
        [BsonId]
        public ObjectId Id { get; set; }  

        public string UserEmail { get; set; }
        public string Message { get; set; }
        public string Category { get; set; }
        public string Priority { get; set; }
        public string Status { get; set; } = "Open";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}