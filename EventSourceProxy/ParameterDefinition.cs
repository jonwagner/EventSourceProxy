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
		/// Initializes a new instance of the ParameterDefinition class.
		/// </summary>
		/// <param name="position">The position of the parameter on the stack.</param>
		/// <param name="sourceType">The type of the parameter.</param>
		/// <param name="name">The name of the parameter.</param>
		/// <param name="converter">An optional converter that converts the parameter to a desired result.</param>
		public ParameterDefinition(int position, Type sourceType, string name, LambdaExpression converter = null)
		{
			if (sourceType == null) throw new ArgumentNullException("sourceType");
			if (name == null) throw new ArgumentNullException("name");
			if (position < 0) throw new ArgumentOutOfRangeException("position", "position must not be negative");

			Position = position;
			SourceType = sourceType;
			Name = name;
			Converter = converter;
		}

		/// <summary>
		/// Gets the position of the parameter.
		/// </summary>
		public int Position { get; private set; }

		/// <summary>
		/// Gets the type of the parameter.
		/// </summary>
		public Type SourceType { get; private set; }

		/// <summary>
		/// Gets the name of the parameter.
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// Gets an expression that converts the parameter to the intended logged value.
		/// </summary>
		public LambdaExpression Converter { get; private set; }
	}
}
