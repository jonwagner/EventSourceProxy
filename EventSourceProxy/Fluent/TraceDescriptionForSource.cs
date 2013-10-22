using System;
using System.Linq.Expressions;

namespace EventSourceProxy.Fluent
{


    public interface ITraceDescriptionForSource<TSource> : ITraceDescription
    {
    }

    public interface ITraceDescriptionForSourceWithParam<TSource, TParam> : ITraceDescriptionForSource<TSource>
    {
        ITraceDescriptionForSourceWithParamValue<TSource, TParam, TValue> Trace<TValue>(Expression<Func<TParam, TValue>> expression);
    }

    internal class TraceDescriptionForSourceWithParam<TSource, TParam> : ITraceDescriptionForSourceWithParam<TSource, TParam>
    {
        public ITraceDescriptionForSourceWithParamValue<TSource, TParam, TValue> Trace<TValue>(Expression<Func<TParam, TValue>> expression)
        {
            return new TraceDescriptionForSourceWithParamValue<TSource, TParam, TValue>(expression);
        }
    }

    public interface ITraceDescriptionForSourceWithMethod<TSource> : ITraceDescriptionForSource<TSource>
    {
        ITraceDescriptionForSourceWithMethodParam<TSource, TParam> With<TParam>();
    }

    internal class TraceDescriptionForSourceWithMethod<TSource> : ITraceDescriptionForSourceWithMethod<TSource>
    {
        public TraceDescriptionForSourceWithMethod(Expression<Action<TSource>> method)
        {
            Method = method;
        }

        public ITraceDescriptionForSourceWithMethodParam<TSource, TParam> With<TParam>()
        {
            return new TraceDescriptionForSourceWithMethodParam<TSource, TParam>(Method);
        }

        public Expression<Action<TSource>> Method { get; private set; }
    }

    public interface ITraceDescriptionForSourceWithMethodParam<TSource, TParam> : ITraceDescriptionForSourceWithParam<TSource, TParam>
    {
    }

    internal class TraceDescriptionForSourceWithMethodParam<TSource, TParam> : ITraceDescriptionForSourceWithMethodParam<TSource, TParam>
    {
        public TraceDescriptionForSourceWithMethodParam(Expression<Action<TSource>> method)
        {
            Method = method;
        }

        public ITraceDescriptionForSourceWithParamValue<TSource, TParam, TTrace> Trace<TTrace>(Expression<Func<TParam, TTrace>> expression)
        {
            return new TraceDescriptionForSourceWithMethodParamValue<TSource, TParam, TTrace>(Method, expression);
        }

        public Expression<Action<TSource>> Method { get; private set; }
    }

    public interface ITraceDescriptionForSourceWithParamValue<TSource, TParam, TValue> : ITraceDescriptionForSourceWithParam<TSource, TParam>
    {
        ITraceDescriptionForSourceWithParamAliasedValue<TSource, TParam, TValue> As(string alias);

        ITraceDescriptionForSourceWithParamValueSerializer<TSource, TParam, TValue> Using(Func<TValue, string> serializer);
    }

    public interface ITraceDescriptionForSourceWithParamValueSerializer<TSource, TParam, TValue> : ITraceDescriptionForSourceWithParam<TSource, TParam>
    {
        ITraceDescriptionForSourceWithParamAliasedValue<TSource, TParam, TValue> As(string alias);
    }
}
