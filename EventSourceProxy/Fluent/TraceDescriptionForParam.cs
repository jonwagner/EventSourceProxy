using System;
using System.Linq.Expressions;

namespace EventSourceProxy.Fluent
{
    public interface ITraceDescriptionForParam<TParam> : ITraceDescription
    {
        ITraceDescriptionForParamWithValue<TParam, TTrace> Trace<TTrace>(Expression<Func<TParam, TTrace>> expression);
    }

    internal class TraceDescriptionForParam<TParam> : ITraceDescriptionForParam<TParam>
    {
        public ITraceDescriptionForParamWithValue<TParam, TTrace> Trace<TTrace>(Expression<Func<TParam, TTrace>> expression)
        {
            return new TraceDescriptionForParamWithValue<TParam, TTrace>(expression);
        }
    }

    public interface ITraceDescriptionForParamWithValue<TParam, TValue> : ITraceDescriptionForParam<TParam>
    {
        ITraceDescriptionForParamWithAliasedValue<TParam, TValue> As(string alias);

        ITraceDescriptionForParamWithValueSerializer<TParam, TValue> Using(Func<TValue, string> serializer);
    }

    public interface ITraceDescriptionForParamWithAliasedValue<TParam, TValue> : ITraceDescriptionForParam<TParam>
    {
    }

    public interface ITraceDescriptionForSourceWithParamAliasedValue<TSource, TParam, TValue> : ITraceDescriptionForSourceWithParam<TSource, TParam>
    {
    }

    public interface ITraceDescriptionForParamWithValueSerializer<TParam, TValue> : ITraceDescriptionForParam<TParam>
    {
        ITraceDescriptionForParamWithAliasedValue<TParam, TValue> As(string alias);
    }
}
