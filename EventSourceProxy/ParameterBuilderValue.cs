using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace EventSourceProxy
{
	/// <summary>
	/// Represents a value to be extracted from a parameter list.
	/// </summary>
	class ParameterBuilderValue
	{
		/// <summary>
		/// Initializes a new instance of the ParameterBuilderValue class.
		/// </summary>
		/// <param name="parameterName">The name of the parameter to bind to, or null to bind to parameters of any name.</param>
		/// <param name="alias">The name to use to trace the parameter.</param>
		/// <param name="parameterType">The type of parameter to filter on.</param>
		/// <param name="converter">An expression used to convert the parameter to another value, or null to use the parameter as it is.</param>
		public ParameterBuilderValue(string parameterName, string alias, Type parameterType, LambdaExpression converter)
		{
			Alias = alias;
			ParameterName = parameterName;

			Converter = converter;

			if (converter != null && converter.Parameters.Count > 0)
				ParameterType = parameterType ?? converter.Parameters[0].Type;
			else
				ParameterType = parameterType;
		}

		/// <summary>
		/// Gets or sets the name to use to log the value.
		/// </summary>
		public string Alias { get; set; }

		/// <summary>
		/// Gets the name of the parameter to bind to, or null, representing binding to a parameter of any name.
		/// </summary>
		public string ParameterName { get; private set; }

		/// <summary>
		/// Gets the type of parameter to bind to.
		/// </summary>
		public Type ParameterType { get; private set; }

		/// <summary>
		/// Gets the expression to use to convert the value. If set, the value will only bind to parameters matching the given type.
		/// </summary>
		public LambdaExpression Converter { get; private set; }

		/// <summary>
		/// Gets or sets a value indicating whether the parameter should be ignored.
		/// </summary>
		public bool Ignore { get; set; }

		/// <summary>
		/// Returns all of the parameters that match this value's name and converter.
		/// </summary>
		/// <param name="parameter">The parameter to evaluate.</param>
		/// <returns>An enumeration of the matching parameters.</returns>
		public bool Matches(ParameterInfo parameter)
		{
			// if both name and type are not specified, then this only matches the null parameter
			// otherwise we will match every parameter
			if (ParameterName == null && ParameterType == null)
				return parameter == null;
			if (parameter == null)
				return false;

			// filter by name and/or parameter type
			return 
				(ParameterName == null || String.Compare(parameter.Name, ParameterName, StringComparison.OrdinalIgnoreCase) == 0) &&
				(ParameterType == null || parameter.ParameterType == ParameterType || parameter.ParameterType.IsSubclassOf(ParameterType));
		}

		/// <summary>
		/// Returns all of the parameters that match this value's name and converter.
		/// </summary>
		/// <param name="parameters">The parameters to evaluate.</param>
		/// <returns>An enumeration of the matching parameters.</returns>
		public IEnumerable<ParameterInfo> Matches(IEnumerable<ParameterInfo> parameters)
		{
			// filter by name and/or parameter type
			return parameters.Where(p => Matches(p));
		}
	}
}
