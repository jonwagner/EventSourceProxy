using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace EventSourceProxy.Fluent
{
    internal interface ITraceDescriptor
    {
        Type Source { get; }

        Type Param { get; }

        Expression Method { get; }

        IEnumerable<ITraceValue> Values { get; }
    }

    internal class TraceDescriptionForParamWithValue<TParam, TValue> : ITraceDescriptor, ITraceDescriptionForParamWithValue<TParam, TValue>, ITraceDescriptionForParamWithAliasedValue<TParam, TValue>, ITraceDescriptionForParamWithValueSerializer<TParam, TValue>
    {
        private readonly IEnumerable<ITraceValue> _expressions;

        public TraceDescriptionForParamWithValue(Expression<Func<TParam, TValue>> expression, IEnumerable<ITraceValue> expressions = null)
        {
            Expression = expression;
            Alias = expression.GetMemberInfo().Name;
            Serializer = new DefaultValueSerializer<TValue>();

            _expressions = (expressions ?? Enumerable.Empty<ITraceValue>()).ToArray();
        }

        Type ITraceDescriptor.Source
        {
            get { return null; }
        }

        Type ITraceDescriptor.Param
        {
            get { return typeof(TParam); }
        }

        Expression ITraceDescriptor.Method
        {
            get { return null; }
        }

        IEnumerable<ITraceValue> ITraceDescriptor.Values
        {
            get { return Expressions.ToArray(); }
        }

        public ITraceDescriptionForParamWithValue<TParam, TTrace> Trace<TTrace>(Expression<Func<TParam, TTrace>> expression)
        {
            return new TraceDescriptionForParamWithValue<TParam, TTrace>(expression, Expressions);
        }

        public ITraceDescriptionForParamWithAliasedValue<TParam, TValue> As(string alias)
        {
            Alias = alias;

            return this;
        }

        public ITraceDescriptionForParamWithValueSerializer<TParam, TValue> Using(Func<TValue, string> serializer)
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

    internal class TraceDescriptionForSourceWithParamValue<TSource, TParam, TValue> : ITraceDescriptor, ITraceDescriptionForSourceWithParamValue<TSource, TParam, TValue>, ITraceDescriptionForSourceWithParamAliasedValue<TSource, TParam, TValue>, ITraceDescriptionForSourceWithParamValueSerializer<TSource, TParam, TValue>
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

        Expression ITraceDescriptor.Method
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

        public ITraceDescriptionForSourceWithParamAliasedValue<TSource, TParam, TValue> As(string alias)
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

    internal class TraceDescriptionForSourceWithMethodParamValue<TSource, TParam, TValue> : ITraceDescriptor, ITraceDescriptionForSourceWithParamValue<TSource, TParam, TValue>, ITraceDescriptionForSourceWithParamAliasedValue<TSource, TParam, TValue>, ITraceDescriptionForSourceWithParamValueSerializer<TSource, TParam, TValue>
    {
        private readonly IEnumerable<ITraceValue> _expressions;

        public TraceDescriptionForSourceWithMethodParamValue(Expression<Action<TSource>> method, Expression<Func<TParam, TValue>> expression, IEnumerable<ITraceValue> expressions = null)
        {
            Method = method;
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

        Expression ITraceDescriptor.Method
        {
            get { return Method; }
        }

        IEnumerable<ITraceValue> ITraceDescriptor.Values
        {
            get { return Expressions.ToArray(); }
        }

        public ITraceDescriptionForSourceWithParamValue<TSource, TParam, TTrace> Trace<TTrace>(Expression<Func<TParam, TTrace>> expression)
        {
            return new TraceDescriptionForSourceWithParamValue<TSource, TParam, TTrace>(expression, Expressions);
        }

        public ITraceDescriptionForSourceWithParamAliasedValue<TSource, TParam, TValue> As(string alias)
        {
            Alias = alias;

            return this;
        }

        public ITraceDescriptionForSourceWithParamValueSerializer<TSource, TParam, TValue> Using(Func<TValue, string> serializer)
        {
            Serializer = new FunctionValueSerializer<TValue>(serializer);

            return this;
        }

        public Expression<Action<TSource>> Method { get; private set; }

        public string Alias { get; private set; }

        public Expression<Func<TParam, TValue>> Expression { get; private set; }

        public IValueSerializer<TValue> Serializer { get; private set; }

        public IEnumerable<ITraceValue> Expressions
        {
            get { return _expressions.Concat(new[] { new TraceValue<TValue>(Expression, Alias, Serializer) }); }
        }
    }
}
