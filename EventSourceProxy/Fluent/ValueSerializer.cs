using System;

namespace EventSourceProxy.Fluent
{
    internal interface IValueSerializer { }

    internal interface IValueSerializer<in TValue> : IValueSerializer
    {
        string SerializeValue(TValue value);
    }

    internal class FunctionValueSerializer<TValue> : IValueSerializer<TValue>
    {
        private readonly Func<TValue, string> _func;

        public FunctionValueSerializer(Func<TValue, string> func)
        {
            _func = func;
        }

        public string SerializeValue(TValue value)
        {
            return _func(value);
        }
    }

    internal class DefaultValueSerializer<TValue> : IValueSerializer<TValue>
    {
        public string SerializeValue(TValue value)
        {
            throw new NotImplementedException();
        }
    }
}
