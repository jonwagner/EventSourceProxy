using System;
using System.Linq.Expressions;

namespace EventSourceProxy.Fluent
{
    internal interface ITraceValue
    {
        Type Type { get; }
        Expression Expression { get; }
        string Alias { get; }
        IValueSerializer Serializer { get; }
    }

    internal class TraceValue<TValue> : ITraceValue
    {
        public TraceValue(Expression expression, string alias, IValueSerializer serializer)
        {
            Expression = expression;
            Alias = alias;
            Serializer = serializer;
        }

        public Type Type
        {
            get { return typeof(TValue); }
        }

        public Expression Expression { get; private set; }

        public string Alias { get; private set; }

        public IValueSerializer Serializer { get; private set; }
    }
}
