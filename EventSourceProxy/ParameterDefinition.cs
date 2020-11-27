using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EventSourceProxy
{
	/// <summary>
	/// Represents the definition of a parameter.
	/// </summary>
	class ParameterDefinition
	{
		/// <summary>
		/// Initializes a new instance of the ParameterDefinition class from a parameter.
		/// </summary>
		/// <param name="alias">The name of the parameter.</param>
		/// <param name="position">The position of the parameter on the stack.</param>
		/// <param name="sourceType">The type of the parameter.</param>
		public ParameterDefinition(string alias, int position, Type sourceType)
		{
			if (sourceType == null) throw new ArgumentNullException("sourceType");
			if (alias == null) throw new ArgumentNullException("alias");
			if (position < 0) throw new ArgumentOutOfRangeException("position", "position must not be negative");

			Position = position;
			SourceType = sourceType;
			Alias = alias;
		}

		/// <summary>
		/// Initializes a new instance of the ParameterDefinition class from parameter.
		/// </summary>
		/// <param name="alias">The name of the parameter.</param>
		/// <param name="parameterInfo">The parameter to bind to.</param>
		/// <param name="converter">An optional converter that converts the parameter to a desired result.</param>
		public ParameterDefinition(string alias, ParameterInfo parameterInfo, ParameterConverter converter = null)
		{
			if (alias == null) throw new ArgumentNullException("alias");

			Alias = alias;
			Converter = converter;

			if (parameterInfo != null)
			{
				Position = parameterInfo.Position;
				SourceType = parameterInfo.ParameterType;

				if (converter != null)
				{
					if (SourceType != converter.InputType && !SourceType.IsSubclassOf(converter.InputType))
						throw new ArgumentException("The conversion expression must match the type of the parameter.", "converter");
				}
			}
			else
			{
				if (converter == null)
					throw new ArgumentException("A conversion expression must be specified.", "converter");
				if (converter.InputType != null)
					throw new ArgumentException("The conversion expression must take no parameters.", "converter");

				Position = -1;
				SourceType = converter.ReturnType;
			}
		}

		/// <summary>
		/// Gets the position of the parameter.
		/// </summary>
		/// <remarks>If less than zero, then there is no source of the parameter.</remarks>
		public int Position { get; private set; }

		/// <summary>
		/// Gets the type of the parameter.
		/// </summary>
		/// <remarks>If null, then there is no source of the parameter.</remarks>
		public Type SourceType { get; private set; }

		/// <summary>
		/// Gets the name to use to log the parameter.
		/// </summary>
		public string Alias { get; private set; }

		/// <summary>
		/// Gets an expression that converts the parameter to the intended logged value.
		/// </summary>
		public ParameterConverter Converter { get; private set; }
	}
}
