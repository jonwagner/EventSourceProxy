using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if NUGET
namespace EventSourceProxy.NuGet
#else
namespace EventSourceProxy
#endif
{
	/// <summary>
	/// Specifies the name to use to trace the given parameter.
	/// </summary>
	/// <remarks>
	/// If multiple parameters are traced into the same name, then they are traced as a string-to-string map,
	/// and serialized into a string by the TraceSerializationProvider.
	/// If TraceAsAttribute is applied to a method, then all parameters of the method are traced into the specified name
	/// unless other TraceAsAttributes are applied.
	/// </remarks>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1813:AvoidUnsealedAttributes")]
	[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Method)]
	public class TraceAsAttribute : Attribute
	{
		/// <summary>
		/// Initializes a new instance of the TraceAsAttribute class, providing a name for the given parameter.
		/// </summary>
		public TraceAsAttribute()
		{
		}

		/// <summary>
		/// Initializes a new instance of the TraceAsAttribute class, providing a name for the given parameter.
		/// </summary>
		/// <param name="name">The name to trace the parameter as.</param>
		public TraceAsAttribute(string name)
		{
			Name = name;
		}

		/// <summary>
		/// Gets the name to use when tracing the parameter.
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// Gets or sets the String.Format to use when tracing the parameter.
		/// </summary>
		public string Format { get; set; }
	}
}
