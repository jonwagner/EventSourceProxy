using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace EventSourceProxy.Fluent
{
    public interface ITraceDescription
    {
    }

    public interface ITraceDescriptionForSource<TSource> : ITraceDescription
    {
    }

    public interface ITraceDescriptionForParam<TParam> : ITraceDescription
    {
        ITraceDescriptionForParamWithValues<TParam> Trace<TTrace>(Expression<Func<TParam, TTrace>> expression);
    }

    public interface ITraceDescriptionForParamWithValues<TParam> : ITraceDescriptionForParam<TParam>
    {
        ITraceDescriptionForParamAliasedValues<TParam> As(string alias);
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
        public TraceDescriptionForSourceWithMethod(Expression<Action> method)
        {
            Method = method;
        }

        public ITraceDescriptionForSourceWithMethodParam<TSource, TParam> With<TParam>()
        {
            throw new NotImplementedException();
        }

        public Expression<Action> Method { get; private set; }
    }

    public interface ITraceDescriptionForSourceWithMethodParam<TSource, TParam> : ITraceDescriptionForSourceWithMethod<TSource>
    {
        ITraceDescriptionForSourceWithParamValue<TSource, TParam, TTrace> Trace<TTrace>(Expression<Func<TParam, TTrace>> expression);
    }

    public interface ITraceDescriptionForSourceWithMethodValues<TSource> : ITraceDescriptionForSourceWithMethod<TSource>
    {
    }

    internal class TraceDescriptionForSourceWithMethodValues<TSource> : ITraceDescriptionForSourceWithMethodValues<TSource>
    {
        public TraceDescriptionForSourceWithMethodValues(IEnumerable<Expression> logExpressions)
        {
            LogExpressions = logExpressions;
        }

        public ITraceDescriptionForSourceWithMethodParam<TSource, TParam> With<TParam>()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Expression> LogExpressions { get; private set; }
    }

    public interface ITraceDescriptionForSourceWithParamValue<TSource, TParam, TValue> : ITraceDescriptionForSourceWithParam<TSource, TParam>
    {
        ITraceDescriptionForSourceWithParamAliasedValues<TSource, TParam, TValue> As(string alias);

        ITraceDescriptionForSourceWithParamValueSerializer<TSource, TParam, TValue> Using(Func<TValue, string> serializer);
    }

    public interface ITraceDescriptionForParamAliasedValues<TParam> : ITraceDescriptionForParam<TParam>
    {
    }

    public interface ITraceDescriptionForSourceWithParamAliasedValues<TSource, TParam, TValue> : ITraceDescriptionForSourceWithParam<TSource, TParam>
    {
    }

    public interface ITraceDescriptionForSourceWithParamValueSerializer<TSource, TParam, TValue> : ITraceDescriptionForSourceWithParam<TSource, TParam>
    {
        ITraceDescriptionForSourceWithParamAliasedValues<TSource, TParam, TValue> As(string alias);
    }

    internal interface IValueSerializer
    {

    }

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


    internal class TraceDescriptionForSourceWithParamValue<TSource, TParam, TValue> : ITraceDescriptor, ITraceDescriptionForSourceWithParamValue<TSource, TParam, TValue>, ITraceDescriptionForSourceWithParamAliasedValues<TSource, TParam, TValue>, ITraceDescriptionForSourceWithParamValueSerializer<TSource, TParam, TValue>
    {
        private readonly IEnumerable<ITraceValue> _expressions;

        public TraceDescriptionForSourceWithParamValue(Expression<Func<TParam, TValue>> expression, IEnumerable<ITraceValue> expressions = null)
        {
            Expression = expression;
            Alias = expression.GetMemberInfo().Name;
            Serializer = new DefaultValueSerializer<TValue>();

            _expressions = (expressions ?? Enumerable.Empty<ITraceValue>()).ToArray();
        }

        Type ITraceDescriptor.Source
        {
            get { return typeof(TSource); }
        }

        Type ITraceDescriptor.Param
        {
            get { return typeof(TParam); }
        }

        Expression<Action> ITraceDescriptor.Method
        {
            get { return null; }
        }

        IEnumerable<ITraceValue> ITraceDescriptor.Values
        {
            get { return Expressions.ToArray(); }
        }

        public ITraceDescriptionForSourceWithParamValue<TSource, TParam, TTrace> Trace<TTrace>(Expression<Func<TParam, TTrace>> expression)
        {
            return new TraceDescriptionForSourceWithParamValue<TSource, TParam, TTrace>(expression, Expressions);
        }

        public ITraceDescriptionForSourceWithParamAliasedValues<TSource, TParam, TValue> As(string alias)
        {
            Alias = alias;

            return this;
        }

        public ITraceDescriptionForSourceWithParamValueSerializer<TSource, TParam, TValue> Using(Func<TValue, string> serializer)
        {
            Serializer = new FunctionValueSerializer<TValue>(serializer);

            return this;
        }

        public string Alias { get; private set; }

        public Expression<Func<TParam, TValue>> Expression { get; private set; }

        public IValueSerializer<TValue> Serializer { get; private set; }

        public IEnumerable<ITraceValue> Expressions 
        {
            get { return _expressions.Concat(new[] { new TraceValue<TValue>(Expression, Alias, Serializer) }); }
        }
    }

    internal interface ITraceDescriptor
    {
        Type Source { get; }

        Type Param { get; }

        Expression<Action> Method { get; }

        IEnumerable<ITraceValue> Values { get; }
    }
}
