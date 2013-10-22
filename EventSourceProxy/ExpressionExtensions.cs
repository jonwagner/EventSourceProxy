using System;
using System.Linq.Expressions;
using System.Reflection;

namespace EventSourceProxy
{

    /// <summary>
    /// Extension methods for <see cref="Expression"/> related classes
    /// </summary>
    public static class ExpressionExtensions
    {
        private static MemberExpression GetMemberExpression(this Expression expression)
        {
            var lambda = (LambdaExpression)expression;

            MemberExpression memberExpression;
            if (lambda.Body is UnaryExpression)
            {
                var unaryExpression = (UnaryExpression)lambda.Body;
                memberExpression = (MemberExpression)unaryExpression.Operand;
            }
            else
            {
                memberExpression = lambda.Body as MemberExpression;
            }

            return memberExpression;
        }

        private static UnaryExpression GetUnaryExpression<TValue>(Expression expression)
        {
            var lambda = (LambdaExpression)expression;

            if (lambda.Body is UnaryExpression)
            {
                return (UnaryExpression)lambda.Body;
            }
            else
            {
                MemberExpression memberExpression = GetMemberExpression(expression);

                return Expression.Convert(memberExpression, typeof(TValue));
            }
        }

        /// <summary>
        /// Gets a <see cref="MemberInfo"/> from the specified <see cref="Expression"/>
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public static MemberInfo GetMemberInfo(this Expression expression)
        {
            LambdaExpression lambda = (LambdaExpression)expression;

            MemberExpression memberExpression = GetMemberExpression(lambda);
            if (memberExpression != null)
                return memberExpression.Member;

            MethodCallExpression methodCallExpression = lambda.Body as MethodCallExpression;
            if (methodCallExpression != null)
                return methodCallExpression.Method;

            return null;

        }

        /// <summary>
        /// Gets a <see cref="MemberInfo"/> from the specified <see cref="Expression"/>
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public static TValue GetMemberValue<TValue>(this Expression expression)
        {
            UnaryExpression unaryExpression = GetUnaryExpression<TValue>(expression);

            LambdaExpression lamdaExpression = Expression.Lambda<Func<TValue>>(unaryExpression);

            var getter = lamdaExpression.Compile();

            return (TValue)getter.DynamicInvoke();
        }
    }
}
