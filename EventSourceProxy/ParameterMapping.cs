﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EventSourceProxy
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
					return _sources[0].SourceType;
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
		public void AddSource(ParameterInfo pi)
		{
			if (pi == null) throw new ArgumentNullException("pi");

			AddSource(new ParameterDefinition(pi.Position, pi.ParameterType, pi.Name));
		}

		/// <summary>
		/// Adds a parameter source to this mapping.
		/// </summary>
		/// <param name="pi">The parameter to add.</param>
		/// <param name="converter">A converter that converts the parameter to a desired value.</param>
		public void AddSource(ParameterInfo pi, LambdaExpression converter)
		{
			if (pi == null) throw new ArgumentNullException("pi");

			_sources.Add(new ParameterDefinition(pi.Position, pi.ParameterType, pi.Name, converter));
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
			AddSource(pi, (LambdaExpression)converter);
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
