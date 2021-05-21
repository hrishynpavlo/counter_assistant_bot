using System;

namespace CounterAssistant.Domain.Models
{
    public class Counter
    {
        private readonly Guid _id;
        private readonly DateTime _created;

        private string _title;
        private int _amount;
        private ushort _step;
        private DateTime? _lastModified;
        private bool _isManual;


        public Counter(Guid id, string title, int amount, ushort step, DateTime created, DateTime? lastModified)
        {
            _id = id;
            _title = title;
            _amount = amount;
            _step = step;
            _created = created;
            _lastModified = lastModified;
        }

        public Counter(string title, int amount, ushort step, bool isManual)
        {
            _id = Guid.NewGuid();
            _created = DateTime.UtcNow;
            _title = title;
            _amount = amount;
            _step = step;
            _isManual = isManual;
        }

        public Guid Id => _id;
        public string Title => _title;
        public int Amount => _amount;
        public ushort Step => _step;
        public DateTime Created => _created;
        public DateTime? LastModified => _lastModified;

        public void Increment()
        {
            _amount += _step;
            _lastModified = DateTime.UtcNow;
        }

        public void Decrement()
        {
            _amount -= _step;
            _lastModified = DateTime.UtcNow;
        }

        public void Rename(string title)
        {
            _title = title;
        }

        public void Reset()
        {
            _amount = 0;
        }
    }
}
