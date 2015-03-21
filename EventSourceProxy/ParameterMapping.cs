using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

#if NUGET
namespace EventSourceProxy.NuGet
#else
namespace EventSourceProxy
#endif
{
	/// <summary>
	/// Represents the mapping between the caller's parameters and the parameters for the underlying method.
	/// </summary>
	public class ParameterMapping
	{
		/// <summary>
		/// The sources of the parameter.
		/// </summary>
		private List<ParameterDefinition> _sources = new List<ParameterDefinition>();

		/// <summary>
		/// Initializes a new instance of the ParameterMapping class with an empty source list.
		/// </summary>
		/// <param name="name">The name of the parameter.</param>
		public ParameterMapping(string name)
		{
			Name = name;
		}

		/// <summary>
		/// Gets the name of the parameter.
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// Gets the sources of the parameter.
		/// </summary>
		internal IEnumerable<ParameterDefinition> Sources { get { return _sources; } }

		/// <summary>
		/// Gets the target type of the parameter.
		/// </summary>
		internal Type TargetType
		{
			get
			{
				if (_sources.Count == 1)
				{
					// if there is one source, then we use the type of the individual source
					var source = _sources[0];
					if (source.Converter != null)
						return source.Converter.ReturnType;

					return source.SourceType;
				}

				// otherwise we have multiple sources, or a context, so we target a string
				return typeof(string);
			}
		}

		/// <summary>
		/// Gets the target type of the parameter, compatible with ETW.
		/// </summary>
		internal Type CleanTargetType { get { return TypeImplementer.GetTypeSupportedByEventSource(TargetType); } }

		/// <summary>
		/// Gets a value indicating whether this mapping has any sources.
		/// </summary>
		internal bool HasSource { get { return _sources.Any(); } }

		/// <summary>
		/// Gets the source type of the parameter.
		/// </summary>
		internal Type SourceType
		{
			get
			{
				// if there is one source, then use its type, otherwise we are reading from an object
				if (_sources.Count == 1)
					return _sources[0].SourceType;
				else
					return typeof(object);
			}
		}

		/// <summary>
		/// Adds a parameter source to this mapping.
		/// </summary>
		/// <param name="pi">The parameter to add.</param>
		/// <param name="alias">The alias to use to log the parameter.</param>
		/// <param name="converter">A converter that converts the parameter to a desired value.</param>
		public void AddSource(ParameterInfo pi, string alias = null, LambdaExpression converter = null)
		{
			if (alias == null)
			{
				if (pi == null)
					throw new ArgumentNullException("pi");
				else
					alias = pi.Name;
			}

			_sources.Add(new ParameterDefinition(alias, pi, converter));
		}

		/// <summary>
		/// Adds a parameter source to this mapping.
		/// </summary>
		/// <typeparam name="TIn">The input type of the converter.</typeparam>
		/// <typeparam name="TOut">The output type of the converter.</typeparam>
		/// <param name="pi">The parameter to add.</param>
		/// <param name="alias">The alias to use to log the parameter.</param>
		/// <param name="converter">A converter that converts the parameter to a desired value.</param>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures"),
			System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "This lets the compiler know to generate expressions")]
		public void AddSource<TIn, TOut>(ParameterInfo pi, string alias, Expression<Func<TIn, TOut>> converter)
		{
			if (pi == null) throw new ArgumentNullException("pi");

			AddSource(pi, alias, (LambdaExpression)converter);
		}

		/// <summary>
		/// Adds a parameter source to this mapping.
		/// </summary>
		/// <typeparam name="TIn">The input type of the converter.</typeparam>
		/// <typeparam name="TOut">The output type of the converter.</typeparam>
		/// <param name="pi">The parameter to add.</param>
		/// <param name="converter">A converter that converts the parameter to a desired value.</param>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures"), 
			System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "This lets the compiler know to generate expressions")]
		public void AddSource<TIn, TOut>(ParameterInfo pi, Expression<Func<TIn, TOut>> converter)
		{
			if (pi == null) throw new ArgumentNullException("pi");

			AddSource(pi, pi.Name, (LambdaExpression)converter);
		}

		/// <summary>
		/// Adds a context method to this mapping.
		/// </summary>
		/// <typeparam name="TOut">The output type of the context expression</typeparam>
		/// <param name="alias">The alias to use to log the context.</param>
		/// <param name="contextExpression">An expression that can generate context.</param>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures"),
			System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "This lets the compiler know to generate expressions")]
		public void AddContext<TOut>(string alias, Expression<Func<TOut>> contextExpression)
		{
			AddSource(new ParameterDefinition(alias, null, contextExpression));
		}

		/// <summary>
		/// Adds a parameter source to this mapping.
		/// </summary>
		/// <param name="source">The parameter to add.</param>
		internal void AddSource(ParameterDefinition source)
		{
			if (source == null) throw new ArgumentNullException("source");

			_sources.Add(source);
		}
	}
}
