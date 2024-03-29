﻿using System;

namespace CounterAssistant.Domain.Models
{
    public class Counter
    {
        private readonly Guid _id;
        private readonly DateTime _createdAt;
        private readonly bool _isManual;
        private readonly CounterUnit _unit;
        private readonly ushort _step;

        private string _title;
        private int _amount;
        private DateTime? _lastModifiedAt;

        public Counter(Guid id, string title, int amount, ushort step, DateTime createdAt, DateTime? lastModifiedAt, bool isManual, CounterUnit unit)
        {
            _id = id;
            _title = title;
            _amount = amount;
            _step = step;
            _createdAt = createdAt;
            _lastModifiedAt = createdAt == lastModifiedAt ? null : lastModifiedAt;
            _isManual = isManual;
            _unit = unit;
        }

        public Counter(string title, int amount, ushort step, bool isManual, CounterUnit unit)
        {
            _id = Guid.NewGuid();
            _createdAt = DateTime.UtcNow;
            _title = title;
            _amount = amount;
            _step = step;
            _isManual = isManual;
            _unit = unit;
        }

        public Guid Id => _id;
        public string Title => _title;
        public int Amount => _amount;
        public ushort Step => _step;
        public DateTime CreatedAt => _createdAt;
        public DateTime LastModifiedAt => _lastModifiedAt.HasValue ? _lastModifiedAt.Value : _createdAt;
        public bool IsManual => _isManual;
        public CounterUnit Unit => _unit;

        public void Increment()
        {
            _amount += _step;
            _lastModifiedAt = DateTime.UtcNow;
        }

        public void Decrement()
        {
            _amount -= _step;
            _lastModifiedAt = DateTime.UtcNow;
        }

        public void Rename(string title)
        {
            _title = title;
        }

        public void Reset()
        {
            _amount = 0;
            _lastModifiedAt = DateTime.UtcNow;
        }
    }

    public enum CounterUnit
    {
        Day = 1,
        Week,
        Lesson,
        Time,
        Training,
        Evening,
        Morning
    }
}
