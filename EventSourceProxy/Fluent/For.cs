using System;
using System.Linq.Expressions;

namespace EventSourceProxy.Fluent
{
    public static class For<TSource>
    {
        public static ITraceDescriptionForSourceWithParam<TSource, TParam> With<TParam>()
        {
            return new TraceDescriptionForSourceWithParam<TSource, TParam>();
        }

        public static ITraceDescriptionForSourceWithMethod<TSource> Method(Expression<Action<TSource>> methodExpression)
        {
            throw new NotImplementedException();
        }
    }
}
