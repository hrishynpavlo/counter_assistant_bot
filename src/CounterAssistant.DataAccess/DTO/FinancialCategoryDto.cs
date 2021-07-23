using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

namespace CounterAssistant.DataAccess.DTO
{
    public class FinancialCategoryDto
    {
        [BsonId]
        public ObjectId Id { get; set; }

        public string Name { get; set; }

        public HashSet<string> Sellers { get; set; }
    }
}
