using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EventSourceProxy
{
	/// <summary>
	/// Represents the definition of a parameter.
	/// </summary>
	public class ParameterDefinition
	{
		/// <summary>
		/// Initializes a new instance of the ParameterDefinition class.
		/// </summary>
		/// <param name="position">The position of the parameter on the stack.</param>
		/// <param name="sourceType">The type of the parameter.</param>
		/// <param name="name">The name of the parameter.</param>
		public ParameterDefinition(int position, Type sourceType, string name)
		{
			if (sourceType == null) throw new ArgumentNullException("sourceType");
			if (name == null) throw new ArgumentNullException("name");
			if (position < 0) throw new ArgumentOutOfRangeException("position", "position must not be negative");

			Position = position;
			SourceType = sourceType;
			Name = name;
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
	}
}
