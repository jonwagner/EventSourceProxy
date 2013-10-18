using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace EventSourceProxy
{
    public interface IEventDataProvider<T>
    {
        IEnumerable<Tuple<string, object>> GetPayloadFrom(T source);

        IEnumerable<Tuple<string, Type>> Schema { get; }
    }

    public interface IEventValueProvider<in TSource>
    {
        string Name { get; }

        Type Type { get; }

        object GetValue(TSource source);
    }

    internal class InspectingValueProvider<TSource, TValue> : IEventValueProvider<TSource>
    {
        private readonly Func<TSource, TValue> _func;

        public InspectingValueProvider(Expression<Func<TSource, TValue>> expression)
        {
            Name = expression.GetMemberInfo().Name;
            Type = typeof(TValue);

            _func = expression.Compile();
        }

        public string Name { get; private set; }

        public Type Type { get; private set; }

        public object GetValue(TSource source)
        {
            return _func(source);
        }
    }

    public class EventDataProvider<TSource> : IEventDataProvider<TSource>
    {
        private readonly IEventValueProvider<TSource>[] _valueProviders;

        /// <summary>
        /// Constructs a <see cref="IEventValueProvider{T}"/> for the <see cref="TSource"/> which uses the
        /// specified <see cref="Expression{T}"/> of <see cref="Func{T,T}"/> to return a schema/payload information
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="expression"></param>
        /// <returns></returns>
        public static IEventValueProvider<TSource> Inspect<TValue>(Expression<Func<TSource, TValue>> expression)
        {
            return new InspectingValueProvider<TSource, TValue>(expression);
        }

        /// <summary>
        /// Instantiates a new InspectingEventDataProvider that uses the specified <s
        /// </summary>
        /// <param name="valueProviders"></param>
        public EventDataProvider(params IEventValueProvider<TSource>[] valueProviders)
        {
            _valueProviders = valueProviders;
        }

        /// <summary>
        /// Extracts payload values from the specified <typeparamref name="TSource"/> value
        /// </summary>
        /// <param name="source"></param>
        /// <returns>An unmaterlizard <see cref="IEnumerable{T}"/> of <see cref="Tuple{T,T}"/></returns>
        /// <remarks>
        /// This method deliberately returns an unmaterialized LINQ expression to ensure minimal overhead while
        /// tracing is not enabled for a type
        /// </remarks>
        public IEnumerable<Tuple<string, object>> GetPayloadFrom(TSource source)
        {
            return _valueProviders.Select(provider => Tuple.Create(provider.Name, provider.GetValue(source)));
        }

        /// <summary>
        /// Returns schema information for the <see cref="EventDataProvider{T}"/>
        /// </summary>
        /// <returns>An unmaterialized <see cref="IEnumerable{T}"/> of <see cref="Tuple{T,T}"/></returns>
        /// <remarks>
        /// This method deliberately returns an unmaterialized LINQ expression to ensure minimal overhead while
        /// tracing is not enabled for a type
        /// </remarks>
        public IEnumerable<Tuple<string, Type>> Schema
        {
            get { return _valueProviders.Select(provider => Tuple.Create(provider.Name, provider.Type)); }
        }
    }
}
