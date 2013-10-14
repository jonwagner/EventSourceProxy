using System;
using System.Collections.Generic;
using System.Linq;
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
		/// The target type of the parameter.
		/// </summary>
		private Type _targetType;

		/// <summary>
		/// Initializes a new instance of the ParameterMapping class from an existing parameter.
		/// </summary>
		/// <param name="name">The name of the parameter.</param>
		/// <param name="pi">The parameter to initialize from.</param>
		public ParameterMapping(string name, ParameterInfo pi)
		{
			if (pi == null) throw new ArgumentNullException("pi");

			MappingType = ParameterMappingType.Parameter;
			Name = name;

			AddSource(pi);
		}

		/// <summary>
		/// Initializes a new instance of the ParameterMapping class from values.
		/// </summary>
		/// <param name="mappingType">The type of the mapping.</param>
		/// <param name="name">The name of the parameter.</param>
		/// <param name="targetType">The target type of the mapping.</param>
		public ParameterMapping(ParameterMappingType mappingType, string name, Type targetType)
		{
			MappingType = mappingType;
			Name = name;
			TargetType = targetType;
		}

		/// <summary>
		/// Gets the sources of the parameter.
		/// </summary>
		public IEnumerable<ParameterDefinition> Sources { get { return _sources; } }

		/// <summary>
		/// Gets the name of the parameter.
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// Gets the target type of the parameter.
		/// </summary>
		public Type TargetType
		{
			get
			{
				if (_sources.Count == 1)
					return _sources[0].SourceType;
				else if (_sources.Count > 1)
					return typeof(object);
				else
					return _targetType;
			}

			private set
			{
				_targetType = value;
			}
		}

		/// <summary>
		/// Gets the target type of the parameter, compatible with ETW.
		/// </summary>
		public Type CleanTargetType { get { return TypeImplementer.GetTypeSupportedByEventSource(TargetType); } }

		/// <summary>
		/// Gets the mapping type.
		/// </summary>
		public ParameterMappingType MappingType { get; private set; }

		/// <summary>
		/// Gets the source type of the parameter.
		/// </summary>
		public Type SourceType
		{
			get
			{
				if (_sources.Count == 0)
					return TargetType;
				else if (_sources.Count == 1)
					return _sources[0].SourceType;
				else
					throw new InvalidOperationException();
			}
		}

		/// <summary>
		/// Gets the source of the parameter.
		/// </summary>
		public ParameterDefinition Source
		{
			get
			{
				if (_sources.Count == 1)
					return _sources[0];
				return null;
			}
		}

		/// <summary>
		/// Adds a parameter source to this mapping.
		/// </summary>
		/// <param name="pi">The parameter to add.</param>
		public void AddSource(ParameterInfo pi)
		{
			if (pi == null) throw new ArgumentNullException("pi");

			_sources.Add(new ParameterDefinition(pi.Position, pi.ParameterType, pi.Name));
		}
	}
}
