using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace CounterAssistant.DataAccess.DTO
{
    public class FinancialTransaction
    {
        [BsonId]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid Id { get; set; }

        public decimal Amount { get; set; }

        public DateTime Date { get; set; }

        public string Category { get; set; }

        public string Title { get; set; }
        
        public string Comments { get; set; }
    }
}
