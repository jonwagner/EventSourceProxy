using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EventSourceProxy
{
	#region Public Interfaces
	/// <summary>
	/// Configures the parameters that are traced.
	/// </summary>
	/// <remarks>These methods are valid regardless of the syntax state.</remarks>
	public interface IParameterBuilderBase
	{
		/// <summary>
		/// Configures a parameter rule that matches all types and methods.
		/// </summary>
		/// <returns>A configuration rule that can be extended.</returns>
		IParameterBuilder ForAnything();

		/// <summary>
		/// Configures a parameter rule that matches the given type.
		/// </summary>
		/// <typeparam name="TSource">The type to be matched.</typeparam>
		/// <returns>A configuration rule that can be extended.</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter")]
		IParameterBuilder For<TSource>();

		/// <summary>
		/// Configures a parameter rule that matches the given type.
		/// </summary>
		/// <typeparam name="TSource">The type to be matched.</typeparam>
		/// <param name="methodExpression">A lambda expression that contains a method call to match on.</param>
		/// <returns>A configuration rule that can be extended.</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
		IParameterBuilder For<TSource>(Expression<Action<TSource>> methodExpression);

		/// <summary>
		/// Begins a block where the expressions are based on the type T.
		/// </summary>
		/// <typeparam name="T">The type to use for the expressions.</typeparam>
		/// <returns>A continuation of the configuration.</returns>
		IParameterBuilderWithType<T> With<T>();

		/// <summary>
		/// Begins a block where the expressions are based on the type T, filtered by the given parameter name.
		/// </summary>
		/// <typeparam name="T">The type to use for the expressions.</typeparam>
		/// <param name="parameterName">The parameter name to filter on.</param>
		/// <returns>A continuation of the configuration.</returns>
		IParameterBuilderWithType<T> With<T>(string parameterName);
	}

	/// <summary>
	/// Configures the parameters that are traced.
	/// </summary>
	/// <remarks>These methods are valid outside of With blocks.</remarks>
	public interface IParameterBuilder : IParameterBuilderBase
	{
		/// <summary>
		/// Trace one or more parameters by name.
		/// </summary>
		/// <param name="parameterNames">The list of parameters to trace.</param>
		/// <returns>A continuation of the configuration.</returns>
		IParameterBuilderWithParameter Trace(params string[] parameterNames);

		/// <summary>
		/// Trace an expression, filtering by parameter name, and giving an alias.
		/// </summary>
		/// <typeparam name="T">The type of the parameter.</typeparam>
		/// <typeparam name="TValue">The result of the expression.</typeparam>
		/// <param name="parameterName">The name of the parameter.</param>
		/// <param name="parameterAlias">An alias to use for the parameter.</param>
		/// <param name="accessorExpressions">One or more expressions to evaluate against the parameter.</param>
		/// <returns>A continuation of the configuration.</returns>
		IParameterBuilderWithParameter Trace<T, TValue>(string parameterName, string parameterAlias, params Expression<Func<T, TValue>>[] accessorExpressions);

		/// <summary>
		/// Trace an expression, filtering by parameter name.
		/// </summary>
		/// <typeparam name="T">The type of the parameter.</typeparam>
		/// <typeparam name="TValue">The result of the expression.</typeparam>
		/// <param name="parameterName">The name of the parameter.</param>
		/// <param name="accessorExpressions">One or more expressions to evaluate against the parameter.</param>
		/// <returns>A continuation of the configuration.</returns>
		IParameterBuilderWithParameter Trace<T, TValue>(string parameterName, params Expression<Func<T, TValue>>[] accessorExpressions);

		/// <summary>
		/// Trace an expression.
		/// </summary>
		/// <typeparam name="T">The type of the parameter.</typeparam>
		/// <typeparam name="TValue">The result of the expression.</typeparam>
		/// <param name="accessorExpressions">One or more expressions to evaluate against the parameter.</param>
		/// <returns>A continuation of the configuration.</returns>
		IParameterBuilderWithParameter Trace<T, TValue>(params Expression<Func<T, TValue>>[] accessorExpressions);

		/// <summary>
		/// Ignores one or more named parameters.
		/// </summary>
		/// <param name="parameterNames">The list of parameters to ignore.</param>
		/// <returns>A continuation of the configuration.</returns>
		IParameterBuilder Ignore(params string[] parameterNames);
		
		/// <summary>
		/// Ignores one or more parameters by type.
		/// </summary>
		/// <param name="types">The list of types to ignore.</param>
		/// <returns>A continuation of the configuration.</returns>
		IParameterBuilder Ignore(params Type[] types);

		/// <summary>
		/// Ignores a parameter by type.
		/// </summary>
		/// <typeparam name="T">The type to ignore.</typeparam>
		/// <returns>A continuation of the configuration.</returns>
		IParameterBuilder Ignore<T>();

		/// <summary>
		/// Adds context to the log by calling the given context expression.
		/// </summary>
		/// <typeparam name="T">The type returned by the context expression.</typeparam>
		/// <param name="alias">The alias to use to log the context.</param>
		/// <param name="contextExpression">The expression to generate the context.</param>
		/// <returns>A continuation of the configuration.</returns>
		IParameterBuilder AddContext<T>(string alias, Expression<Func<T>> contextExpression);

		/// <summary>
		/// Adds a context parameter to the logged method. The data is retrieved from TraceContext.GetValue.
		/// </summary>
		/// <typeparam name="T">The type returned by the context expression.</typeparam>
		/// <param name="alias">The alias to use to log the data.</param>
		/// <param name="key">The key to use to retrieve the data from TraceContext.GetValue.</param>
		/// <returns>A continuation of the configuration.</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Alias")]
		IParameterBuilder AddContextData<T>(string alias, string key = null);
	}

	/// <summary>
	/// Configures the parameters that are traced.
	/// </summary>
	/// <remarks>These methods are valid outside of With blocks, when there is at least one parameter defined.</remarks>
	public interface IParameterBuilderWithParameter : IParameterBuilder
	{
		/// <summary>
		/// Traces additional parameters by name, combining them with the previous item(s) traced.
		/// </summary>
		/// <param name="parameterNames">The list of parameters to trace.</param>
		/// <returns>A continuation of the configuration.</returns>
		IParameterBuilderWithParameter And(params string[] parameterNames);

		/// <summary>
		/// Traces additional expressions, combining them with the previous item(s) traced.
		/// </summary>
		/// <typeparam name="T">The type of the parameter.</typeparam>
		/// <typeparam name="TValue">The result of the expression.</typeparam>
		/// <param name="accessorExpressions">One or more expressions to evaluate against the parameter.</param>
		/// <returns>A continuation of the configuration.</returns>
		IParameterBuilderWithParameter And<T, TValue>(params Expression<Func<T, TValue>>[] accessorExpressions);

		/// <summary>
		/// Renames the previous item(s) traced.
		/// If one alias is given, the last traced item is renamed.
		/// Otherwise, the number of aliases must match the number of items traced in the last bundle.
		/// </summary>
		/// <param name="aliases">The set of aliases to use.</param>
		/// <returns>A continuation of the configuration.</returns>
		IParameterBuilderWithParameter As(params string[] aliases);

		/// <summary>
		/// Renames the previous bundle of items traced.
		/// </summary>
		/// <param name="alias">The set of aliases to use.</param>
		/// <returns>A continuation of the configuration.</returns>
		IParameterBuilderWithParameter TogetherAs(string alias);
	}

	/// <summary>
	/// Configures the parameters that are traced.
	/// </summary>
	/// <typeparam name="T">The type of the parameter currently selected.</typeparam>
	/// <remarks>These methods are valid inside With blocks.</remarks>
	public interface IParameterBuilderWithType<T> : IParameterBuilderBase
	{
		/// <summary>
		/// Trace one or more parameters by name.
		/// </summary>
		/// <param name="parameterNames">The list of parameters to trace.</param>
		/// <returns>A continuation of the configuration.</returns>
		IParameterBuilderWithTypeAndParameter<T> Trace(params string[] parameterNames);

		/// <summary>
		/// Trace an expression, filtering by parameter name, and giving an alias.
		/// </summary>
		/// <typeparam name="TValue">The result of the expression.</typeparam>
		/// <param name="parameterName">The name of the parameter.</param>
		/// <param name="accessorExpressions">One or more expressions to evaluate against the parameter.</param>
		/// <returns>A continuation of the configuration.</returns>
		IParameterBuilderWithTypeAndParameter<T> Trace<TValue>(string parameterName, params Expression<Func<T, TValue>>[] accessorExpressions);

		/// <summary>
		/// Trace an expression.
		/// </summary>
		/// <typeparam name="TValue">The result of the expression.</typeparam>
		/// <param name="accessorExpressions">One or more expressions to evaluate against the parameter.</param>
		/// <returns>A continuation of the configuration.</returns>
		IParameterBuilderWithTypeAndParameter<T> Trace<TValue>(params Expression<Func<T, TValue>>[] accessorExpressions);

		/// <summary>
		/// Ends a With block.
		/// </summary>
		/// <returns>A continuation of the configuration.</returns>
		IParameterBuilder EndWith();
	}

	/// <summary>
	/// Configures the parameters that are traced.
	/// </summary>
	/// <typeparam name="T">The type of the parameter currently selected.</typeparam>
	/// <remarks>These methods are valid inside With blocks when at least one parameter defined.</remarks>
	public interface IParameterBuilderWithTypeAndParameter<T> : IParameterBuilderWithType<T>
	{
		/// <summary>
		/// Traces additional parameters by name, combining them with the previous item(s) traced.
		/// </summary>
		/// <param name="parameterNames">The list of parameters to trace.</param>
		/// <returns>A continuation of the configuration.</returns>
		IParameterBuilderWithTypeAndParameter<T> And(params string[] parameterNames);

		/// <summary>
		/// Traces additional expressions, combining them with the previous item(s) traced.
		/// </summary>
		/// <typeparam name="TValue">The result of the expression.</typeparam>
		/// <param name="accessorExpressions">One or more expressions to evaluate against the parameter.</param>
		/// <returns>A continuation of the configuration.</returns>
		IParameterBuilderWithTypeAndParameter<T> And<TValue>(params Expression<Func<T, TValue>>[] accessorExpressions);

		/// <summary>
		/// Renames the previous item(s) traced.
		/// If one alias is given, the last traced item is renamed.
		/// Otherwise, the number of aliases must match the number of items traced in the last bundle.
		/// </summary>
		/// <param name="aliases">The set of aliases to use.</param>
		/// <returns>A continuation of the configuration.</returns>
		IParameterBuilderWithTypeAndParameter<T> As(params string[] aliases);

		/// <summary>
		/// Renames the previous bundle of items traced.
		/// </summary>
		/// <param name="alias">The set of aliases to use.</param>
		/// <returns>A continuation of the configuration.</returns>
		IParameterBuilderWithTypeAndParameter<T> TogetherAs(string alias);
	}
	#endregion

	/// <summary>
	/// Implements a ParameterBuilder that binds to any method.
	/// </summary>
	internal class ParameterBuilder : IParameterBuilder, IParameterBuilderWithParameter
	{
		/// <summary>
		/// The values to trace.
		/// </summary>
		private List<ParameterBuilderValue> _values = new List<ParameterBuilderValue>();

		/// <summary>
		/// Initializes a new instance of the ParameterBuilder class.
		/// </summary>
		/// <param name="tpp">The TraceParameterProvider to bind to.</param>
		public ParameterBuilder(TraceParameterProvider tpp)
		{
			Provider = tpp;
		}

		/// <summary>
		/// Gets the alias to use when outputting this parameter.
		/// </summary>
		public string Alias { get; private set; }

		/// <summary>
		/// Gets the provider that this builder is bound to.
		/// </summary>
		public TraceParameterProvider Provider { get; private set; }

		/// <summary>
		/// Gets the list of values to trace.
		/// </summary>
		public IEnumerable<ParameterBuilderValue> Values { get { return _values; } }

		/// <summary>
		/// Determines whether this ParameterBuilder matches the given method.
		/// </summary>
		/// <param name="methodInfo">The method to analyze.</param>
		/// <returns>True if the method matches.</returns>
		public virtual bool Matches(MethodInfo methodInfo)
		{
			return true;
		}

		#region Trace Methods
		/// <inheritdoc/>
		public IParameterBuilderWithParameter Trace(params string[] parameterNames)
		{
			if (parameterNames == null) throw new ArgumentNullException("parameterNames");

			IParameterBuilderWithParameter builder = New();
			foreach (var p in parameterNames)
				builder = builder.And(p);

			return builder;
		}

		/// <inheritdoc/>
		public IParameterBuilderWithParameter Trace<TParam, TValue>(string parameterName, params Expression<Func<TParam, TValue>>[] accessorExpressions)
		{
			return Trace(parameterName, parameterName, accessorExpressions);
		}

		/// <inheritdoc/>
		public IParameterBuilderWithParameter Trace<TParam, TValue>(string parameterName, string alias, params Expression<Func<TParam, TValue>>[] accessorExpressions)
		{
			if (accessorExpressions == null) throw new ArgumentNullException("accessorExpressions");

			ParameterBuilder newBuilder = New();
			ParameterBuilder builder = newBuilder;

			foreach (var a in accessorExpressions)
			{
				// attempt to get the name of the parameter off of a simple expression
				string accessorAlias = null;
				MemberExpression expression = a.Body as MemberExpression;
				if (expression != null)
					accessorAlias = expression.Member.Name;

				builder = builder.AndImpl(parameterName, accessorAlias, a);
			}

			// make sure that the alias is always the parameter name
			if (alias != null)
				newBuilder.Alias = alias;

			return builder;
		}

		/// <inheritdoc/>
		public IParameterBuilderWithParameter Trace<TParam, TValue>(params Expression<Func<TParam, TValue>>[] accessorExpressions)
		{
			return Trace(null, accessorExpressions);
		}
		#endregion

		#region And Methods
		/// <inheritdoc/>
		public IParameterBuilderWithParameter And(params string[] parameterNames)
		{
			if (parameterNames == null) throw new ArgumentNullException("parameterNames");

			ParameterBuilder builder = this;

			foreach (var p in parameterNames)
				builder = (ParameterBuilder)builder.AndImpl(p, p, (LambdaExpression)null);

			return builder;
		}

		/// <inheritdoc/>
		public IParameterBuilderWithParameter And<TParam, TValue>(params Expression<Func<TParam, TValue>>[] accessorExpressions)
		{
			if (accessorExpressions == null) throw new ArgumentNullException("accessorExpressions");

			ParameterBuilder builder = this;

			foreach (var a in accessorExpressions)
			{
				// attempt to get the name of the parameter off of a simple expression
				string alias = null;
				MemberExpression expression = a.Body as MemberExpression;
				if (expression != null)
					alias = expression.Member.Name;

				builder = AndImpl(null, alias, a);
			}

			return builder;
		}

		/// <inheritdoc/>
		public IParameterBuilderWithParameter And<TParam, TValue>(string parameterName, Expression<Func<TParam, TValue>> accessorExpression)
		{
			return AndImpl(parameterName, parameterName, (LambdaExpression)accessorExpression);
		}
		#endregion

		#region Ignore Methods
		/// <inheritdoc/>
		public IParameterBuilder Ignore(params string[] parameterNames)
		{
			if (parameterNames == null) throw new ArgumentNullException("parameterNames");

			var builder = New();
			foreach (var p in parameterNames)
				builder = builder.IgnoreImpl(p, null);

			return builder;
		}

		/// <inheritdoc/>
		public IParameterBuilder Ignore(params Type[] types)
		{
			if (types == null) throw new ArgumentNullException("types");

			var builder = New();
			foreach (var t in types)
				builder = builder.IgnoreImpl(null, t);

			return builder;
		}

		/// <inheritdoc/>
		public IParameterBuilder Ignore<T>()
		{
			return IgnoreImpl(null, typeof(T));
		}
		#endregion

		#region As Methods
		/// <inheritdoc/>
		public IParameterBuilderWithParameter As(params string[] aliases)
		{
			if (aliases == null) throw new ArgumentNullException("aliases");

			// if there is only one value in the parameter, then set the name
			if (_values.Count < 2 && aliases.Length == 1)
				Alias = aliases[0];

			// add the aliases to the values
			if (aliases.Length == 1)
			{
				// if there is only one alias, apply to the last value entered
				_values.Last().Alias = aliases[0];
			}
			else if (_values.Count == aliases.Length)
			{
				// if there is more than one, and the counts match, then do a one-to-one mapping
				for (int i = 0; i < _values.Count; i++)
					_values[i].Alias = aliases[i];
			}
			else
				throw new ArgumentException("The number of aliases must match the number of values in the parameter", "aliases");

			return this;
		}

		/// <inheritdoc/>
		public IParameterBuilderWithParameter TogetherAs(string alias)
		{
			Alias = alias;

			return this;
		}
		#endregion

		#region With Methods
		/// <inheritdoc/>
		public IParameterBuilderWithType<TParam> With<TParam>()
		{
			return new ParameterBuilderWithType<TParam>(this, null);
		}

		/// <inheritdoc/>
		public IParameterBuilderWithType<TParam> With<TParam>(string parameterName)
		{
			return new ParameterBuilderWithType<TParam>(this, parameterName);
		}
		#endregion

		#region For Methods
		/// <inheritdoc/>
		public IParameterBuilder ForAnything()
		{
			return new ParameterBuilder(Provider);
		}

		/// <inheritdoc/>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter")]
		public IParameterBuilder For<TSource>()
		{
			return new ParameterBuilder<TSource>(Provider);
		}

		/// <inheritdoc/>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
		public IParameterBuilder For<TSource>(Expression<Action<TSource>> methodExpression)
		{
			return new ParameterBuilder<TSource>(Provider, methodExpression);
		}
		#endregion

		#region Context Methods
		/// <inheritdoc/>
		public IParameterBuilder AddContext<TValue>(string alias, Expression<Func<TValue>> contextExpression)
		{
			var builder = New();

			builder.Alias = alias;
			builder._values.Add(new ParameterBuilderValue(null, alias, null, contextExpression));

			Provider.Add(builder);

			return builder;
		}

		/// <inheritdoc/>
		public IParameterBuilder AddContextData<TValue>(string alias, string key = null)
		{
			if (alias == null) throw new ArgumentNullException("alias");
			if (key == null)
				key = alias;

			// need to bind the alias into an expression to be evaluated at runtime
			var constant = Expression.Constant(key);
			var call = Expression.Call(typeof(TraceContext).GetMethod("GetValue"), constant);
			var castAsObject = Expression.Convert(call, typeof(TValue));
			var lambda = Expression.Lambda<Func<TValue>>(castAsObject);

			return AddContext<TValue>(alias, lambda);
		}
		#endregion

		#region Implementation
		/// <summary>
		/// Constructs a new instance of a ParameterBuilder.
		/// </summary>
		/// <returns>A new ParameterBuilder.</returns>
		protected virtual ParameterBuilder New()
		{
			return new ParameterBuilder(Provider);
		}

		/// <summary>
		/// Implements the And operation by adding values to the builder.
		/// </summary>
		/// <param name="parameterName">The name of the parameter to add.</param>
		/// <param name="alias">An optional alias of the parameter.</param>
		/// <param name="accessorExpression">An optional expression to use.</param>
		/// <returns>A continuation of the configuration.</returns>
		private ParameterBuilder AndImpl(string parameterName, string alias, LambdaExpression accessorExpression)
		{
			// if (Alias == null)
			// 	Alias = alias;

			var value = new ParameterBuilderValue(parameterName, alias, null, accessorExpression);
			_values.Add(value);

			Provider.Add(this);

			return this;
		}

		/// <summary>
		/// Implements the Ignore operation by adding values to the builder.
		/// </summary>
		/// <param name="parameterName">The name of the parameter to add.</param>
		/// <param name="type">An optional type to ignore.</param>
		/// <returns>A continuation of the configuration.</returns>
		private ParameterBuilder IgnoreImpl(string parameterName, Type type)
		{
			var value = new ParameterBuilderValue(parameterName, parameterName, type, null);
			value.Ignore = true;
			_values.Add(value);

			Provider.Add(this);

			return this;
		}
		#endregion
	}

	/// <summary>
	/// Implements a ParameterBuilder where the type or method has been specified.
	/// </summary>
	/// <typeparam name="T">The type of the interface.</typeparam>
	[SuppressMessage("Microsoft.StyleCop.CSharp.MaintainabilityRules", "SA1402:FileMayOnlyContainASingleClass", Justification = "The classes are related by implementing multiple generic signatures.")]
	internal class ParameterBuilder<T> : ParameterBuilder
	{
		/// <summary>
		/// The method to bind to.
		/// </summary>
		private MethodInfo _method;

		/// <summary>
		/// Initializes a new instance of the ParameterBuilder class.
		/// </summary>
		/// <param name="tpp">The TraceParameterProvider to bind to.</param>
		public ParameterBuilder(TraceParameterProvider tpp) : base(tpp)
		{
		}

		/// <summary>
		/// Initializes a new instance of the ParameterBuilder class.
		/// </summary>
		/// <param name="tpp">The TraceParameterProvider to bind to.</param>
		/// <param name="methodExpression">An expression representing the method call to bind to.</param>
		public ParameterBuilder(TraceParameterProvider tpp, Expression<Action<T>> methodExpression) : base(tpp)
		{
			var methodCall = methodExpression.Body as MethodCallExpression;
			if (methodCall == null)
				throw new ArgumentException("methodExpression must be a call to a method on the interface", "methodExpression");

			_method = methodCall.Method;
		}

		/// <summary>
		/// Determines whether this builder matches the given method.
		/// </summary>
		/// <param name="methodInfo">The method to analyze.</param>
		/// <returns>True if this builder matches the given method.</returns>
		public override bool Matches(MethodInfo methodInfo)
		{
			return (methodInfo.DeclaringType == typeof(T) || methodInfo.DeclaringType.IsSubclassOf(typeof(T))) &&
					(_method == null || _method == methodInfo);
		}

		/// <summary>
		/// Constructs a new instance of a ParameterBuilder.
		/// </summary>
		/// <returns>A new ParameterBuilder.</returns>
		protected override ParameterBuilder New()
		{
			return new ParameterBuilder<T>(Provider);
		}
	}

	/// <summary>
	/// Wraps the ParameterBuilder configuration within a With block, before a parameter has been logged.
	/// </summary>
	/// <typeparam name="T">The type of the With block.</typeparam>
	[SuppressMessage("Microsoft.StyleCop.CSharp.MaintainabilityRules", "SA1402:FileMayOnlyContainASingleClass", Justification = "The classes are related by implementing multiple generic signatures.")]
	internal class ParameterBuilderWithType<T> : IParameterBuilderWithType<T>
	{
		/// <summary>
		/// Initializes a new instance of the ParameterBuilderWithType class.
		/// </summary>
		/// <param name="builder">The builder to bind to.</param>
		/// <param name="parameterName">An optional name of the parameter to filter on.</param>
		public ParameterBuilderWithType(ParameterBuilder builder, string parameterName)
		{
			Builder = builder;
			ParameterName = parameterName;
		}

		/// <summary>
		/// Gets the builder this wrapper is bound to.
		/// </summary>
		protected ParameterBuilder Builder { get; private set; }

		/// <summary>
		/// Gets the name of the parameter to filter on.
		/// </summary>
		protected string ParameterName { get; private set; }

		/// <inheritdoc/>
		public IParameterBuilderWithTypeAndParameter<T> Trace(params string[] parameterNames)
		{
			return new ParameterBuilderWithTypeAndParameter<T>(Builder.Trace(parameterNames), ParameterName);
		}

		/// <inheritdoc/>
		public IParameterBuilderWithTypeAndParameter<T> Trace<TValue>(string parameterName, params Expression<Func<T, TValue>>[] accessorExpressions)
		{
			return new ParameterBuilderWithTypeAndParameter<T>(Builder.Trace(parameterName, accessorExpressions), ParameterName);
		}

		/// <inheritdoc/>
		public IParameterBuilderWithTypeAndParameter<T> Trace<TValue>(params Expression<Func<T, TValue>>[] accessorExpressions)
		{
			return new ParameterBuilderWithTypeAndParameter<T>(Builder.Trace(ParameterName, null, accessorExpressions), ParameterName);
		}

		/// <inheritdoc/>
		public IParameterBuilderWithType<TOther> With<TOther>()
		{
			return Builder.With<TOther>();
		}

		/// <inheritdoc/>
		public IParameterBuilderWithType<TOther> With<TOther>(string parameterName)
		{
			return Builder.With<TOther>(parameterName);
		}

		/// <inheritdoc/>
		public IParameterBuilder EndWith()
		{
			return Builder;
		}

		/// <inheritdoc/>
		public IParameterBuilder ForAnything()
		{
			return Builder.ForAnything();
		}

		/// <inheritdoc/>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter")]
		public IParameterBuilder For<TSource>()
		{
			return Builder.For<TSource>();
		}

		/// <inheritdoc/>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
		public IParameterBuilder For<TSource>(Expression<Action<TSource>> methodExpression)
		{
			return Builder.For<TSource>(methodExpression);
		}
	}

	/// <summary>
	/// Wraps the ParameterBuilder configuration within a With block, after a parameter has been logged.
	/// </summary>
	/// <typeparam name="T">The type of the With block.</typeparam>
	[SuppressMessage("Microsoft.StyleCop.CSharp.MaintainabilityRules", "SA1402:FileMayOnlyContainASingleClass", Justification = "The classes are related by implementing multiple generic signatures.")]
	internal class ParameterBuilderWithTypeAndParameter<T> : ParameterBuilderWithType<T>, IParameterBuilderWithTypeAndParameter<T>
	{
		/// <summary>
		/// Initializes a new instance of the ParameterBuilderWithTypeAndParameter class.
		/// </summary>
		/// <param name="builder">The builder to bind to.</param>
		/// <param name="parameterName">An optional name of the parameter to filter on.</param>
		public ParameterBuilderWithTypeAndParameter(IParameterBuilderWithParameter builder, string parameterName) : base((ParameterBuilder)builder, parameterName)
		{
		}

		/// <inheritdoc/>
		public IParameterBuilderWithTypeAndParameter<T> And(params string[] parameterNames)
		{
			return new ParameterBuilderWithTypeAndParameter<T>(Builder.And(parameterNames), ParameterName);
		}

		/// <inheritdoc/>
		public IParameterBuilderWithTypeAndParameter<T> And<TValue>(params Expression<Func<T, TValue>>[] accessorExpressions)
		{
			return new ParameterBuilderWithTypeAndParameter<T>(Builder.And(accessorExpressions), ParameterName);
		}

		/// <inheritdoc/>
		public IParameterBuilderWithTypeAndParameter<T> As(params string[] aliases)
		{
			return new ParameterBuilderWithTypeAndParameter<T>(Builder.As(aliases), ParameterName);
		}

		/// <inheritdoc/>
		public IParameterBuilderWithTypeAndParameter<T> TogetherAs(string alias)
		{
			return new ParameterBuilderWithTypeAndParameter<T>(Builder.TogetherAs(alias), ParameterName);
		}
	}
}
