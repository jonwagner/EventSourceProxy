using System;
using System.Collections.Generic;
using System.Linq;
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
				// determine the name to trace under
				var traceAs = parameter.GetCustomAttribute<TraceAsAttribute>() ?? traceAsDefault;
				var traceName = (traceAs != null) ? traceAs.Name : parameter.Name;

				// find the mapping that matches the name
				// create a new mapping or add to the existing mapping of the given name
				var mapping = parameters.Find(p => String.Compare(p.Name, traceName, StringComparison.OrdinalIgnoreCase) == 0);
				if (mapping == null)
					parameters.Add(new ParameterMapping(traceName, parameter));
				else
					mapping.AddSource(parameter);
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
