namespace chARpack
{
    public class ThreadSafe<T>
    {
        private readonly object _locker = new object();
        private T _value;

        public ThreadSafe() { }

        public ThreadSafe(T initialValue)
        {
            _value = initialValue;
        }

        public T Value
        {
            get
            {
                lock (_locker)
                {
                    return _value;
                }
            }
            set
            {
                lock (_locker)
                {
                    _value = value;
                }
            }
        }

        public override string ToString()
        {
            return Value?.ToString() ?? "null";
        }
    }
}