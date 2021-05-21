using CounterAssistant.Domain.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace CounterAssistant.DataAccess.DTO
{
    public class CounterDto
    {
        [BsonId]
        [BsonGuidRepresentation(GuidRepresentation.Unspecified)]
        public Guid Id { get; set; }
        public string Title { get; set; }
        public ushort Step { get; set; }
        public int Amount { get; set; }
        public int UserId { get; set; }
        public DateTime Created { get; set; }
        public DateTime? LastModified { get; set; }

        public Counter ToDomain() => new Counter(Id, Title, Amount, Step, Created, LastModified);

        public static CounterDto FromDomain(Counter counter, int userId)
        {
            return new CounterDto
            {
                Id = counter.Id,
                Amount = counter.Amount,
                Created = counter.Created,
                LastModified = counter.LastModified,
                Step = counter.Step,
                Title = counter.Title,
                UserId = userId
            };
        }
    }
}
