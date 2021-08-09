using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace CounterAssistant.DataAccess.DTO
{
    public class MonobankTransaction
    {
        [BsonId]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid Id { get; init; } = Guid.NewGuid();
        public string Type { get; set; }
        public TransactionData Data { get; set; }
    }

    public class TransactionData
    {
        public string Account { get; set; }
        public StatementItem StatementItem { get; set; }
    }

    public class StatementItem
    {
        public string Id { get; set; }
        public long Time { get; set; }
        public string Description { get; set; }
        public int Mcc { get; set; }
        public bool Hold { get; set; }
        public long Amount { get; set; }
        public long OperationAmount { get; set; }
        public int CurrencyCode { get; set; }
        public long CommissionRate { get; set; }
        public long CashbackAmount { get; set; }
        public long Balance { get; set; }
        public string Comment { get; set; }
        public string ReceiptId { get; set; }
        public string CounterEdrpou { get; set; }
        public string CounterIban { get; set; }
    }
}
