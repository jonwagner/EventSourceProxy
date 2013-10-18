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
	/// Implements a provider that can bundle the parameters of an interface.
	/// </summary>
	public class TraceParameterProvider
	{
		/// <summary>
		/// Returns the parameter mapping for the given method.
		/// </summary>
		/// <param name="methodInfo">The method to analyze.</param>
		/// <returns>A list of ParameterMapping representing the desired bundling of parameters.</returns>
		public virtual IReadOnlyCollection<ParameterMapping> ProvideParameterMapping(MethodInfo methodInfo)
		{
			if (methodInfo == null) throw new ArgumentNullException("methodInfo");

			// get the default name to use for tracing (null if none specified)
			var traceAsDefault = methodInfo.GetCustomAttribute<TraceAsAttribute>();

			var parameters = new List<ParameterMapping>();

			foreach (var parameter in methodInfo.GetParameters())
			{
				// we need one parameter per attribute, and at least one per parameter
				var attributes = parameter.GetCustomAttributes<TraceAsAttribute>();
				if (!attributes.Any())
					attributes = new TraceAsAttribute[1] { traceAsDefault ?? new TraceAsAttribute(parameter.Name) };

				foreach (var attribute in attributes)
				{
					var traceName = attribute.Name;

					// find the mapping that matches the name or create a new mapping
					var mapping = parameters.FirstOrDefault(p => String.Compare(p.Name, traceName, StringComparison.OrdinalIgnoreCase) == 0);
					if (mapping == null)
					{
						mapping = new ParameterMapping(traceName);
						parameters.Add(mapping);
					}

					// if the attribute is a TraceMember, then create an expression to get the member
					LambdaExpression expression = null;
					var traceMember = attribute as TraceMemberAttribute;
					if (traceMember != null)
					{
						var input = Expression.Parameter(parameter.ParameterType);
						expression = Expression.Lambda(
							Expression.MakeMemberAccess(
								input,
								parameter.ParameterType.GetMember(traceMember.Member).First()),
							input);
					}

					mapping.AddSource(parameter, expression);
				}
			}

			return parameters.AsReadOnly();
		}

		/// <summary>
		/// Uses the TraceParameterProvider to get the mapping for the given method.
		/// </summary>
		/// <param name="methodInfo">The method to analyze.</param>
		/// <returns>A list of parameter mappings for the method.</returns>
		internal static IReadOnlyCollection<ParameterMapping> GetParameterMapping(MethodInfo methodInfo)
		{
			var provider = ProviderManager.GetProvider<TraceParameterProvider>(methodInfo.DeclaringType, typeof(TraceParameterProviderAttribute), () => new TraceParameterProvider());

			return provider.ProvideParameterMapping(methodInfo);
		}
	}
}
