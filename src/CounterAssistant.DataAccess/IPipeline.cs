using System;
using System.Reactive.Subjects;

namespace CounterAssistant.DataAccess
{
    public interface IPipeline<T> : IDisposable
        where T: class
    {
        void Recieve(T message);
        IObservable<T> GetStream();
    }

    public class ReactivePipeline<T> : IPipeline<T> where T : class
    {
        private readonly Subject<T> _subject = new Subject<T>();

        public IObservable<T> GetStream()
        {
            return _subject;
        }

        public void Recieve(T message)
        {
            _subject.OnNext(message);
        }

        public void Dispose()
        {
            if (!_subject?.IsDisposed ?? false) _subject?.Dispose();
        }
    }
}
