using CounterAssistant.Domain.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace CounterAssistant.DataAccess.DTO
{
    public class CounterDto
    {
        [BsonId]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid Id { get; set; }
        public string Title { get; set; }
        public ushort Step { get; set; }
        public int Amount { get; set; }
        public int UserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastModifiedAt { get; set; }
        public bool IsManual { get; set; }
        public string Unit { get; set; }

        public Counter ToDomain() => new Counter(Id, Title, Amount, Step, CreatedAt, LastModifiedAt, IsManual, Enum.TryParse<CounterUnit>(Unit, ignoreCase: true, out var unit) ? unit : CounterUnit.Time);

        public static CounterDto FromDomain(Counter counter, int userId)
        {
            return new CounterDto
            {
                Id = counter.Id,
                Amount = counter.Amount,
                CreatedAt = counter.CreatedAt,
                LastModifiedAt = counter.LastModifiedAt,
                Step = counter.Step,
                Title = counter.Title,
                UserId = userId,
                IsManual = counter.IsManual,
                Unit = counter.Unit.ToString()
            };
        }
    }
}
